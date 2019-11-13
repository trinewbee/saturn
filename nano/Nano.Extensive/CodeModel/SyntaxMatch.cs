using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Lexical;

namespace Nano.Ext.CodeModel
{
	public interface SNPTrait
	{
		LexToken FetchMeanToken(ref int pos, int end);
		LexToken PeekMeanToken(ref int pos, int end);
	}

	public abstract class SNPNode
	{
		public string Key = null;

		public abstract SNPMatch Match(SNPTrait trait, int pos, int end);

		protected SNPMatch MatchSingleToken(SNPTrait trait, int pos, int end, Predicate<LexToken> pred)
		{
			var token = trait.PeekMeanToken(ref pos, end);
			if (token != null && pred(token))
				return new SNPMatch(this, pos, pos + 1);
			else
				return null;
		}

		protected SNPMatch MakeNest(int pos, SNPMatch imatch)
		{
			var match = new SNPMatch(this, pos, imatch.End);
			match.ChildNodes.Add(imatch);
			return match;
		}
	}

	public class SNPMatch
	{
		public SNPNode Cr;
		public int Start, End;
		public List<SNPMatch> ChildNodes;

		public SNPMatch(SNPNode cr, int start, int end)
		{
			Cr = cr;
			Start = start;
			End = end;
			ChildNodes = new List<SNPMatch>();
		}

		public string Key => Cr.Key;

		public int Count => ChildNodes.Count;

		public SNPMatch this[int index] => ChildNodes[index];

		public List<SNPMatch> Select(Predicate<SNPMatch> pred)
		{
			var r = new List<SNPMatch>();
			var queue = new Queue<SNPMatch>();
			queue.Enqueue(this);

			while (queue.Count != 0)
			{
				var m = queue.Dequeue();
				if (pred(m))
					r.Add(m);

				foreach (var mi in m.ChildNodes)
					queue.Enqueue(mi);
			}
			return r;
		}

		public SNPMatch SelectSingle(Predicate<SNPMatch> pred)
		{
			var r = Select(pred);
			if (r.Count > 1)
				throw new Exception("More than one result");
			else if (r.Count == 1)
				return r[0];
			else
				return null;
		}

		public List<SNPMatch> Select(string key) => Select(x => x.Key == key);

		public SNPMatch SelectSingle(string key) => SelectSingle(x => x.Key == key);

		public List<SNPMatch> SelectAny(params string[] keys) => Select(x => x.Key != null && Array.IndexOf(keys, x.Key) >= 0);

		public SNPMatch SelectSingleAny(params string[] keys) => SelectSingle(x => x.Key != null && Array.IndexOf(keys, x.Key) >= 0);
	}

	// 匹配给定的 symbol
	public class SNPSymbol : SNPNode
	{
		public int Symbol;

		public override SNPMatch Match(SNPTrait trait, int pos, int end) => MatchSingleToken(trait, pos, end, Pred);

		bool Pred(LexToken token) => token.Type == LexTokenType.Symbol && token.VI == Symbol;
	}

	// 匹配给定的 keyword
	public class SNPKeyword : SNPNode
	{
		public int Keyword;

		public override SNPMatch Match(SNPTrait trait, int pos, int end) => MatchSingleToken(trait, pos, end, Pred);

		bool Pred(LexToken token) => token.Type == LexTokenType.Keyword && token.VI == Keyword;
	}

	// 匹配 ident token
	public class SNPIdent : SNPNode
	{
		public override SNPMatch Match(SNPTrait trait, int pos, int end) => MatchSingleToken(trait, pos, end, Pred);

		bool Pred(LexToken token) => token.Type == LexTokenType.Ident;
	}

	// 匹配数值和字符串常量 token
	public class SNPLiteral : SNPNode
	{
		public override SNPMatch Match(SNPTrait trait, int pos, int end) => MatchSingleToken(trait, pos, end, Pred);

		bool Pred(LexToken token) => token.Type == LexTokenType.Int || token.Type == LexTokenType.Long || token.Type == LexTokenType.Double || token.Type == LexTokenType.String;
	}

	// 匹配任一个子节点，如果有多个匹配则抛出异常
	public class SNPAny : SNPNode, IEnumerable<SNPNode>
	{
		public List<SNPNode> Nodes = new List<SNPNode>();

		public void Add(SNPNode node) => Nodes.Add(node);

