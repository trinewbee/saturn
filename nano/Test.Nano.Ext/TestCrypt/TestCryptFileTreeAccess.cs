using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nano.Storage;
using Nano.Crypt;
using Nano.UnitTest;

namespace TestExt
{
	class TestCryptFileTreeAccess
	{
		const string pathTest = "testCryptKeyValueAccess";

		AesCryptAgent m_aes;
		BlockCrypt m_ebc, m_dbc;

		FileTreeAccess m_pacc, m_acc;
		FileTreeItem m_proot, m_root;

		public TestCryptFileTreeAccess()
		{
			Test.InitDirectory(pathTest);

			byte[] key = new byte[] {
				1, 9, 8, 2, 1, 1, 2, 2, 1, 9, 8, 0, 0, 9, 0, 3
			};
			var tail = new XorTailTransform(key);
			m_aes = new AesCryptAgent(key, tail);
			m_ebc = m_aes.CreateEncrypt();
			m_dbc = m_aes.CreateDecrypt();

			m_pacc = new LocalFileTreeAccess(pathTest);
			m_proot = m_pacc.Root;

			m_acc = new CryptFileTreeAccess(m_dbc, m_pacc, m_proot);
			m_root = m_acc.Root;
		}

		void testAtomRead()
		{
			byte[] ss = Encoding.UTF8.GetBytes("Nice to meet you, it is a sample message.");
			Test.Assert(ss.Length == 41);
			byte[] st = m_ebc.TransformFinalBlock(ss);
			Test.Assert(st.Length == ss.Length);
			string name = "1";
			m_proot.AtomCreateChild(name, st);

			m_root.Refresh();
			var fi = m_root[name];

			st = fi.AtomRead();
			Test.AssertListEqual(ss, st);

			st = fi.AtomRead(12, 18);
			Test.Assert(st.Length == 18);
			Test.AssertRangeEqual(ss, 12, st, 0, 18);

			st = fi.AtomRead(20, 20);
			Test.Assert(st.Length == 20);
			Test.AssertRangeEqual(ss, 20, st, 0, 20);

			st = fi.AtomRead(0, 41);
			Test.AssertListEqual(ss, st);
		}

		void Dispose()
		{
			m_acc.Close();
			Directory.Delete(pathTest, true);
		}

		public static void Run()
		{
			Console.WriteLine("TestCrypt.TestCryptFileTreeAccess");
			TestCryptFileTreeAccess o = new TestCryptFileTreeAccess();
			o.testAtomRead();
			o.Dispose();
		}
	}
}
