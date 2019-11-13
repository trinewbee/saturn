using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Storage;
using Nano.Storage.Common;
using Nano.Crypt;
using Nano.UnitTest;

namespace TestExt
{
	class TestCryptKeyValueAccess
	{
		const string pathTest = "testCryptKeyValueAccess";

		AesCryptAgent m_aes;
		BlockCrypt m_ebc, m_dbc;

		SimpleKeyValueAccess m_pacc;
		CryptKeyValueAccess m_acc;

		class MyDivider : SingleLayerKeyValueDivider
		{
			int m_n;
			List<string> m_bucks;

			public MyDivider(int n)
			{
				m_n = n;
				m_bucks = new List<string>();
				for (int i = 0; i < m_n; ++i)
					m_bucks.Add(i.ToString());
			}

			public string GetBucket(string key) => m_bucks[key.GetHashCode() % m_n];

			public List<string> ListBuckets() => m_bucks;
		}

		public TestCryptKeyValueAccess()
		{
			Test.InitDirectory(pathTest);

			byte[] key = new byte[] {
				1, 9, 8, 2, 1, 1, 2, 2, 1, 9, 8, 0, 0, 9, 0, 3
			};
			var tail = new XorTailTransform(key);
			m_aes = new AesCryptAgent(key, tail);
			m_ebc = m_aes.CreateEncrypt();
			m_dbc = m_aes.CreateDecrypt();

			var afs = new LocalFileTreeAccess(pathTest);
			var loc = new SingleLayerKeyValueLocator(new MyDivider(4));
			loc.InitBuckets(afs.Root);

			m_pacc = new SimpleKeyValueAccess(afs, afs.Root, loc);
			m_acc = new CryptKeyValueAccess(m_pacc, m_ebc, m_dbc);
		}

		void testAtomRead()
		{
			byte[] ss = Encoding.UTF8.GetBytes("Nice to meet you, it is a sample message.");
			Test.Assert(ss.Length == 41);
			byte[] st = m_ebc.TransformFinalBlock(ss);
			Test.Assert(st.Length == ss.Length);
			string name = "1";
			m_pacc.AtomCreate(name, st);

			st = m_acc.AtomRead(name);
			Test.AssertListEqual(ss, st);

			st = m_acc.AtomRead(name, 12, 18);
			Test.Assert(st.Length == 18);
			Test.AssertRangeEqual(ss, 12, st, 0, 18);

			st = m_acc.AtomRead(name, 20, 20);
			Test.Assert(st.Length == 20);
			Test.AssertRangeEqual(ss, 20, st, 0, 20);

			st = m_acc.AtomRead(name, 0, 41);
			Test.AssertListEqual(ss, st);
		}

		void testAtomCreate()
		{
			byte[] ss = Encoding.UTF8.GetBytes("Nice to meet you, it is another sample message.");
			string name = "2";
			var item = m_acc.AtomCreate(name, new MemoryStream(ss));
			Debug.Assert(item.Size == ss.Length);

			byte[] st = m_acc.AtomRead(name);
			Test.AssertListEqual(ss, st);
		}

		void Dispose()
		{
			m_acc.Close();
			Directory.Delete(pathTest, true);
		}

		public static void Run()
		{
			Console.WriteLine("TestCrypt.TestCryptKeyValueAccess");
			TestCryptKeyValueAccess o = new TestCryptKeyValueAccess();
			o.testAtomRead();
			o.testAtomCreate();
			o.Dispose();
		}
	}
}
