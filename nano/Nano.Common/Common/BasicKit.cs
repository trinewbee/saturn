using System;

namespace Nano.Common
{	
	public static class BasicKit
	{
		public static void Release<T>(ref T obj, Action f) where T : class
		{
			if (obj != null)
			{
				f();
				obj = null;
			}
		}

		public static void Release<T>(ref T obj, Action<T> f) where T : class
		{
			if (obj != null)
			{
				f(obj);
				obj = null;
			}
		}

		public static void Dispose<T>(ref T obj) where T : IDisposable
		{
			if (obj != null)
			{
				obj.Dispose();
				obj = default;
			}
		}

		public static void ReleaseComObject<T>(ref T o) where T : class
		{
			if (o != null)
			{
                System.Runtime.InteropServices.Marshal.ReleaseComObject(o);
				o = null;
			}
		}

		// https://docs.microsoft.com/zh-cn/dotnet/standard/frameworks
		// 条件编译：目标框架
#if NET48
#endif
	}
}
