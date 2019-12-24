using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics;
using Nano.Collection;

namespace Nano.Ext.CodeModel
{
    /// <summary>Lambda 表达式构建辅助类</summary>
    public static class LEB
	{
		public static MethodInfo MI_ExprLambda;

		#region Static initializer

		static LEB()
		{
			MI_ExprLambda = SelectLambdaMethod();
		}

		static MethodInfo SelectLambdaMethod()
		{
			var msa = typeof(Expression).GetMethods();
			var msc = CollectionKit.Select(msa, delegate (MethodInfo m)
			{
				if (m.Name != "Lambda")
					return false;
				if (!m.IsGenericMethod)
					return false;
				var prms = m.GetParameters();
				if (prms.Length != 2 ||
					prms[0].ParameterType != typeof(Expression) ||
					prms[1].ParameterType != typeof(ParameterExpression[]))
					return false;
				return true;
			});
			Debug.Assert(msc.Count == 1);
			return msc[0];
		}

        #endregion

        // Making Expression instance
        // Expression<Func<int, int, int>> f = (x, y) => (x + y);

        #region 参数列表

        /// <summary>根据 ParameterInfo 构建 ParameterExpression 数组</summary>
        /// <param name="dprms">ParameterInfo 列表（包括数组）</param>
        /// <param name="offset">参数起始位置</param>
        /// <param name="count">参数数目</param>
        /// <returns>返回构建的 ParameterExpression 数组</returns>
        public static ParameterExpression[] Args(IList<ParameterInfo> dprms, int offset, int count)
		{
			ParameterExpression[] prms = new ParameterExpression[count];
			for (int i = 0; i < count; ++i)
			{
				var dprm = dprms[offset + i];
				prms[i] = Expression.Parameter(dprm.ParameterType, dprm.Name);

			}
			return prms;
		}

		/// <summary>根据给定的 MethodInfo 的输入参数信息构建 ParameterExpression 数组</summary>
		/// <param name="mi">MethodInfo</param>
		/// <returns>返回构建的 ParameterExpression 数组</returns>
		public static ParameterExpression[] Args(MethodInfo mi)
		{
			var dprms = mi.GetParameters();
			return Args(dprms, 0, dprms.Length);
		}

        public static ParameterExpression[] Args(Type vt)   // should be a delegate type
        {
            if (!vt.IsSubclassOf(typeof(MulticastDelegate)))
                throw new Exception("Not a delegate type");
            var mi = vt.GetMethod("Invoke");
            if (mi == null)
                throw new Exception("Method Invoke not found");
            return Args(mi);
        }

		#endregion

		#region 简单节点

		public static Expression Value(object o) => Expression.Constant(o);

		#endregion

		#region 类型转换

		/// <summary>如果给定的节点不是 object 类型，转换为 object 类型</summary>
		/// <param name="e">给定的节点</param>
		/// <returns>表达转换的节点</returns>
		public static Expression Object(Expression e) => e.Type != typeof(object) ? Expression.Convert(e, typeof(object)) : e;

		#endregion

		#region 数组

		/// <summary>将给定的节点列表构建一个 object 数组节点</summary>
		/// <param name="prms">节点列表</param>
		/// <returns>返回构建的 object 数组节点</returns>
		public static Expression ObjectArray(params Expression[] prms) => ObjectArray(prms, 0, prms.Length);

		/// <summary>将给定的节点列表构建一个 object 数组节点</summary>
		/// <param name="prms">节点列表</param>
		/// <param name="offset">起始位置</param>
		/// <param name="count">节点数目</param>
		/// <returns>返回构建的 object 数组节点</returns>
		public static Expression ObjectArray(IList<Expression> prms, int offset, int count)
		{
			Expression[] prmso = new Expression[count];
			for (int i = 0; i < count; ++i)
				prmso[i] = Object(prms[i + offset]);
			var prmsa = Expression.NewArrayInit(typeof(object), prmso);
			return prmsa;
		}

		#endregion

		#region 函数调用

		/// <summary>构造一个函数调用节点</summary>
		/// <param name="instance">作用对象（静态方法传入 null）</param>
		/// <param name="mi">目标方法</param>
		/// <param name="args">参数列表</param>
		/// <returns>返回构建的函数调用节点</returns>
		public static Expression Call(Expression instance, MethodInfo mi, params Expression[] args) => Expression.Call(instance, mi, args);

