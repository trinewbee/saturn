using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using ArpanTECH;

namespace Nano.Crypt.Rsa
{
	public class RsaKeyPair
	{
		public byte[] N, E, D;

		public RsaKeyPair(byte[] _n, byte[] _e, byte[] _d)
		{
			N = _n;
			E = _e;
			D = _d;
		}

		public RsaKeyPair(RSAParameters prms)
		{
			N = prms.Modulus;
			E = prms.Exponent;
			D = prms.D;
		}

		public bool HasPrivateKey
		{
			get { return D != null; }
		}

		public RsaKeyPair Public
		{
			get { return D != null ? new RsaKeyPair(N, E, null) : this; }
		}
	}

	public class RsaUtility
	{
		RsaKeyPair m_key;
		RSAx m_rsax;

		public RsaUtility(RsaKeyPair key)
		{
			m_key = key;
			int keysize = key.N.Length * 8;
			RSAxParameters rsaxprms;
			if (key.D != null)
				rsaxprms = new RSAxParameters(key.N, key.E, key.D, keysize);
			else
				rsaxprms = new RSAxParameters(key.N, key.E, keysize);
			m_rsax = new RSAx(rsaxprms);
		}

		public byte[] Encrypt(byte[] data, bool fPrivate, bool fOAEP)
		{
			return m_rsax.Encrypt(data, fPrivate, fOAEP);
		}

		public byte[] Decrypt(byte[] data, bool fPrivate, bool fOAEP)
		{
			return m_rsax.Decrypt(data, fPrivate, fOAEP);
		}

		public RsaKeyPair KeyPair
		{
			get { return m_key; }
		}

		#region Key Generator

		// 1024 is a mininum key size for security
		// 2048 is suggested a better one
		// 4096 provides more security
		// but the higher will cause very low efficiency
		public static RSAParameters GenerateParameters(int keysize)
		{
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
			RSAParameters prms = rsa.ExportParameters(true);
			return prms;
		}

		public static RsaKeyPair GenerateKeyPair(int keysize)
		{
			RSAParameters prms = GenerateParameters(keysize);
			return new RsaKeyPair(prms);
		}

		#endregion
	}
}
