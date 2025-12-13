using Microsoft.AspNetCore.Mvc;
using Puff.NetCore;

namespace TestAspNetCore
{
    /// <summary>
    /// 中间件功能演示控制器
    /// </summary>
    [Route("[controller]")]
    public class MiddlewareExampleController : JmController
    {
        
        /// <summary>
        /// 公开API - 无需任何中间件
        /// </summary>
        [IceApi()]
        object GetPublicData()
        {
            return new
            {
                message = "This is public data",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }

        /// <summary>
        /// 需要认证的API - 使用TokenValidationMiddleware
        /// </summary>
        [IceApi()]
        [RequireAuth]
        object GetUserData()
        {
            // 从中间件上下文获取用户信息（如果有的话）
            return new
            {
                message = "This is protected user data",
                userId = "123", // 在实际应用中应该从中间件上下文获取
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }

        /// <summary>
        /// 需要限流的API - 使用RateLimitMiddleware
        /// </summary>
        [IceApi()]
        [RateLimit(5)] // 每分钟最多5次请求
        object GetLimitedData()
        {
            return new
            {
                message = "This API has rate limiting",
                limit = "5 requests per minute",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }

        /// <summary>
        /// 需要审计的API - 使用AuditMiddleware
        /// </summary>
        [IceApi()]
        [Audit(Action = "GetSensitiveData")]
        object GetSensitiveData()
        {
            return new
            {
                message = "This is sensitive data that will be audited",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }

        /// <summary>
        /// 需要认证和审计的API - 使用多个中间件
        /// </summary>
        [IceApi()]
        [RequireAuth]
        [Audit(Action = "GetSecureUserData")]
        object GetSecureUserData()
        {
            return new
            {
                message = "This is secure user data (auth + audit)",
                userId = "123",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }
    }

    /// <summary>
    /// 文件相关控制器 - 演示按控制器配置中间件
    /// </summary>
    /// 
    [Route("[controller]")]
    public class FileExampleController : JmController
    {
        /// <summary>
        /// 文件上传API - 使用限流中间件
        /// </summary>
        [IceApi()]
        [RateLimit(2)] // 每分钟最多2次上传（严格限流）
        object UploadFile()
        {
            return new
            {
                message = "File uploaded successfully",
                fileId = "file_123",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }

        /// <summary>
        /// 文件下载API - 使用限流中间件
        /// </summary>
        [IceApi()]
        [RateLimit(20)] // 每分钟最多20次下载
        object DownloadFile()
        {
            return new
            {
                message = "File download initiated",
                fileUrl = "/files/download/123",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }

        /// <summary>
        /// 大文件上传API - 限流 + 特殊处理
        /// </summary>
        [IceApi()]
        [RateLimit(2)] // 更严格的限流：每分钟最多2次
        object UploadLargeFile()
        {
            return new
            {
                message = "Large file upload initiated",
                maxSize = "100MB",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }
    }

    /// <summary>
    /// 管理员控制器 - 演示多重安全中间件
    /// </summary>
    /// 
    [Route("[controller]")]
    public class AdminExampleController : JmController
    {
       
        /// <summary>
        /// 管理员功能 - 需要认证和审计
        /// </summary>
        [IceApi()]
        [RequireAuth]
        [Audit(Action = "AdminOperation")]
        object AdminOperation()
        {
            return new
            {
                message = "Admin operation completed",
                operation = "user_management",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }

        /// <summary>
        /// 系统配置 - 需要认证、审计和限流
        /// </summary>
        [IceApi()]
        [RequireAuth]
        [Audit(Action = "SystemConfig")]
        [RateLimit(3)] // 每分钟最多3次配置操作
        object UpdateSystemConfig()
        {
            return new
            {
                message = "System configuration updated",
                configType = "security_settings",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }
    }

    /// <summary>
    /// 演示类级别中间件的控制器 - 整个控制器需要认证和审计
    /// </summary>
    [Route("[controller]")]
    [RequireAuth]  //  类级别：整个控制器都需要认证
    [Audit(Action = "SecureControllerAccess")]  // 类级别：整个控制器的操作都会被审计
    public class SecureAreaController : JmController
    {
        /// <summary>
        /// 继承类级别中间件：RequireAuth + Audit
        /// </summary>
        [IceApi()]
        object GetSecureInfo()
        {
            return new
            {
                message = "This method inherits class-level auth + audit",
                area = "secure",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }

        /// <summary>
        /// 继承类级别中间件 + 方法级别限流
        /// </summary>
        [IceApi()]
        [RateLimit(3)] // ⚡ 方法级别：加上限流
        object GetCriticalData()
        {
            return new
            {
                message = "Class auth+audit + method rate limit",
                area = "critical",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }

        /// <summary>
        /// 继承类级别中间件 + 方法级别审计（覆盖类级别审计）
        /// </summary>
        [IceApi()]
        [Audit(Action = "SpecificOperation")] // 📋 方法级别：覆盖类级别的审计配置
        object PerformSpecificOperation()
        {
            return new
            {
                message = "Class auth + method-specific audit",
                operation = "specific",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                status = "success"
            };
        }
    }
}
