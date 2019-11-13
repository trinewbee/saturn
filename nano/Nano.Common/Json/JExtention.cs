using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Json.Ext
{
    public static class JExtention
    {
        public static string Dump(this JNode node)
        {
            var jfmt = new JFormater();
            return jfmt.Format(node);
        }

        /// <summary>
        /// deep copy
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static JNode Clone(this JNode src)
        {            
            if (src.IsArray)
            {
                var ret = new JNode(src.Type);
                foreach (var n in src.Items)
                    ret.Add(Clone(n));

                return ret;
            }
            else if (src.IsObject)
            {
                var ret = new JNode(src.Type);
                foreach (var f in src.Fields)
                    ret[f.Name] = Clone(f.Value);

                return ret;
            }

            return src;
        }

        public static bool TryGetEnum<TEnum>(this JNode node, out TEnum ret, bool ignoreCase = false) where TEnum : struct
        {
            return Enum.TryParse(node.TextValue, ignoreCase, out ret);
        }

        public static TEnum GetEnum<TEnum>(this JNode node, TEnum def = default(TEnum), bool ignoreCase = false) where TEnum : struct
        {
            TEnum ret;
            if (Enum.TryParse(node.TextValue, ignoreCase, out ret))
                return ret;
            return def;
        }
    }
}
