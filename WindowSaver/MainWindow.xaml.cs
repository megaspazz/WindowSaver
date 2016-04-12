using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;

namespace WindowSaver
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static readonly string AUTOSAVE_DIR = @"autosave";

		private WindowSaveInfo _info = null;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			_info = new WindowSaveInfo();
			_info.ForegroundWindow = WinAPI.GetCurrentWindow();
			List<IntPtr> lst = WinAPI.GetAllWindows();
			Graph g = new Graph();
			foreach (IntPtr hWnd in lst)
			{
				WindowPosition wp = WindowPosition.FromHandle(hWnd);
				if (WinAPI.IsVisible(hWnd) && !string.IsNullOrEmpty(wp.Text))
				{
					Console.WriteLine("[{0}] ({1}) {2} === {3} -> {4}", hWnd, wp.ShowCmd, WinAPI.IsVisible(hWnd), wp.Text, WinAPI.GetText(wp.NextWindow));
				}
				_info.WindowSave.Add(hWnd, wp);
				g.AddEdge(hWnd, wp.NextWindow);
			}
			_info.IterOrder = g.ToplogicalSort().Select(x => x.Handle).Reverse().ToList();
			this.btnWriteToFile.IsEnabled = true;
			this.btnLoad.IsEnabled = true;
			this.txtCurrent.Text = "User Save @ " + DateTime.Now;
			if (this.chkAutoSave.IsChecked.HasValue && this.chkAutoSave.IsChecked.Value)
			{
				if (!Directory.Exists(AUTOSAVE_DIR))
				{
					Directory.CreateDirectory(AUTOSAVE_DIR);
				}
				string timeStr = DateTime.Now.ToString("yyyyMMddhhmmssfff");
				string filename = Path.Combine(AUTOSAVE_DIR, timeStr + ".wsi");
				_info.Save(filename);
			}
		}

		private void btnLoad_Click(object sender, RoutedEventArgs e)
		{
			Console.WriteLine("LOADING...");
			WinAPI.MinimizeAll();
			foreach (IntPtr handle in _info.IterOrder)
			{
				WindowPosition wp;
				if (WinAPI.ValidHandle(handle) && _info.WindowSave.TryGetValue(handle, out wp))
				{
					WinAPI.MoveAndResize(handle, wp.NextWindow, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
					if (wp.Visible && !string.IsNullOrEmpty(wp.Text))
					{
						Console.WriteLine("MOVED: " + handle + " (" + wp.ShowCmd + ") [" + wp.Text + "]");
						WinAPI.SetWindowPosition(handle, wp.WindowPlacement);
					}
				}
			}
			WinAPI.DisplayWindow(_info.ForegroundWindow, 5);
		}

		private void btnWriteToFile_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog sfd = new SaveFileDialog()
			{
				Title = "Save Window Info",
				Filter = "Window Save Info|*.wsi"
			};
			bool? result = sfd.ShowDialog();
			if (result.HasValue && result.Value)
			{
				string file = sfd.FileName;
				_info.Save(file);
			}
		}

		private void btnLoadFromFile_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog()
			{
				Title = "Load Window Info",
				Filter = "Window Save Info|*.wsi"
			};
			bool? result = ofd.ShowDialog();
			if (result.HasValue && result.Value)
			{
				string file = ofd.FileName;
				this.txtCurrent.Text = file;
				this.btnLoad.IsEnabled = true;
				this.btnWriteToFile.IsEnabled = true;
				_info = WindowSaveInfo.FromFile(file);
			}
		}
	}

	[Serializable]
	public class WindowPosition
	{
		public IntPtr Handle { get; private set; }
		public Rectangle Position { get; private set; }
		public int ShowCmd { get; private set; }
		public IntPtr NextWindow { get; private set; }
		public bool Visible { get; private set; }
		public string Text { get; private set; }
		public WinAPI.WINDOWPLACEMENT WindowPlacement { get; private set; }

		public override bool Equals(object obj)
		{
			WindowPosition wp = obj as WindowPosition;
			if (wp == null) {
				return false;
			}
			return this.Handle.Equals(wp.Handle)
				&& this.Position.Equals(wp.Position)
				&& this.ShowCmd == wp.ShowCmd
				&& this.NextWindow.Equals(wp.NextWindow);
		}

		public override int GetHashCode()
		{
			return this.Handle.GetHashCode()
				^ this.Position.GetHashCode()
				^ this.ShowCmd
				^ this.NextWindow.GetHashCode();
		}

		public static WindowPosition FromHandle(IntPtr hWnd)
		{
			WinAPI.WINDOWPLACEMENT wp = WinAPI.GetWindowPosition(hWnd);
			Rectangle rect = wp.RcNormalPosition.ToRectangle();
			int cmd = wp.ShowCmd;
			IntPtr next = WinAPI.GetWindowBelow(hWnd);
			bool vis = WinAPI.IsVisible(hWnd);
			string txt = WinAPI.GetText(hWnd);
			return new WindowPosition
			{
				Handle = hWnd,
				Position = rect,
				ShowCmd = cmd,
				NextWindow = next,
				Visible = vis,
				Text = txt,
				WindowPlacement = wp
			};
		}
	}

	[Serializable]
	public class WindowSaveInfo
	{
		public Dictionary<IntPtr, WindowPosition> WindowSave { get; set; }
		public List<IntPtr> IterOrder { get; set; }
		public IntPtr ForegroundWindow { get; set; }

		public WindowSaveInfo()
		{
			this.WindowSave = new Dictionary<IntPtr, WindowPosition>();
			this.IterOrder = null;
			this.ForegroundWindow = IntPtr.Zero;
		}

		public void Save(string file)
		{
			using (FileStream fs = new FileStream(file, FileMode.Create))
			{
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(fs, this);
			}
		}

		public static WindowSaveInfo FromFile(string file)
		{
			WindowSaveInfo wsi;
			using (FileStream fs = new FileStream(file, FileMode.Open))
			{
				BinaryFormatter bf = new BinaryFormatter();
				wsi = (WindowSaveInfo)bf.Deserialize(fs);
			}
			return wsi;
		}
	}
	
	/**
	 * This method returns a list of the nodes after they have been topologically sorted.
	 * It will return null if it was impossible to do so.
	 * NOTE: This will destructively modify the graph!
	 */
	public class Graph
	{
		private Dictionary<IntPtr, Node> _nodes = new Dictionary<IntPtr, Node>();

		public void AddEdge(IntPtr hWnd1, IntPtr hWnd2)
		{
			Node n1 = _nodes.ContainsKey(hWnd1) ? _nodes[hWnd1] : new Node(hWnd1);
			Node n2 = _nodes.ContainsKey(hWnd2) ? _nodes[hWnd2] : new Node(hWnd2);
			n1.nexts.Add(n2);
			n2.prevs.Add(n1);
			_nodes[hWnd1] = n1;
			_nodes[hWnd2] = n2;
		}

		public List<Node> ToplogicalSort()
		{
			ICollection<Node> nodes = _nodes.Values;
			List<Node> lst = new List<Node>();
			HashSet<Node> starts = new HashSet<Node>();
			Queue<Node> q = new Queue<Node>();
			foreach (Node n in nodes)
			{
				if (n.prevs.Count == 0)
				{
					starts.Add(n);
					q.Enqueue(n);
				}
			}
			while (q.Count > 0)
			{
				Node n = q.Dequeue();
				if (!starts.Contains(n))
				{
					continue;
				}
				lst.Add(n);
				foreach (Node m in n.nexts)
				{
					m.prevs.Remove(n);
					if (m.prevs.Count == 0)
					{
						starts.Add(m);
						q.Enqueue(m);
					}
				}
				n.nexts.Clear();
			}
			foreach (Node n in nodes)
			{
				if ((n.prevs.Count > 0) || (n.nexts.Count > 0))
				{
					return null;
				}
			}
			return lst;
		}
	}

	/**
	 * A Node containing information about incoming and outgoing edges.
	 */
	public class Node
	{
		public IntPtr Handle { get; set; }
		public HashSet<Node> nexts { get; private set; }
		public HashSet<Node> prevs { get; private set; }

		public Node(IntPtr hWnd)
		{
			this.Handle = hWnd;
			this.nexts = new HashSet<Node>();
			this.prevs = new HashSet<Node>();
		}
	}
}
