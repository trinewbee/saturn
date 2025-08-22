using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Nano.Logs;

namespace Puff.NetCore
{
    #region 中间件特性基类
    
    /// <summary>
    /// 中间件特性基类 - 所有中间件特性都应继承此类
    /// </summary>
    public abstract class BaseMiddlewareAttribute : Attribute
    {
        /// <summary>
        /// 创建对应的中间件实例
        /// </summary>
        public abstract IPuffMiddleware CreateMiddleware();
        
        /// <summary>
        /// 中间件执行优先级（数值越小优先级越高）
        /// </summary>
        public virtual int Priority => 100;
    }

    #endregion
    
    #region 中间件核心接口

    /// <summary>
    /// 方法信息接口 - 只暴露中间件需要的安全信息
    /// </summary>
    public interface IMethodInfo
    {
        /// <summary>
        /// 方法名称
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// API标志
        /// </summary>
        IceApiFlag ApiFlags { get; }
        
        /// <summary>
        /// 检查方法是否包含指定特性
        /// </summary>
        bool HasAttribute<T>() where T : Attribute;
        
        /// <summary>
        /// 获取指定特性的实例（如果存在）
        /// </summary>
        T GetAttribute<T>() where T : Attribute;
        
        /// <summary>
        /// 获取指定类型的所有特性实例
        /// </summary>
        T[] GetAttributes<T>() where T : Attribute;
        
        /// <summary>
        /// 获取所有继承自指定基类的特性
        /// </summary>
        T[] GetCustomAttributes<T>() where T : Attribute;
    }

    /// <summary>
    /// 方法信息的安全实现 - 封装 JmMethod 但只暴露安全信息
    /// </summary>
    internal class JmMethodInfo : IMethodInfo
    {
        private readonly JmMethod _jmMethod;
        
        public JmMethodInfo(JmMethod jmMethod)
        {
            _jmMethod = jmMethod ?? throw new ArgumentNullException(nameof(jmMethod));
        }
        
        public string Name => _jmMethod.Name;
        
        public IceApiFlag ApiFlags => _jmMethod.Attr?.Flags ?? IceApiFlag.Json;
        
        public bool HasAttribute<T>() where T : Attribute
        {
            return _jmMethod.MI?.GetCustomAttribute<T>() != null;
        }
        
        public T GetAttribute<T>() where T : Attribute
        {
            return _jmMethod.MI?.GetCustomAttribute<T>();
        }
        
        public T[] GetAttributes<T>() where T : Attribute
        {
            return _jmMethod.MI?.GetCustomAttributes<T>()?.ToArray() ?? new T[0];
        }
        
        public T[] GetCustomAttributes<T>() where T : Attribute
        {
            var methodAttributes = _jmMethod.MI?.GetCustomAttributes<T>(inherit: true)?.ToArray() ?? new T[0];
            var classAttributes = _jmMethod.MI?.DeclaringType?.GetCustomAttributes<T>(inherit: true)?.ToArray() ?? new T[0];
            
            var allAttributes = new List<T>();
            allAttributes.AddRange(classAttributes);  // 先添加类级别特性
            allAttributes.AddRange(methodAttributes); // 再添加方法级别特性
            

            
            return allAttributes.ToArray();
        }
    }

    /// <summary>
    /// 同步中间件执行上下文
    /// </summary>
    public interface IPuffContext
    {
        /// <summary>
        /// 当前请求的唯一标识符
        /// </summary>
        string RequestId { get; }
        
        /// <summary>
        /// 控制器名称
        /// </summary>
        string ControllerName { get; }
        
        /// <summary>
        /// 动作方法名称
        /// </summary>
        string ActionName { get; }
        
        /// <summary>
        /// 原始HTTP请求对象
        /// </summary>
        IceApiRequest Request { get; }
        
        /// <summary>
        /// HTTP响应对象，中间件可以修改此对象
        /// </summary>
        IceApiResponse Response { get; set; }
        
        /// <summary>
        /// 用于在中间件之间传递数据的字典
        /// </summary>
        IDictionary<string, object> Items { get; }
        
        /// <summary>
        /// 标识请求是否被中止，不再继续执行后续中间件和业务逻辑
        /// </summary>
        bool IsAborted { get; set; }
        
        /// <summary>
        /// 当前执行的方法信息（安全封装）
        /// </summary>
        IMethodInfo MethodInfo { get; }
        
        /// <summary>
        /// 控制器实例
        /// </summary>
        object Controller { get; }
        
