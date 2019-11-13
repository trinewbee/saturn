using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Common
{
	public interface ITreeVisitor<T>
	{
		IEnumerable<T> GetChildren(T item);
		string GetKey(T item);
	}

	public interface ITreeWalkNotify<T>
	{
		void EnterNode(T item);
		void LeaveNode(T item);
	}

	public interface ITreeCompareNotify<T>
	{
		void EnterNode(T item1, T item2);
		void LeaveNode(T item1, T item2);
		void LeftOnly(T parent1, T item1, T parent2);
		void RightOnly(T parent1, T parent2, T item2);
	}

	public interface ITreeCompareNotify2<T, U>
	{
		void EnterNode(T item1, U item2);
		void LeaveNode(T item1, U item2);
		void LeftOnly(T parent1, T item1, U parent2);
		void RightOnly(T parent1, U parent2, U item2);
	}

	public static class TreeWalker
	{
		public static void Walk<T>(T node, ITreeVisitor<T> visitor, ITreeWalkNotify<T> notify)
		{
			notify.EnterNode(node);
			foreach (T nodeSub in visitor.GetChildren(node))
				Walk<T>(nodeSub, visitor, notify);
			notify.LeaveNode(node);
		}

		public static void Compare<T>(T node1, ITreeVisitor<T> v1, T node2, ITreeVisitor<T> v2, ITreeCompareNotify<T> notify)
		{
			notify.EnterNode(node1, node2);

			Dictionary<string, T> map = new Dictionary<string, T>();
			foreach (T sub1 in v1.GetChildren(node1))
				map.Add(v1.GetKey(sub1), sub1);

			// Avoid collection changed when enumerating
			List<T> list = new List<T>(v2.GetChildren(node2));
			foreach (T sub2 in list)
			{
				string key = v2.GetKey(sub2);
				T sub1;
				if (map.TryGetValue(key, out sub1))
				{
					Compare<T>(sub1, v1, sub2, v2, notify);
					map.Remove(key);
				}
				else
					notify.RightOnly(node1, node2, sub2);
			}

			foreach (T sub1 in map.Values)
				notify.LeftOnly(node1, sub1, node2);

			notify.LeaveNode(node1, node2);
			map.Clear();
			map = null;
		}

		public static void Compare2<T, U>(T node1, ITreeVisitor<T> v1, U node2, ITreeVisitor<U> v2, ITreeCompareNotify2<T, U> notify)
		{
			notify.EnterNode(node1, node2);

			Dictionary<string, T> map = new Dictionary<string, T>();
			foreach (T sub1 in v1.GetChildren(node1))
				map.Add(v1.GetKey(sub1), sub1);

			// Avoid collection changed when enumerating
			List<U> list = new List<U>(v2.GetChildren(node2));
			foreach (U sub2 in list)
			{
				string key = v2.GetKey(sub2);
				T sub1;
				if (map.TryGetValue(key, out sub1))
				{
					Compare2<T, U>(sub1, v1, sub2, v2, notify);
					map.Remove(key);
				}
				else
					notify.RightOnly(node1, node2, sub2);
			}

			foreach (T sub1 in map.Values)
				notify.LeftOnly(node1, sub1, node2);

			notify.LeaveNode(node1, node2);
			map.Clear();
			map = null;
		}
	}
}
