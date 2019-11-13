using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Json
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    public class JNodeAttribute : Attribute
    {
        public static bool IsJsonClass(Type t)
        {
            return t.IsDefined(typeof(JNodeAttribute), false);
        }
    }

    public class JFieldAttribute : Attribute
    {
        public string Name { get; }
        public bool IsProperty { get; }
        public bool NoPublic { get; }

        public JFieldAttribute(string name = null, bool noPublic=false, bool isProperty=false)
        {
            Name = name;
            NoPublic = noPublic;
            IsProperty = IsProperty;
        }
    }

    class JsonSerialization
    {

    }
}
