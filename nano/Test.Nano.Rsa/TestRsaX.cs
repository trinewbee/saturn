using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using Nano.Common;
using ArpanTECH;
using Nano.Crypt.Rsa;

namespace TestRsaUtility
{
	class TestRsaX
	{
		const string PLAIN_TEXT = "Hello, RSA";
		static byte[] PLAIN_DATA = Encoding.UTF8.GetBytes(PLAIN_TEXT);

		static void TestNetfxNormalPkcs()
		{
			RSACryptoServiceProvider rsaN = new RSACryptoServiceProvider(2048);
			Debug.Assert(rsaN.KeySize == 2048);

			// Encrypt using public key
			RSAParameters prms = rsaN.ExportParameters(false);
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
			rsa.ImportParameters(prms);
			byte[] cr = rsa.Encrypt(PLAIN_DATA, false);
			Debug.Assert(cr.Length == 256);

			// Decrypt using private key
			prms = rsaN.ExportParameters(true);
			rsa = new RSACryptoServiceProvider();
			rsa.ImportParameters(prms);
			byte[] tr = rsa.Decrypt(cr, false);
			Debug.Assert(tr.Length == PLAIN_TEXT.Length);
			Debug.Assert(Encoding.UTF8.GetString(tr) == PLAIN_TEXT);
		}

		static void TestNetfxNormalOAEP()
		{
			RSACryptoServiceProvider rsaN = new RSACryptoServiceProvider(2048);
			Debug.Assert(rsaN.KeySize == 2048);

			// Encrypt using public key
			RSAParameters prms = rsaN.ExportParameters(false);
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
			rsa.ImportParameters(prms);
			byte[] cr = rsa.Encrypt(PLAIN_DATA, true);
			Debug.Assert(cr.Length == 256);

			// Decrypt using private key
			prms = rsaN.ExportParameters(true);
			rsa = new RSACryptoServiceProvider();
			rsa.ImportParameters(prms);
			byte[] tr = rsa.Decrypt(cr, true);
			Debug.Assert(tr.Length == PLAIN_TEXT.Length);
			Debug.Assert(Encoding.UTF8.GetString(tr) == PLAIN_TEXT);
		}

		static void TestRsaxNormalPkcs()
		{
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
			RSAParameters prms = rsa.ExportParameters(true);

			// Encrypt using public key
			RSAxParameters rsaxprms = new RSAxParameters(prms.Modulus, prms.Exponent, 2048);
			RSAx rsax = new RSAx(rsaxprms);
			byte[] cr = rsax.Encrypt(PLAIN_DATA, false, false);
			Debug.Assert(cr.Length == 256);

			// Decrypt using private key
			rsaxprms = new RSAxParameters(prms.Modulus, prms.Exponent, prms.D, 2048);
			rsax = new RSAx(rsaxprms);
			byte[] tr = rsax.Decrypt(cr, true, false);
			Debug.Assert(BinaryValue.IsEqual(tr, PLAIN_DATA));
		}

		static void TestRsaxNormalOAEP()
		{
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
			RSAParameters prms = rsa.ExportParameters(true);

			// Encrypt using public key
			RSAxParameters rsaxprms = new RSAxParameters(prms.Modulus, prms.Exponent, 2048);
			RSAx rsax = new RSAx(rsaxprms);
			byte[] cr = rsax.Encrypt(PLAIN_DATA, false, true);
			Debug.Assert(cr.Length == 256);

			// Decrypt using private key
			rsaxprms = new RSAxParameters(prms.Modulus, prms.Exponent, prms.D, 2048);
			rsax = new RSAx(rsaxprms);
			byte[] tr = rsax.Decrypt(cr, true, true);
			Debug.Assert(BinaryValue.IsEqual(tr, PLAIN_DATA));
		}

		static void TestRsaxPrivateEncryptOAEP()
		{
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
			RSAParameters prms = rsa.ExportParameters(true);

			// Encrypt using private key
			RSAxParameters rsaxprms = new RSAxParameters(prms.Modulus, prms.Exponent, prms.D, 2048);
			RSAx rsax = new RSAx(rsaxprms);
			byte[] cr = rsax.Encrypt(PLAIN_DATA, true, true);
			Debug.Assert(cr.Length == 256);

			// Decrypt using public key
			rsaxprms = new RSAxParameters(prms.Modulus, prms.Exponent, 2048);
			rsax = new RSAx(rsaxprms);
			byte[] tr = rsax.Decrypt(cr, false, true);
			Debug.Assert(BinaryValue.IsEqual(tr, PLAIN_DATA));
		}