		public override SNPMatch Match(SNPTrait trait, int pos, int end)
		{
			SNPMatch match = null;
			foreach (var node in Nodes)
			{
				var imatch = node.Match(trait, pos, end);
				if (imatch != null)
				{
					if (match == null)
						match = MakeNest(pos, imatch);
					else
						throw new Exception("More than one matches found");
				}
			}
			return match;
		}

		public IEnumerator<SNPNode> GetEnumerator() => Nodes.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => Nodes.GetEnumerator();
	}

	// 匹配重复节点
	public class SNPRepeat : SNPNode
	{
		public SNPNode Node;
		public bool AllowZero;	// 允许匹配 0 个实例

		public override SNPMatch Match(SNPTrait trait, int pos, int end)
		{
			var match = new SNPMatch(this, pos, pos);
			while (pos < end)
			{
				var imatch = Node.Match(trait, pos, end);
				if (imatch != null)
				{
					match.ChildNodes.Add(imatch);
					pos = match.End = imatch.End;
				}
				else
					break;
			}
			if (match.ChildNodes.Count != 0 || AllowZero)
				return match;
			else
				throw new Exception("No match found");
		}
	}

	// 匹配可缺省节点
	public class SNPOmit : SNPNode
	{
		public SNPNode Node;

		public override SNPMatch Match(SNPTrait trait, int pos, int end)
		{
			var imatch = Node.Match(trait, pos, end);
			if (imatch != null)
				return MakeNest(pos, imatch);
			else
				return new SNPMatch(this, pos, pos); // empty match
		}
	}

	// 匹配末尾可缺省节点
	public class SNPEndOmit : SNPNode
	{
		public SNPNode Node;

		public override SNPMatch Match(SNPTrait trait, int pos, int end)
		{
			trait.PeekMeanToken(ref pos, end);
			if (pos >= end)
				return new SNPMatch(this, pos, pos); // empty match

			var imatch = Node.Match(trait, pos, end);
			if (imatch != null)
				return MakeNest(pos, imatch);
			else
				return null;
		}
	}

	// 匹配序列
	public class SNPSeq : SNPNode, IEnumerable<SNPNode>
	{
		public List<SNPNode> Nodes = new List<SNPNode>();

		public void Add(SNPNode node) => Nodes.Add(node);

		public override SNPMatch Match(SNPTrait trait, int pos, int end)
		{
			var match = new SNPMatch(this, pos, pos);
			foreach (var node in Nodes)
			{
				var imatch = node.Match(trait, pos, end);
				if (imatch == null)
					return null;

				pos = match.End = imatch.End;
				match.ChildNodes.Add(imatch);
			}
			return match;
		}

		public IEnumerator<SNPNode> GetEnumerator() => Nodes.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => Nodes.GetEnumerator();
	}

	// 匹配成对的括号
	public class SNPBrackets : SNPNode
	{
		public int HeadSymbol;
		public int[] OpenSymbols, CloseSymbols;

		public override SNPMatch Match(SNPTrait trait, int pos, int end)
		{
			var bpos = pos;
			var token = trait.PeekMeanToken(ref pos, end);
			if (token.Type != LexTokenType.Symbol || token.VI != HeadSymbol)
				return null;

			var stack = new Stack<int>();
			for (; pos < end; )
			{
				token = trait.FetchMeanToken(ref pos, end);
				if (token.Type != LexTokenType.Symbol)
					continue;

				var symbol = token.VI;
				var index = Array.IndexOf(OpenSymbols, symbol);
				if (index >= 0)
					stack.Push(CloseSymbols[index]);
				else if (symbol == stack.Peek())
				{
					stack.Pop();
					if (stack.Count == 0)
						break;
				}
			}

			return new SNPMatch(this, bpos, pos);
		}
	}

	// 要求匹配的子项目一直到给定的结束位置
	public class SNPFill : SNPNode
	{
		public SNPNode Node;

		public override SNPMatch Match(SNPTrait trait, int pos, int end)
		{
			var imatch = Node.Match(trait, pos, end);
			if (imatch == null)
				return null;

			int epos = imatch.End;
			trait.PeekMeanToken(ref epos, end);
			if (epos < end)
				return null;

			var match = new SNPMatch(this, pos, end);
			match.ChildNodes.Add(imatch);
			return match;
		}
	}
}
