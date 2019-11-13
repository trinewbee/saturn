using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Nano.Win32
{
	public static class WinBase
	{
		public const int TRUE = 1, FALSE = 0;

		public const uint ERROR_SUCCESS = 0;

		public static bool IsTrue(int x) => x != 0;
		public static bool IsFalse(int x) => x == 0;

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;
		}

		public const uint PROCESS_TERMINATE = 0x0001;
		public const uint PROCESS_CREATE_THREAD = 0x0002;
		public const uint PROCESS_SET_SESSIONID = 0x0004;
		public const uint PROCESS_VM_OPERATION = 0x0008;
		public const uint PROCESS_VM_READ = 0x0010;
		public const uint PROCESS_VM_WRITE = 0x0020;
		public const uint PROCESS_DUP_HANDLE = 0x0040;
		public const uint PROCESS_CREATE_PROCESS = 0x0080;
		public const uint PROCESS_SET_QUOTA = 0x0100;
		public const uint PROCESS_SET_INFORMATION = 0x0200;
		public const uint PROCESS_QUERY_INFORMATION = 0x0400;
		public const uint PROCESS_SUSPEND_RESUME = 0x0800;
		public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
		public const uint PROCESS_SET_LIMITED_INFORMATION = 0x2000;

	}

	public class WinErrorException : Exception
	{
		public uint Err;

		public WinErrorException(uint err)
		{
			Err = err;
		}

		public static WinErrorException LastErr()
		{
			uint err = Kernel32.GetLastError();
			return new WinErrorException(err);
		}
	}
}