		static void TestMixedPkcs()
		{
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
			RSAParameters prms = rsa.ExportParameters(true);
			RSAxParameters rsaxprms = new RSAxParameters(prms.Modulus, prms.Exponent, prms.D, 2048);
			RSAx rsax = new RSAx(rsaxprms);

			// Encrypt using public key by RSAx
			byte[] cr = rsax.Encrypt(PLAIN_DATA, false, false);

			// Decrypt using private key by .netfx class
			byte[] tr = rsa.Decrypt(cr, false);
			Debug.Assert(BinaryValue.IsEqual(tr, PLAIN_DATA));

			// Encrypt using public key by .netfx class
			cr = rsa.Encrypt(PLAIN_DATA, false);

			// Decrypt using private key by RSAx
			tr = rsax.Decrypt(cr, false);
			Debug.Assert(BinaryValue.IsEqual(tr, PLAIN_DATA));
		}

		static void TestMixedOAEP()
		{
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
			RSAParameters prms = rsa.ExportParameters(true);
			RSAxParameters rsaxprms = new RSAxParameters(prms.Modulus, prms.Exponent, prms.D, 2048);
			RSAx rsax = new RSAx(rsaxprms);

			// Encrypt using public key by RSAx
			byte[] cr = rsax.Encrypt(PLAIN_DATA, false, true);

			// Decrypt using private key by .netfx class
			byte[] tr = rsa.Decrypt(cr, true);
			Debug.Assert(BinaryValue.IsEqual(tr, PLAIN_DATA));

			// Encrypt using public key by .netfx class
			cr = rsa.Encrypt(PLAIN_DATA, true);

			// Decrypt using private key by RSAx
			tr = rsax.Decrypt(cr, true);
			Debug.Assert(BinaryValue.IsEqual(tr, PLAIN_DATA));
		}

		static void TestPresetKeyPair()
		{
			Nano.Crypt.Rsa.RsaKeyPair key = Program.PRESET_KEY;
			RSAxParameters rsaxprms = new RSAxParameters(key.N, key.E, key.D, 2048);
			RSAx rsax = new RSAx(rsaxprms);

			// Encrypt using public key (PKCS)
			byte[] cr = rsax.Encrypt(PLAIN_DATA, false, false);
			byte[] tr = rsax.Decrypt(cr, true, false);
			Debug.Assert(BinaryValue.IsEqual(PLAIN_DATA, tr));

			// Encrypt using public key (OAEP)
			cr = rsax.Encrypt(PLAIN_DATA, false, true);
			tr = rsax.Decrypt(cr, true, true);
			Debug.Assert(BinaryValue.IsEqual(PLAIN_DATA, tr));

			// Encrypt using private key (PKCS)
			cr = rsax.Encrypt(PLAIN_DATA, true, false);
			tr = rsax.Decrypt(cr, false, false);
			Debug.Assert(BinaryValue.IsEqual(PLAIN_DATA, tr));

			// Encrypt using private key (OAEP)
			cr = rsax.Encrypt(PLAIN_DATA, true, true);
			tr = rsax.Decrypt(cr, false, true);
			Debug.Assert(BinaryValue.IsEqual(PLAIN_DATA, tr));
		}

		static void TestRsaUtil()
		{
			// rsaPublic is a demostration exporting public key
			// for this case itself, rsaPrivate can fit all operations
			RsaUtility rsaPrivate = new RsaUtility(Program.PRESET_KEY);
			RsaUtility rsaPublic = new RsaUtility(rsaPrivate.KeyPair.Public);

			// Encrypt using public key (PKCS)
			byte[] cr = rsaPublic.Encrypt(PLAIN_DATA, false, false);
			byte[] tr = rsaPrivate.Decrypt(cr, true, false);
			Debug.Assert(BinaryValue.IsEqual(PLAIN_DATA, tr));

			// Encrypt using public key (OAEP)
			cr = rsaPublic.Encrypt(PLAIN_DATA, false, true);
			tr = rsaPrivate.Decrypt(cr, true, true);
			Debug.Assert(BinaryValue.IsEqual(PLAIN_DATA, tr));

			// Encrypt using private key (PKCS)
			cr = rsaPrivate.Encrypt(PLAIN_DATA, true, false);
			tr = rsaPublic.Decrypt(cr, false, false);
			Debug.Assert(BinaryValue.IsEqual(PLAIN_DATA, tr));

			// Encrypt using private key (OAEP)
			cr = rsaPrivate.Encrypt(PLAIN_DATA, true, true);
			tr = rsaPublic.Decrypt(cr, false, true);
			Debug.Assert(BinaryValue.IsEqual(PLAIN_DATA, tr));
		}

		public static void Run()
		{
			TestNetfxNormalPkcs();
			TestNetfxNormalOAEP();
			TestRsaxNormalPkcs();
			TestRsaxNormalOAEP();
			TestRsaxPrivateEncryptOAEP();
			TestMixedPkcs();
			TestMixedOAEP();
			TestPresetKeyPair();
			TestRsaUtil();
		}
	}
}