		/// <summary>构造一个函数调用节点</summary>
		/// <param name="instance">作用对象（静态方法传入 null）</param>
		/// <param name="mi">目标方法</param>
		/// <param name="args">参数列表</param>
		/// <returns>返回构建的函数调用节点</returns>
		public static Expression Call(object instance, MethodInfo mi, params Expression[] args)
		{
			var tag = Expression.Constant(instance);
			return Expression.Call(tag, mi, args);
		}

		#endregion

		#region 表达式编译

		/// <summary>编译一个动态给定目标类型的表达式</summary>
		/// <param name="vt">表达式的委托类型</param>
		/// <param name="body">表达式数根节点</param>
		/// <param name="prms">参数列表</param>
		/// <returns>返回编译完成的委托实现</returns>
		public static object Compile(Type vt, Expression body, params ParameterExpression[] prms)
		{
			// var expr = Expression.Lambda<My.SendNotify>(body, prms);			
			var mi = MI_ExprLambda.MakeGenericMethod(vt);
			object expr = mi.Invoke(null, new object[] { body, prms });

			// var m = expr.Compile();
			object m = ((LambdaExpression)expr).Compile();
			return m;
		}

		/// <summary>编译一个给定目标类型的表达式</summary>
		/// <typeparam name="T">表达式的委托类型</typeparam>
		/// <param name="body">表达式数根节点</param>
		/// <param name="prms">参数列表</param>
		/// <returns>返回编译完成的委托实现</returns>
		public static T Compile<T>(Expression body, params ParameterExpression[] prms)
		{
			var expr = Expression.Lambda<T>(body, prms);
			return expr.Compile();
		}

		#endregion
	}

    /// <summary>Lambda 表达式模型封装</summary>
    public class LEM
    {
        public class Argx
        {
            public ParameterExpression[] Items;

			public Argx(IList<ParameterExpression> _items) { Items = CollectionKit.ToArray(_items); }

            public Argx(ParameterExpression[] _items) { Items = _items; }

			public int Count
			{
				get { return Items.Length; }
			}

			public LEM this[int index]
			{
				get { return Items[index]; }
			}

            public LEM this[string key]
            {
                get
                {
                    var item = CollectionKit.Find(Items, x => x.Name == key);
                    if (item == null)
                        throw new KeyNotFoundException();
                    return new LEM(item);
                }
            }
        }

        Expression e;

        public LEM(Expression _e) { e = _e; }

        public static implicit operator LEM(Expression e) => new LEM(e);

        public static implicit operator Expression(LEM m) => m.e;

        public static LEM New(Expression e) => new LEM(e);

		public Type Type
		{
			get { return e.Type; }
		}

        #region Parameters

        public static Argx Args(IList<ParameterInfo> prms, int offset, int count) => new Argx(LEB.Args(prms, offset, count));

        public static Argx Args(MethodInfo mi) => new Argx(LEB.Args(mi));

        public static Argx Args(Type vt) => new Argx(LEB.Args(vt));

        public static Argx Args<T>() => new Argx(LEB.Args(typeof(T)));

        #endregion

        #region Values

        public static LEM Value(object o) => new LEM(Expression.Constant(o));

		#endregion

		#region Arrays

		public static LEM ObjectArray(IList<LEM> lems, int offset, int count)
		{
			var exprs = CollectionKit.Transform(lems, x => (Expression)x);
			return LEB.ObjectArray(exprs, offset, count);
		}

		public static LEM ObjectArray(IList<Expression> exprs, int offset, int count) => LEB.ObjectArray(exprs, offset, count);

		public static LEM ObjectArray(params LEM[] lems) => ObjectArray(lems, 0, lems.Length);

		public static LEM ObjectArray(params Expression[] exprs) => LEB.ObjectArray(exprs);

        #endregion

        #region Arithmetic operators

        public static LEM operator +(LEM a, LEM b) => Expression.Add(a, b);

        public static LEM operator -(LEM a, LEM b) => Expression.Subtract(a, b);

        public static LEM operator *(LEM a, LEM b) => Expression.Multiply(a, b);

        public static LEM operator /(LEM a, LEM b) => Expression.Divide(a, b);

		#endregion

		#region Comparison operators

		public static LEM operator >(LEM a, LEM b) => Expression.GreaterThan(a, b);

		public static LEM operator >=(LEM a, LEM b) => Expression.GreaterThanOrEqual(a, b);

		public static LEM operator <(LEM a, LEM b) => Expression.LessThan(a, b);

