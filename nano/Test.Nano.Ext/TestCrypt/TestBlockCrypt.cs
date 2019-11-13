using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Nano.Common;
using Nano.Collection;
using Nano.Crypt;
using Nano.UnitTest;

namespace TestExt
{
	class TestBlockCrypt
	{
		AesCryptAgent m_aes;
		BlockCrypt m_ebc, m_dbc;
		byte[] m_pdata;
		MemoryStream m_plain, m_crypt;

		TestBlockCrypt()
		{
			m_pdata = Encoding.UTF8.GetBytes("Hello, my best friends");
			m_plain = new MemoryStream(m_pdata);
			Test.Assert(m_plain.Length == 22);

			byte[] key = new byte[16];
			PlainTailTransform tail = new PlainTailTransform();
			m_aes = new AesCryptAgent(key, tail);

			m_ebc = m_aes.CreateEncrypt();
			m_dbc = m_aes.CreateDecrypt();

			m_crypt = new MemoryStream();
		}

		void TestCreateAesCryptAgent()
		{
			// with plain tail transform
			byte[] key = new byte[16];
			AesCryptAgent aes = new AesCryptAgent(key, new PlainTailTransform());
			var d = aes.CreateEncrypt();
			byte[] cdata = d.TransformFinalBlock(m_pdata);
			string c_str = BinaryValue.ToHexStringLower(cdata);
			Test.Assert(c_str == "7bf71972f5d4ea12677cc7c34c73f75e7269656e6473");
			byte[] magic = d.TransformFinalBlock(new byte[16]); // used later

			d = aes.CreateDecrypt();
			byte[] tdata = d.TransformFinalBlock(cdata);
			Test.AssertListEqual(tdata, m_pdata);

			// with xor tail transform
			aes = new AesCryptAgent(key, new XorTailTransform(magic));
			d = aes.CreateEncrypt();
			cdata = d.TransformFinalBlock(m_pdata);
			c_str = BinaryValue.ToHexStringLower(cdata);
			Test.Assert(c_str == "7bf71972f5d4ea12677cc7c34c73f75e14802eba8bf9");

			d = aes.CreateDecrypt();
			tdata = d.TransformFinalBlock(cdata);
			Test.AssertListEqual(tdata, m_pdata);
		}

		void TestCreateAesCryptAgentWithMagic()
		{
			byte[] key = new byte[16];
			AesCryptAgent aes = new AesCryptAgent(key, (byte[])null);
			var d = aes.CreateEncrypt();
			byte[] cdata = d.TransformFinalBlock(m_pdata);
			string c_str = BinaryValue.ToHexStringLower(cdata);
			Test.Assert(c_str == "7bf71972f5d4ea12677cc7c34c73f75e14802eba8bf9");

			aes = new AesCryptAgent(key, new byte[16]);
			d = aes.CreateDecrypt();
			byte[] tdata = d.TransformFinalBlock(cdata);
			Test.AssertListEqual(tdata, m_pdata);
		}

		void TestCryptStream()
		{
			m_ebc.TransformStream(m_plain, m_crypt);
			Test.Assert(m_crypt.Length == m_plain.Length);
			string crypt_str = BinaryValue.ToHexString(m_crypt.ToArray()).ToLowerInvariant();
			Test.Assert(crypt_str == "7bf71972f5d4ea12677cc7c34c73f75e7269656e6473");

			MemoryStream decrypt = new MemoryStream();
			m_dbc.TransformStream(m_crypt, decrypt);
			byte[] decrypt_data = decrypt.ToArray();
			string decrypt_str = BinaryValue.ToHexString(decrypt_data).ToLowerInvariant();
			Test.Assert(decrypt_str == "48656c6c6f2c206d79206265737420667269656e6473");
			Test.AssertListEqual(decrypt_data, m_pdata);
		}

		void TestComputHash()
		{
			SHA1 hash = SHA1.Create();

			m_plain.Seek(0, SeekOrigin.Begin);
			byte[] sha1c = m_dbc.ComputeHash(m_crypt, hash);
			Test.Assert(BinaryValue.ToHexString(sha1c) == "0EE7DCEA0E491D58D162E77882CF74E332D4561A");
		}

		void TestReadPart()
		{
			byte[] t = m_dbc.ReadPart(m_crypt, 0, 22);
			Test.AssertListEqual(t, m_pdata);

			t = m_dbc.ReadPart(m_crypt, 0, 512);
			Test.AssertListEqual(t, m_pdata);

			t = m_dbc.ReadPart(m_crypt, 4, 8);
			Test.AssertListEqual(t, CollectionKit.ToArray(m_pdata, 4, 8));

			t = m_dbc.ReadPart(m_crypt, 8, 16);
			Test.AssertListEqual(t, CollectionKit.ToArray(m_pdata, 8, 14));

			t = m_dbc.ReadPart(m_crypt, 17, 4);
			Test.AssertListEqual(t, CollectionKit.ToArray(m_pdata, 17, 4));

			t = m_dbc.ReadPart(m_crypt, 17, 8);
			Test.AssertListEqual(t, CollectionKit.ToArray(m_pdata, 17, 5));
		}

		public static void Run()
		{
			Console.WriteLine("TestCrypt.TestBlockCrypt");
			TestBlockCrypt o = new TestBlockCrypt();
			o.TestCreateAesCryptAgent();
			o.TestCreateAesCryptAgentWithMagic();
            o.TestCryptStream();
			o.TestComputHash();
			o.TestReadPart();
		}
	}
}
