using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nano.Collection;

namespace Nano.UnitTest
{
	public static class Test
	{
        public static int Fails = 0;

		#region Assertion

		public static void Fail(string message = "Assertion failed")
		{
            ++Fails;
			Console.WriteLine(message);
            System.Diagnostics.Debug.Fail(message);
		}

		public static void Assert(bool f, string message = "Assertion failed")
		{
			if (!f)
				Fail(message);
		}

		public static void AssertException(Action action, Type et = null)
		{
			try
			{
				action();
				Fail("MissingException");
			}
			catch (Exception e)
			{
				Assert(et == null || e.GetType() == et, "WrongExceptionType");
			}
		}

		public static void AssertExceptionType<T>(Action action) where T : Exception
		{
			try
			{
				action();
				Fail("MissingException");
			}
			catch (T)
			{
			}
			catch
			{
				Fail("WrongExceptionType");
			}
		}

		#endregion

		#region Array operations

		public static void AssertRangeEqual<T>(IList<T> x, int off_x, IList<T> y, int off_y, int count) where T : IComparable =>
			Assert(CollectionKit.CompareRange(x, off_x, y, off_y, count));

		public static void AssertListEqual<T>(IList<T> x, IList<T> y) where T : IComparable => Assert(CollectionKit.CompareList(x, y));

		public static void AssertRangeEqual<T>(IList<T> x, int off_x, IList<T> y, int off_y, int count, Comparison<T> cmp) =>
			Assert(CollectionKit.CompareRange(x, off_x, y, off_y, count, cmp));

		public static void AssertListEqual<T>(IList<T> x, IList<T> y, Comparison<T> cmp) => Assert(CollectionKit.CompareList(x, y, cmp));

		#endregion

		#region Random sequence

		public static void MakeData(byte[] data, int off, int size, byte seed)
		{
			for (int i = 0; i < size; ++i)
			{
				data[off + i] = seed;
				++seed;
			}
		}

		public static byte[] MakeData(int size, byte seed)
		{
			byte[] data = new byte[size];
			MakeData(data, 0, size, seed);
			return data;
		}

		public static void WriteData(Stream stream, int size, byte seed)
		{
			byte[] data = MakeData(size, seed);
			stream.Write(data, 0, size);
		}

		public static byte[] ReadData(Stream stream)
		{
			Test.Assert(stream.Length < int.MaxValue);
			byte[] data = new byte[(int)stream.Length];
			stream.Seek(0, SeekOrigin.Begin);
			if (stream.Read(data, 0, data.Length) != data.Length)
				throw new IOException("Read failed");
			return data;
		}

		public static bool VerifyData(byte[] data, int off, int size, byte seed)
		{
			for (int i = 0; i < size; ++i)
			{
				if (data[off + i] != seed)
					return false;
				++seed;
			}
			return true;
		}

		public static bool VerifyData(Stream stream, int size, byte seed)
		{
			if (stream.Length != size)
				return false;

			byte[] data = ReadData(stream);
			return VerifyData(data, 0, size, seed);
		}

		public static void AssertVerifyData(byte[] data, int off, int size, byte seed)
		{
			Assert(VerifyData(data, off, size, seed));
		}

		public static void AssertVerifyData(Stream istream, int size, byte seed)
		{
			Assert(VerifyData(istream, size, seed));
		}

		#endregion

		#region Environment

		public static void InitDirectory(string pathTest)
		{
			if (Directory.Exists(pathTest))
				Directory.Delete(pathTest, true);
			Directory.CreateDirectory(pathTest);
		}

		#endregion

        public static void Report()
        {
            Console.WriteLine(Fails != 0 ? $"{Fails} error(s)" : "succeeded");
        }
	}
}
