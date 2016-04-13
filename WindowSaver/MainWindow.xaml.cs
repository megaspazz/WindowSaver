using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
			foreach (IntPtr handle in _info.IterOrder)
			{
				WindowPosition wp;
				if (WinAPI.ValidHandle(handle) && _info.WindowSave.TryGetValue(handle, out wp))
				{
					WinAPI.MoveAndResize(handle, wp.NextWindow, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
					if (wp.Visible && !string.IsNullOrEmpty(wp.Text))
					{
						WindowPosition curr = WindowPosition.FromHandle(handle);
						if (curr.ShowCmd == 2 && wp.ShowCmd == 2 && !wp.Position.IsEmpty)
						{
							WinAPI.DisplayWindow(handle, 9);
						}
						WinAPI.SetWindowPosition(handle, wp.WindowPlacement);
						WinAPI.MoveAndResize(handle, wp.NextWindow, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
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
}