		public static LEM operator <=(LEM a, LEM b) => Expression.LessThanOrEqual(a, b);

		public static LEM operator ==(LEM a, LEM b) => Expression.Equal(a, b);

		public static LEM operator !=(LEM a, LEM b) => Expression.NotEqual(a, b);

		#endregion

		#region Logical operators

		public static LEM operator &(LEM a, LEM b) => Expression.And(a, b);

        public static LEM operator |(LEM a, LEM b) => Expression.Or(a, b);

        public static LEM operator ^(LEM a, LEM b) => Expression.ExclusiveOr(a, b);

        public static LEM And(LEM a, LEM b) => Expression.AndAlso(a, b);

        public static LEM Or(LEM a, LEM b) => Expression.OrElse(a, b);

		#endregion

		#region Function call

		/// <summary>创建函数调用</summary>
		/// <param name="o">对象实例（静态方法传入 null）</param>
		/// <param name="mi">目标方法</param>
		/// <param name="args">参数列表</param>
		/// <returns>包装函数调用的节点</returns>
		public static LEM Call(LEM o, MethodInfo mi, params LEM[] args)
		{
			LEM e = (object)o != null ? o.e : null;
			Expression[] prms = CollectionKit.Transform(args, x => x.e, args.Length);
			return Expression.Call(e, mi, prms);
		}

		public static LEM Call(LEM o, Delegate dl, params LEM[] args)
		{
			var mi = dl.Method;
			return Call(o, mi, args);
		}

		public LEM Call(MethodInfo mi, params LEM[] args) => Call(this, mi, args);

		public LEM Call(Delegate dl, params LEM[] args) => Call(this, dl, args);

		#endregion

		#region Field

		public static LEM Field(LEM o, FieldInfo field) => Expression.Field(o.e, field);

		public static LEM Field(LEM o, Type vt, string name)
		{
			var field = vt.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			return Field(o, field);
		}

		#endregion

		#region Compile

		public Expression<T> Lambda<T>(params ParameterExpression[] args) => Expression.Lambda<T>(e, args);

		public T Compile<T>(params ParameterExpression[] prms) => LEB.Compile<T>(e, prms);

        public T Compile<T>(Argx args) => LEB.Compile<T>(e, args.Items);

        public object Compile(Type vt, params ParameterExpression[] prms) => LEB.Compile(vt, e, prms);

        public object Compile(Type vt, Argx args) => LEB.Compile(vt, e, args.Items);

		#endregion

		#region Import expression

		static Expression ImportNode(Expression e, Dictionary<string, ParameterExpression> args)
		{
			switch (e.NodeType)
			{
				case ExpressionType.GreaterThan:
					return _Binary(Expression.GreaterThan, e, args);
				case ExpressionType.GreaterThanOrEqual:
					return _Binary(Expression.GreaterThanOrEqual, e, args);
				case ExpressionType.LessThan:
					return _Binary(Expression.LessThan, e, args);
				case ExpressionType.LessThanOrEqual:
					return _Binary(Expression.LessThanOrEqual, e, args);
				case ExpressionType.Equal:
					return _Binary(Expression.Equal, e, args);
				case ExpressionType.NotEqual:
					return _Binary(Expression.NotEqual, e, args);
				case ExpressionType.Parameter:
					return _Parameter(e, args);
				case ExpressionType.Constant:
					return e;
				default:
					throw new Exception("UnsupportedExpressionNode:" + e.NodeType);
			}
		}

		static Expression _Binary(Func<Expression, Expression, BinaryExpression> m, Expression e, Dictionary<string, ParameterExpression> args)
		{
			var _e = (BinaryExpression)e;
			return m(ImportNode(_e.Left, args), ImportNode(_e.Right, args));
		}

		static Expression _Parameter(Expression e, Dictionary<string, ParameterExpression> args)
		{
			var _e = (ParameterExpression)e;
			return args[_e.Name];
		}

		public static Dictionary<string, ParameterExpression> BuildArgMap<T>(Expression<T> e)
		{
			var map = new Dictionary<string, ParameterExpression>();
			foreach (var prm in e.Parameters)
				map.Add(prm.Name, prm);
			return map;
		}

		public static LEM Import<T>(Expression<T> e, Dictionary<string, ParameterExpression> args) => ImportNode(e.Body, args);

		public static LEM Import<T>(Expression<T> e)
		{
			var map = BuildArgMap(e);
			return Import<T>(e, map);
		}

		#endregion
	}
}
