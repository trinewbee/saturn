using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Cbfs.Test
{
    public class TestWildcardMatch
    {
        WildcardMatch m_cwm;

        static void Assert(bool f)
        {
            System.Diagnostics.Debug.Assert(f);
        }

        void setUp()
        {
            m_cwm = new WildcardMatch();
        }

        void testInit()
        {
            m_cwm.Init("*");
            Assert(m_cwm.m_lst && m_cwm.m_rst && m_cwm.m_subs.Count == 0);

            m_cwm.Init("ab");
            Assert(!m_cwm.m_lst && !m_cwm.m_rst && m_cwm.m_subs.Count == 1 && m_cwm.m_subs[0] == "ab");

            m_cwm.Init("a*");
            Assert(!m_cwm.m_lst && m_cwm.m_rst && m_cwm.m_subs.Count == 1 && m_cwm.m_subs[0] == "a");

            m_cwm.Init("*er");
            Assert(m_cwm.m_lst && !m_cwm.m_rst && m_cwm.m_subs.Count == 1 && m_cwm.m_subs[0] == "er");

            m_cwm.Init("*.*");
            Assert(m_cwm.m_lst && m_cwm.m_rst && m_cwm.m_subs.Count == 1 && m_cwm.m_subs[0] == ".");

            m_cwm.Init("a*?*b");
            Assert(!m_cwm.m_lst && !m_cwm.m_rst && m_cwm.m_subs.Count == 3 && m_cwm.m_subs[0] == "a" && m_cwm.m_subs[1] == "?" && m_cwm.m_subs[2] == "b");

            m_cwm.Init("*a?b*");
            Assert(m_cwm.m_lst && m_cwm.m_rst && m_cwm.m_subs.Count == 1 && m_cwm.m_subs[0] == "a?b");

            m_cwm.Init("???-*.log");
            Assert(!m_cwm.m_lst && !m_cwm.m_rst && m_cwm.m_subs.Count == 2 && m_cwm.m_subs[0] == "???-" && m_cwm.m_subs[1] == ".log");

            m_cwm.Init("**");
            Assert(m_cwm.m_lst && m_cwm.m_rst && m_cwm.m_subs.Count == 0);
        }

        void testMatchSimple()
        {
            m_cwm.Init("*");
            Assert(m_cwm.Match(""));
            Assert(m_cwm.Match("abc"));

            m_cwm.Init("");
            Assert(m_cwm.Match(""));
            Assert(!m_cwm.Match("abc"));

            m_cwm.Init("ab");
            Assert(m_cwm.Match("ab"));
            Assert(!m_cwm.Match("abc"));
            Assert(!m_cwm.Match("cab"));

            m_cwm.Init("ab*");
            Assert(m_cwm.Match("ab"));
            Assert(m_cwm.Match("abc"));
            Assert(!m_cwm.Match("cab"));

            m_cwm.Init("*ab");
            Assert(m_cwm.Match("ab"));
            Assert(!m_cwm.Match("abc"));
            Assert(m_cwm.Match("cab"));

            m_cwm.Init("a*a");
            Assert(m_cwm.Match("aa"));
            Assert(!m_cwm.Match("a"));
            Assert(m_cwm.Match("aaa"));
            Assert(m_cwm.Match("aba"));
        }

        void testMatchMulti()
        {
            m_cwm.Init("*ab*");
            Assert(m_cwm.Match("ab"));
            Assert(m_cwm.Match("abc"));
            Assert(m_cwm.Match("cab"));
            Assert(m_cwm.Match("cabc"));

            m_cwm.Init("a*b*a");
            Assert(!m_cwm.Match("aa"));
            Assert(m_cwm.Match("aba"));
            Assert(m_cwm.Match("acbca"));
            Assert(!m_cwm.Match("abab"));
            Assert(!m_cwm.Match("baba"));

            m_cwm.Init("*a*a*");
            Assert(!m_cwm.Match("cat"));
            Assert(m_cwm.Match("caat"));
            Assert(m_cwm.Match("cabat"));
            Assert(m_cwm.Match("caaaat"));
            Assert(m_cwm.Match("aoa"));

            m_cwm.Init("*-*-*.*");
            Assert(m_cwm.Match("--."));
            Assert(m_cwm.Match("1-1-1.o"));
            Assert(!m_cwm.Match("1-111.o"));
        }

        void testMatchQues()
        {
            m_cwm.Init("c?t");
            Assert(!m_cwm.Match("ct"));
            Assert(m_cwm.Match("CAT"));

            m_cwm.Init("?*?");
            Assert(!m_cwm.Match("a"));
            Assert(m_cwm.Match("aa"));
            Assert(m_cwm.Match("aaa"));
            Assert(m_cwm.Match("aaaa"));

            m_cwm.Init("*?*");
            Assert(m_cwm.Match("a"));

            m_cwm.Init("*????*????*");
            Assert(m_cwm.Match("11112222"));
            Assert(!m_cwm.Match("1111222"));
        }

        public static void Run()
        {
            var o = new TestWildcardMatch();
            o.setUp();
            o.testInit();
            o.testMatchSimple();
            o.testMatchMulti();
            o.testMatchQues();
        }
    }
}
