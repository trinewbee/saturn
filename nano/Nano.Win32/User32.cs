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

		// BOOL EnumChildWindows(HWND hWndParent, WNDENUMPROC lpEnumFunc, LPARAM lParam);
		[DllImport("user32.dll")]
		public static extern int EnumChildWindows(IntPtr hWndParent, EnumWindowsProc func, IntPtr lParam);

		// BOOL WINAPI IsWindowVisible(_In_ HWND hWnd);
		[DllImport("user32.dll")]
		public static extern int IsWindowVisible(IntPtr hwnd);

		// int WINAPI GetWindowText(_In_ HWND hWnd, _Out_ LPTSTR lpString, _In_ int nMaxCount);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetWindowText(IntPtr hwnd, StringBuilder lptrString, int nMaxCount);

		// int GetClassName(HWND hWnd, LPTSTR lpClassName, int nMaxCount);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

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

		// HWND GetForegroundWindow();
		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		// BOOL SetForegroundWindow(HWND hWnd);
		[DllImport("user32.dll")]
		public static extern int SetForegroundWindow(IntPtr hWnd);

		// BOOL SetWindowText(HWND hWnd, LPCWSTR lpString);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int SetWindowText(IntPtr hWnd, string lpString);

		#endregion

		// BOOL WINAPI GetCursorPos(_Out_ LPPOINT lpPoint);
		[DllImport("user32.dll")]
		public static extern int GetCursorPos(ref WinBase.POINT lpPoint);

		// HWND GetDlgItem(HWND hDlg, int nIDDlgItem);
		[DllImport("user32.dll")]
		public static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

		#region Message

		// BOOL PostMessageW(HWND hWnd, UINT Msg, WPARAM wParam, LPARAM lParam);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int PostMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

		// LRESULT WINAPI SendMessage(_In_ HWND hWnd, _In_ UINT Msg, _In_ WPARAM wParam, _In_ LPARAM lParam);
		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, UIntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, uint wParam, uint lParam);

		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, UIntPtr wParam, string lParam);

		#endregion

		#region WM_ constants

		public const uint WM_SETTEXT = 0x000C;
		public const uint WM_GETTEXT = 0x000D;
		public const uint WM_GETTEXTLENGTH = 0x000E;

		public const uint WM_COMMAND = 0x0111;

		public const uint WM_KEYDOWN = 0x0100;
		public const uint WM_KEYUP = 0x0101;
		public const uint WM_CHAR = 0x0102;
		public const uint WM_DEADCHAR = 0x0103;
		public const uint WM_SYSKEYDOWN = 0x0104;
		public const uint WM_SYSKEYUP = 0x0105;
		public const uint WM_SYSCHAR = 0x0106;

		#endregion

		#region BN_ constants

		public const int BN_CLICKED = 0;
		public const int BN_PAINT = 1;
		public const int BN_HILITE = 2;
		public const int BN_UNHILITE = 3;
		public const int BN_DISABLE = 4;
		public const int BN_DOUBLECLICKED = 5;
		public const int BN_PUSHED = BN_HILITE;
		public const int BN_UNPUSHED = BN_UNHILITE;
		public const int BN_DBLCLK = BN_DOUBLECLICKED;
		public const int BN_SETFOCUS = 6;
		public const int BN_KILLFOCUS = 7;

		#endregion

		#region Menu

		// HMENU GetMenu(HWND hWnd);
		[DllImport("user32.dll")]
		public static extern IntPtr GetMenu(IntPtr hWnd);

		// int GetMenuItemCount(HMENU hMenu);
		[DllImport("user32.dll")]
		public static extern int GetMenuItemCount(IntPtr hMenu);

		// int GetMenuStringW(HMENU hMenu, UINT uIDItem, LPWSTR lpString, int cchMax, UINT flags);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetMenuString(IntPtr hMenu, uint uIDItem, StringBuilder lpString, int cchMax, uint flags);
		public const uint MF_BYCOMMAND = 0x00000000u;
		public const uint MF_BYPOSITION = 0x00000400u;

		// HMENU GetSubMenu(HMENU hMenu, int nPos);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr GetSubMenu(IntPtr hMenu, int nPos);

		// UINT GetMenuItemID(HMENU hMenu, int nPos);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern uint GetMenuItemID(IntPtr hMenu, int nPos);

		// BOOL GetMenuItemInfoW(HMENU hmenu, UINT item, BOOL fByPosition, LPMENUITEMINFOW lpmii);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetMenuItemInfo(IntPtr hMenu, uint item, int fByPosition, ref MENUITEMINFO lpmii);
		public const uint MIIM_STATE = 0x00000001;
		public const uint MIIM_ID = 0x00000002;
		public const uint MIIM_SUBMENU = 0x00000004;
		public const uint MIIM_CHECKMARKS = 0x00000008;
		public const uint MIIM_TYPE = 0x00000010;
		public const uint MIIM_DATA = 0x00000020;
		public const uint MIIM_STRING = 0x00000040;
		public const uint MIIM_BITMAP = 0x00000080;
		public const uint MIIM_FTYPE = 0x00000100;

		public const uint MFT_OWNERDRAW = 0x100;
		public const uint MFT_SEPARATOR = 0x800;

		#endregion

		// DWORD WINAPI GetWindowThreadProcessId(_In_ HWND hWnd, _Out_opt_ LPDWORD lpdwProcessId);
		[DllImport("user32.dll")]
		public static extern uint GetWindowThreadProcessId(IntPtr hwnd, ref uint processId);
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct MENUITEMINFO
	{
		public uint cbSize;
		public uint fMask;
		public uint fType;
		public uint fState;
		public uint wID;
		public IntPtr hSubMenu;
		public IntPtr hbmpChecked;
		public IntPtr hbmpUnchecked;
		public IntPtr dwItemData;
		public String dwTypeData;
		public uint cch;
		public IntPtr hbmpItem;
		public static uint Size() => (uint)Marshal.SizeOf(typeof(MENUITEMINFO));
	}

	public class Window
	{
		public IntPtr hWnd;	// HWND

		public Window(IntPtr hwnd)
		{
			hWnd = hwnd;
		}

		#region Properties

		public Window Parent
		{
			get
			{
				var phwnd = User32.GetParent(hWnd);
				return phwnd !=  IntPtr.Zero ? new Window(phwnd) : null;
			}
		}

		public string Text
		{
			get
			{
				StringBuilder sb = new StringBuilder(1024);
				int n = User32.GetWindowText(hWnd, sb, sb.Capacity - 1);

				if (n == 0)
					return null;	// ignore error

				var s = sb.ToString();
				Debug.Assert(s.Length == n);
				return s;
			}
		}

		public bool Visible => WinBase.IsTrue(User32.IsWindowVisible(hWnd));

		#endregion

		public string GetClassName()
        {
			StringBuilder sb = new StringBuilder(1024);
			int n = User32.GetClassName(hWnd, sb, sb.Capacity - 1);
			var s = sb.ToString();
			Debug.Assert(s.Length == n);
			return s;
		}

		public static List<Window> ListWindows()
		{
			var handles = new List<Window>();
			User32.EnumWindowsProc proc = delegate (IntPtr hwnd, IntPtr lParam)
			{
				handles.Add(new Window(hwnd));
				return WinBase.TRUE;
			};

			int ret = User32.EnumWindows(proc, IntPtr.Zero);
			if (WinBase.IsFalse(ret))
				throw WinErrorException.LastErr();

			return handles;
		}

		public static List<Window> ListChildWindows(IntPtr hwndParent)
        {
			var handles = new List<Window>();
			User32.EnumWindowsProc proc = delegate (IntPtr hwnd, IntPtr lParam)
			{
				handles.Add(new Window(hwnd));
				return WinBase.TRUE;
			};

			int ret = User32.EnumChildWindows(hwndParent, proc, IntPtr.Zero);
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
