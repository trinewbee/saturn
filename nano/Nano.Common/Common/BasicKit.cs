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
	}
}
