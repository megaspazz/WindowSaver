using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowSaver
{
	public static class WinAPI
	{
		public static IntPtr GetHandleFromPoint(Point pt)
		{
			return GetHandleFromPoint(pt.X, pt.Y);
		}

		public static IntPtr GetHandleFromPoint(int x, int y)
		{
			POINT pt = new POINT(x, y);
			return WindowFromPoint(pt);
		}

		public static IntPtr GetHandleFromCursor()
		{
			return GetHandleFromPoint(Cursor.Position);
		}

		public static Rectangle GetBounds(IntPtr handle)
		{
			RECT r;
			GetWindowRect(handle, out r); ;
			return new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
		}

		public static Rectangle GetClientArea(IntPtr handle)
		{
			RECT r;
			GetClientRect(handle, out r);
			Point topLeft = default(Point);
			ClientToScreen(handle, ref topLeft);
			return new Rectangle(topLeft.X, topLeft.Y, r.Right - r.Left + 1, r.Bottom - r.Top + 1);
		}

		public static Point ToClientPoint(IntPtr handle, Point screenPt)
		{
			POINT pt = new POINT(screenPt.X, screenPt.Y);
			ScreenToClient(handle, ref pt);
			return new Point(pt.X, pt.Y);
		}

		public static Point ToWindowPoint(IntPtr handle, Point screenPt)
		{
			Rectangle rect = GetBounds(handle);
			return new Point(screenPt.X - rect.X, screenPt.Y - rect.Y);
		}

		public static void Resize(IntPtr handle, int w, int h)
		{
			Rectangle area = GetBounds(handle);
			MoveAndResize(handle, area.X, area.Y, w, h);
		}

		public static bool ByteArraysEqual(byte[] b1, byte[] b2)
		{
			return (b1.Length == b2.Length) && (memcmp(b1, b2, b1.Length) == 0);
		}

		public static List<IntPtr> GetAllWindows()
		{
			List<IntPtr> lst = new List<IntPtr>();
			IntPtr shellHandle = GetShellWindow();

			EnumWindows(delegate(IntPtr hWnd, int lParam)
			{
				if (hWnd == shellHandle)
				{
					return true;
				}
				lst.Add(hWnd);
				return true;
			}, 0);

			return lst;
		}

		public static string GetText(IntPtr handle)
		{
			int len = GetWindowTextLength(handle);
			StringBuilder sb = new StringBuilder(len);
			GetWindowText(handle, sb, len + 1);
			return sb.ToString();
		}

		public static WINDOWPLACEMENT GetWindowPosition(IntPtr hWnd)
		{
			WINDOWPLACEMENT wp = WINDOWPLACEMENT.Generate();
			wp.Length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
			GetWindowPlacement(hWnd, ref wp);
			return wp;
		}

		public static void MoveAndResize(IntPtr handle, int x, int y, int w, int h, int flags = 0)
		{
			MoveAndResize(handle, IntPtr.Zero, x, y, w, h, flags);
		}

		public static void MoveAndResize(IntPtr handle, IntPtr insert, int x, int y, int w, int h, int flags = 0)
		{
			SetWindowPos(handle, insert, x, y, w, h, flags);
		}

		public static bool ValidHandle(IntPtr handle)
		{
			return IsWindow(handle);
		}

		public static IntPtr GetWindowBelow(IntPtr handle)
		{
			return GetWindow(handle, 2);
		}

		public static void DisplayWindow(IntPtr handle, int showCmd)
		{
			ShowWindow(handle, showCmd);
		}

		public static void SetWindowPosition(IntPtr handle, WINDOWPLACEMENT wp)
		{
			SetWindowPlacement(handle, ref wp);
		}

		public static bool IsVisible(IntPtr handle)
		{
			return IsWindowVisible(handle);
		}

		public static void MinimizeAll()
		{
			IntPtr handle = FindWindow("Shell_TrayWnd", null);
			SendMessage(handle, 0x111, (IntPtr)419, IntPtr.Zero); 
		}

		public static IntPtr GetCurrentWindow()
		{
			return GetForegroundWindow();
		}

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		private static extern IntPtr WindowFromPoint(POINT point);

		[DllImport("user32.dll")]
		private static extern void GetWindowRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll")]
		static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll")]
		static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

		[DllImport("user32.dll")]
		static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

		[DllImport("user32.dll")]
		private static extern void SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern int memcmp(byte[] b1, byte[] b2, long count);

		[DllImport("user32.dll")]
		private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr GetShellWindow();

		[DllImport("user32.dll")]
		private static extern void GetWindowText(IntPtr hWnd, StringBuilder text, int count);

		[DllImport("user32.dll")]
		private static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool IsWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

		[DllImport("user32.dll")]
		private static extern void ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		private static extern IntPtr GetWindow(IntPtr hWnd, uint Command);

		[DllImport("user32.dll")]
		private static extern bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, Int32 Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

		[StructLayout(LayoutKind.Sequential)]
		[Serializable]
		public struct POINT
		{
			public int X;
			public int Y;

			public POINT(int x, int y)
			{
				this.X = x;
				this.Y = y;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		[Serializable]
		public struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;

			public Rectangle ToRectangle()
			{
				return new Rectangle(this.Left, this.Top, this.Right - this.Left, this.Bottom - this.Top);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		[Serializable]
		public struct WINDOWPLACEMENT
		{
			public int Length;
			public int Flags;
			public int ShowCmd;
			public POINT PtMinPosition;
			public POINT PtMaxPosition;
			public RECT RcNormalPosition;

			public static WINDOWPLACEMENT Generate()
			{
				return new WINDOWPLACEMENT()
				{
					Length = Marshal.SizeOf(typeof(WINDOWPLACEMENT))
				};
			}
		}
	}
}
