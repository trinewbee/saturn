using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Nano.Cbfs
{
    public class WildcardMatch
    {
        internal string m_pat;
        internal List<string> m_subs;
        internal bool m_lst, m_rst;

        public void Init(string pat)
        {
            m_pat = pat;
            m_lst = m_rst = false;
            m_subs = new List<string>();

            int pos = 0, n = pat.Length;
            for (; pos < n; ++pos)
            {
                if (pat[pos] == '*')
                    m_lst = true;
                else
                    break;
            }
            for (; n > 0; --n)
            {
                if (pat[n - 1] == '*')
                    m_rst = true;
                else
                    break;
            }
            while (pos < n)
            {
                StringBuilder sub = new StringBuilder();
                for (; pos < n; ++pos)
                {
                    if (pat[pos] == '*')
                        break;
                    else
                        sub.Append(ToLower(pat[pos]));
                }
                for (; pos < n; ++pos)
                {
                    if (pat[pos] != '*')
                        break;
                }
                m_subs.Add(sub.ToString());
            }

        }

        public bool Match(string str)
        {
            if (m_subs.Count == 0)
                return MatchZero(str);

            int lc = 0, hc = str.Length;
            int li = 0, hi = m_subs.Count;
            if (!m_lst)
            {
                if (!MatchExact(str, 0, m_subs[0]))
                    return false;

                li = 1;
                lc = m_subs[0].Length;

                if (m_subs.Count == 1)
                {
                    if (!m_rst)
                        return lc == str.Length;
                    else
                        return true;
                }
            }
            if (!m_rst)
            {
                string sub = m_subs[m_subs.Count - 1];
                if (!MatchExact(str, str.Length - sub.Length, sub))
                    return false;

                hi = m_subs.Count - 1;
                hc = str.Length - sub.Length;

                if (m_subs.Count == 1)
                {
                    Debug.Assert(m_lst);
                    return true;
                }
            }

            return MatchParts(str, lc, hc, li, hi);
        }

        static char ToLower(char ch)
        {
            if (ch >= 'A' && ch <= 'Z')
                return (char)(ch + ('a' - 'A'));
            else
                return ch;
        }

        static bool MatchChar(char c, char t)
        {
            c = ToLower(c);
            return t == c || t == '?';
        }

        bool MatchZero(string str)
        {
            Debug.Assert(m_lst == m_rst);
            if (m_lst)
                return true;
            else
                return str.Length == 0;
        }

        bool MatchExact(string str, int pos, string sub)
        {
            Debug.Assert(sub.Length > 0);
            if (str.Length < pos + sub.Length)
                return false;
            for (int i = 0; i < sub.Length; ++i)
            {
                if (!MatchChar(str[pos + i], sub[i]))
                    return false;
            }
            return true;
        }

        bool MatchParts(string str, int lc, int hc, int li, int hi)
        {
            // Pattern "aaa*bbb"
            Debug.Assert(hi >= li);
            if (hi <= li)
                return hc >= lc;

            if (hc <= lc)
                return false;

            for (int mi = li; mi < hi; ++mi)
            {
                string sub = m_subs[mi];
                int mc = MatchPart(str, lc, hc, sub);
                if (mc < 0)
                    return false;
                lc = mc + sub.Length;
            }

            return true;
        }

        int MatchPart(string str, int lc, int hc, string sub)
        {
            if (hc - lc < sub.Length)
                return -1;

            for (int mc = lc; mc <= hc - sub.Length; ++mc)
            {
                Debug.Assert(hc - mc >= sub.Length);
                if (MatchExact(str, mc, sub))
                    return mc;
            }
            return -1;
        }
    }
}
