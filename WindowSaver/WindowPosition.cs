using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowSaver
{
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
			if (wp == null)
			{
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
}
