using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Nano.Xapi.Netdisk
{
    public class Range
    {
        public long From;
        public long To;

        public override string ToString()
        {
            return string.Format("{0},{1}", From, To);
        }

        public Range(long from, long to)
        {
            From = from;
            To = to;
        }

        public Range()
        {
            
        }

        public static Range FromString(string text)
        {
            if (text == null)
                return null;
            text = text.Trim();
            if (text.Length == 0)
                return null;
            var tokens = text.Split(',');
            if (tokens.Length != 2)
                return null;
            
            var from = long.Parse(tokens[0]);
            var to = long.Parse(tokens[1]);
            return new Range() { From = from, To = to };
        }

        public static void Add(List<Range> ranges, long from, long to)
        {
            Debug.Assert(from < to && from >= 0 && to > 0);

            var i = 0;
            Range range = null;
            while (i < ranges.Count)
            {
                var r = ranges[i];

                //已经找到重合range的情况下，将to包含的range删除掉，找到to的结束点
                if (range != null)
                {
                    //下一个range不重合，结束查找
                    if (r.From > range.To)
                    {
                        break;
                    }

                    //下一个range的结束点在本次的结束点之后，则合并range。
                    if (r.To >= range.To)
                    {
                        range.To = r.To;
                        ranges.RemoveAt(i);
                        break;
                    }

                    ranges.RemoveAt(i);
                    continue;
                }

                //range排在最前面了，插入在最前面
                if (r.From > to)
                {
                    range = new Range() { From = from, To = to };
                    ranges.Insert(i, range);
                    break;
                }

                //重合range的开始
                if (r.To >= from)
                {
                    range = r;
                    if (r.From > from)
                    {
                        range.From = from;
                    }

                    if (range.To >= to)
                    {
                        break;
                    }
                    range.To = to;
                }
                i++;
            }

            //没有找到合适的range，则在尾部添加
            if (range == null)
            {
                ranges.Add(new Range() { From = from, To = to });
            }
        }

        public static List<Range> Copy(List<Range> ranges)
        {
            var ret = new List<Range>();
            for (var i = 0; i < ranges.Count; i++)
            {
                var r = ranges[i];
                ret.Add(new Range() { From = r.From, To = r.To });
            }
            return ret;
        }

        public static void Remove(List<Range> ranges, long from, long to)
        {
            Debug.Assert(from < to && from >= 0 && to > 0);

            var i = 0;
            while (i < ranges.Count)
            {
                var r = ranges[i];

                //不再范围内
                if (r.From >= to)
                {
                    break;
                }

                //To在r范围内，则切断，保留后面的部分
                if (r.To >= to)
                {
                    if (r.From >= from)
                    {
                        r.From = to;
                        if (r.From == r.To)
                        {
                            ranges.RemoveAt(i);
                        }
                        break;
                    }

                    var rto = r.To;
                    r.To = from;
                    if (rto != to)
                    {
                        ranges.Insert(i+1, new Range() { From = to, To = rto });
                    }
                    break;
                }

                //to在r范围之后，包含了r
                //1. 把整个r包含了
                if (r.From >= from)
                {
                    ranges.RemoveAt(i);
                }
                else if (r.To >= from)
                {
                    r.To = from;
                    i++;
                }
                else
                {
                    i++;
                }
            }
        }

        public static List<Range> Merge(List<Range> ranges, long from, long to)
        {
            Debug.Assert(from < to && from >= 0 && to > 0);

            var i = 0;
            Range range = null;
            List<Range> ret = new List<Range>();
            bool over = false;
            while (i < ranges.Count)
            {
                var r = ranges[i];


                //(from,to)在r前，直接认为需要下载
                if (r.From >= to)
                {
                    range = new Range() { From = from, To = to };
                    ret.Add(range);
                    over = true;
                    break;
                }

                //有重叠的地方
                if (r.To >= to)
                {
                    //from在r左，截取from，r.From
                    if (r.From > from)
                    {
                        range = new Range() { From = from, To = r.From };
                        ret.Add(range);
                        over = true;
                        break;
                    }

                    range = r;
                    over = true;
                    break;
                }
                else
                {
                    if (r.From > from)
                    {
                        ret.Add(new Range() { From = from, To = r.From });
                        from = r.To;
                    }
                    else if (r.To > from)
                    {
                        from = r.To;
                    }
                    i++;
                }
            }

            if (!over)
            {
                ret.Add(new Range() { From = from, To = to });
            }

            return ret;
        }

        

        public static bool Contains(List<Range> ranges, long from, long to)
        {
            //TODO 
            Debug.Assert(from < to && from >= 0 && to > 0);

            var i = 0;
            List<Range> ret = new List<Range>();
            while (i < ranges.Count)
            {
                var r = ranges[i];
                if (r.From >= to)
                {
                    return false;
                }

                if (r.To > from)
                {
                    return r.To >= to;
                }

                i++;
            }
            return false;
        }

        public static long Length(Range r)
        {
            return r.To - r.From;
        }

        public static long Total(List<Range> Ranges)
        {
            long total = 0;
            foreach (var r in Ranges)
            {
                total += (r.To - r.From);
            }
            return total;
        }

        public static void FromString(List<Range> ranges, string text)
        {
            if (text == null)
                return;

            text = text.Trim();
            if (text.Length == 0)
                return;

            var tokens = text.Split('|');
            if (tokens.Length == 0)
                return;

            for (var i = 0; i < tokens.Length; i++)
            {
                var r = Range.FromString(tokens[i]);
                if (null != r)
                    ranges.Add(r);
            }
        }

        public static string ToString(List<Range> ranges)
        {
            string s = "";
            foreach (var r in ranges)
            {
                s += r.ToString() + "|";
            }
            return s.TrimEnd('|');
        }
    }
}