        /// <summary>
        /// 当前环境信息
        /// </summary>
        Env Environment { get; }
    }

    /// <summary>
    /// 同步中间件基础接口
    /// </summary>
    public interface IPuffMiddleware
    {
        /// <summary>
        /// 执行中间件逻辑（同步版本）
        /// </summary>
        /// <param name="context">执行上下文</param>
        /// <param name="next">下一个中间件的执行委托</param>
        /// <returns>是否继续执行后续中间件</returns>
        bool Invoke(IPuffContext context, Action next);
    }

    /// <summary>
    /// 同步中间件委托类型
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="next">下一个中间件的执行委托</param>
    /// <returns>是否继续执行后续中间件</returns>
    public delegate bool MiddlewareDelegate(IPuffContext context, Action next);

    #endregion

    #region 中间件管道接口

    /// <summary>
    /// 基于特性的同步中间件管道接口
    /// </summary>
    public interface IMiddlewarePipeline
    {
        /// <summary>
        /// 执行中间件管道（同步版本）
        /// </summary>
        /// <param name="context">执行上下文</param>
        /// <param name="businessLogic">业务逻辑委托</param>
        /// <returns>是否成功执行</returns>
        bool Invoke(IPuffContext context, Action businessLogic);
    }

    #endregion

    #region 中间件管道实现

    /// <summary>
    /// 基于特性的同步中间件管道实现
    /// </summary>
    public class AttributeBasedMiddlewarePipeline : IMiddlewarePipeline
    {
        public bool Invoke(IPuffContext context, Action businessLogic)
        {
            // 获取方法上的所有中间件特性
            var middlewareAttributes = context.MethodInfo.GetCustomAttributes<BaseMiddlewareAttribute>();
            
            if (middlewareAttributes == null || middlewareAttributes.Length == 0)
            {
                // 如果没有中间件特性，直接执行业务逻辑
                businessLogic();
                return true;
            }
            
            // 按优先级排序（数值越小优先级越高）
            var sortedAttributes = middlewareAttributes.OrderBy(attr => attr.Priority).ToArray();
            

            
            // 创建中间件实例列表
            var middlewares = new List<IPuffMiddleware>();
            foreach (var attr in sortedAttributes)
            {
                try
                {
                    var middleware = attr.CreateMiddleware();
                    if (middleware != null)
                    {
                        middlewares.Add(middleware);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Err($"创建中间件失败: {attr.GetType().Name}", ex.ToString());
                    // 中间件创建失败时，继续处理其他中间件
                }
            }
            
            // 构建执行链
            var index = 0;
            var executionSuccessful = true;
            
            void InvokeNext()
            {
                if (context.IsAborted) 
                {
                    executionSuccessful = false;
                    return;
                }
                
                if (index >= middlewares.Count)
                {
                    businessLogic();
                    executionSuccessful = !context.IsAborted;
                    return;
                }
                
                var middleware = middlewares[index++];
                
                // 执行中间件，并检查返回值
                var continueExecution = middleware.Invoke(context, () => {
                    InvokeNext(); // Action调用，不返回值
                });
                
                // 如果中间件返回false，停止执行
                if (!continueExecution)
                {
                    executionSuccessful = false;
                    return;
                }
                
                // 检查执行状态
                if (!executionSuccessful)
                {
                    return;
                }
            }
            
            InvokeNext();
            return executionSuccessful;
        }
        
       
    }



    #endregion

    #region 中间件上下文实现

    /// <summary>
    /// 同步中间件上下文的具体实现
    /// </summary>
    public class PuffContext : IPuffContext
    {
        public string RequestId => Environment.reqId;
        public string ControllerName => Controller.GetType().Name;
        public string ActionName => MethodInfo.Name;
        public IceApiRequest Request { get; private set; }
        public IceApiResponse Response { get; set; }
        public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
        public bool IsAborted { get; set; }
        public IMethodInfo MethodInfo { get; private set; }
        public object Controller { get; private set; }
        public Env Environment { get; private set; }
        
        /// <summary>
        /// 内部保留对原始 JmMethod 的引用，供框架内部使用
        /// </summary>
        internal JmMethod InternalMethod { get; private set; }

        internal PuffContext(Env env, JmMethod method, object controller, HttpRequest httpRequest)
        {
            Environment = env;
            InternalMethod = method;
            MethodInfo = new JmMethodInfo(method);
            Controller = controller;
            Request = new AspncHttpRequest(httpRequest);
        }
    }

    #endregion




}

