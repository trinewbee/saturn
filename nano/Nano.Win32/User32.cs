using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace Nano.Win32
{
	public static class User32
	{
		#region Window

		// HWND WINAPI GetParent(_In_ HWND hWnd);
		[DllImport("user32.dll")]
		public static extern IntPtr GetParent(IntPtr hwnd);

		// BOOL CALLBACK EnumWindowsProc(_In_ HWND hwnd, _In_ LPARAM lParam);
		public delegate int EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

		// BOOL WINAPI EnumWindows(_In_ WNDENUMPROC lpEnumFunc, _In_ LPARAM lParam);
		[DllImport("user32.dll")]
		public static extern int EnumWindows(EnumWindowsProc func, IntPtr lParam);

		// BOOL WINAPI IsWindowVisible(_In_ HWND hWnd);
		[DllImport("user32.dll")]
		public static extern int IsWindowVisible(IntPtr hwnd);

		// int WINAPI GetWindowText(_In_ HWND hWnd, _Out_ LPTSTR lpString, _In_ int nMaxCount);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetWindowText(IntPtr hwnd, StringBuilder lptrString, int nMaxCount);

		// HWND WINAPI WindowFromPoint(_In_ POINT Point);
		[DllImport("user32.dll")]
		public static extern int WindowFromPoint(WinBase.POINT point);

		// HWND WINAPI GetDesktopWindow(void);
		[DllImport("user32.dll")]
		public static extern IntPtr GetDesktopWindow();

		// HWND WINAPI SetParent(_In_ HWND hWndChild, _In_opt_ HWND hWndNewParent);
		[DllImport("user32.dll")]
		public static extern IntPtr SetParent(IntPtr hwndChild, IntPtr hwndParent);

		// HWND WINAPI FindWindow(_In_opt_ LPCTSTR lpClassName, _In_opt_ LPCTSTR lpWindowName);
		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(string className, string windowName);

		// HWND WINAPI GetShellWindow(void);
		[DllImport("user32.dll")]
		public static extern IntPtr GetShellWindow();

		// HWND WINAPI FindWindowEx(_In_opt_ HWND hwndParent, _In_opt_ HWND hwndChildAfter, _In_opt_ LPCTSTR lpszClass, _In_opt_ LPCTSTR lpszWindow);
		[DllImport("user32.dll")]
		public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		#endregion

		// BOOL WINAPI GetCursorPos(_Out_ LPPOINT lpPoint);
		[DllImport("user32.dll")]
		public static extern int GetCursorPos(ref WinBase.POINT lpPoint);

		// LRESULT WINAPI SendMessage(_In_ HWND hWnd, _In_ UINT Msg, _In_ WPARAM wParam, _In_ LPARAM lParam);
		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, UIntPtr wParam, IntPtr lParam);

		// DWORD WINAPI GetWindowThreadProcessId(_In_ HWND hWnd, _Out_opt_ LPDWORD lpdwProcessId);
		[DllImport("user32.dll")]
		public static extern uint GetWindowThreadProcessId(IntPtr hwnd, ref uint processId);
	}

	public class Window
	{
		public IntPtr m_hwnd;	// HWND

		public Window(IntPtr hwnd)
		{
			m_hwnd = hwnd;
		}

		#region Properties

		public Window Parent
		{
			get
			{
				var phwnd = User32.GetParent(m_hwnd);
				return phwnd !=  IntPtr.Zero ? new Window(phwnd) : null;
			}
		}

		public string Text
		{
			get
			{
				StringBuilder sb = new StringBuilder(512);
				int n = User32.GetWindowText(m_hwnd, sb, sb.Capacity);

				if (n == 0)
					return null;	// ignore error

				var s = sb.ToString();
				Debug.Assert(s.Length == n);
				return s;
			}
		}

		public bool Visible
		{
			get { return WinBase.IsTrue(User32.IsWindowVisible(m_hwnd)); }
		}

		#endregion

		public static List<Window> ListWindows()
		{
			var handles = new List<Window>();
			User32.EnumWindowsProc proc = delegate (IntPtr hwnd, IntPtr lParam)
			{
				handles.Add(new Window(hwnd));
				return WinBase.TRUE;
			};

			int ret = User32.EnumWindows(proc, new IntPtr(0));
			if (WinBase.IsFalse(ret))
				throw WinErrorException.LastErr();

			return handles;
		}

		public static List<Window> ListTopVisibleWindows()
		{
			var org = ListWindows();
			var filter = new List<Window>();
			foreach (var win in org)
			{
				if (win.Parent == null && win.Visible)
					filter.Add(win);
			}
			return filter;
		}
	}
}
