using Nano.Nuts;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Puff.NetCore
{
    public class FilterLog
    {
        public static string[] filterKeys = { };
        public static void Init(string[] _filterKeys)
        {
            filterKeys = _filterKeys;
        }

        public static string Filter(string value)
        {
            if (value == null || value.Length == 0)
                return "";
            var strJn = DObject.ImportJson(value);
            foreach (var key in filterKeys)
            {
                if (strJn.IsNull())
                    continue;
                if (strJn.HasKey(key))
                {
                    var jn = strJn.ToJson();
                    jn.DeleteChildItem(key);
                    strJn = DObject.ImportJson(jn);

                }
            }            
            return strJn.ToString();
        }
        public string Md5DigestB64(byte[] data)
        {
            return BinToB64(Md5Digest(data));
        }

        public byte[] Md5Digest(byte[] data)
        {
            MD5 m = MD5.Create();
            return m.ComputeHash(data);
        }

        public string BinToB64(byte[] data)
        {
            return Convert.ToBase64String(data);
        }
    }
}
