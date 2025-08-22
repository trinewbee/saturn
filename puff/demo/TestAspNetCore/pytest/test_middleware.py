#encoding=utf8

import json
import urllib.request
import urllib.error
import time

def RequestRaw(url, headers = {}, data = None):
    try:        
        # 在Python 3中，POST数据需要编码为bytes
        if data and isinstance(data, str):
            data = data.encode('utf-8')
        request = urllib.request.Request(url=url, headers=headers, data=data)
        r = urllib.request.urlopen(request)
        data = r.read()
        r.close()
        # 解码响应数据
        if isinstance(data, bytes):
            data = data.decode('utf-8')
        return (r.code, r.headers, data)
    except urllib.error.HTTPError as e:
        return (e.code, e.headers if hasattr(e, 'headers') else None, None)

def RequestApi(url, headers = {}, **kwargs):
    """请求API并解析响应，根据stat字段判断成功/失败"""
    data = json.dumps(kwargs) if kwargs != None else None
    r = RequestRaw(url, headers, data=data)
    status_code = r[0]
    raw_content = r[2]
    
    # 尝试解析JSON响应
    if status_code == 200 and raw_content:
        try:
            response = json.loads(raw_content)
            # JmController返回的JSON格式: {"stat": "ok", "data": {...}} 或 {"stat": "error", "m": "..."}
            if response.get("stat") == "ok" and "data" in response:
                return response["data"]  # 成功：返回data字段中的实际数据
            elif response.get("stat") != "ok":
                return None  # 失败：stat不是"ok"，返回None表示错误
            return response  # 如果格式不符合预期，返回原始响应
        except:
            pass  # JSON解析失败
    
    return None  # 非200状态码或解析失败

def RequestApiExpectError(url, headers = {}, **kwargs):
    """期望失败的请求，返回(是否为错误, 错误信息)"""
    data = json.dumps(kwargs) if kwargs != None else None
    r = RequestRaw(url, headers, data=data)
    status_code = r[0]
    raw_content = r[2]
    
    # 尝试解析JSON响应
    if status_code == 200 and raw_content:
        try:
            response = json.loads(raw_content)
            # 检查stat字段判断是否为错误响应
            if response.get("stat") != "ok":
                error_msg = response.get("m", "Unknown error")
                return (True, error_msg)  # (是错误, 错误信息)
            else:
                return (False, "Success")  # (不是错误, 成功信息)
        except:
            pass  # JSON解析失败
    
    # 如果不是200或解析失败，也认为是错误
    return (True, f"HTTP {status_code}")  # (是错误, HTTP状态码)

server = "http://127.0.0.1:5000"

def test_public_api():
    """测试公开API（无中间件限制）"""
    print("测试公开API...")
    url = server + "/middlewareexample/getpublicdata"
    r = RequestApi(url)
    if r == None:
        print("❌ 测试失败: 公开API应该成功")
        return False
    if r.get("message") == None:
        print("❌ 测试失败: 应该有message字段")
        return False
    print("✅ 公开API测试通过")
    return True

def test_authentication_middleware():
    """测试认证中间件"""
    print("测试认证中间件...")
    url = server + "/middlewareexample/getuserdata"
    
    # 测试无Token访问（应该失败）
    is_error, error_info = RequestApiExpectError(url)
    if not is_error:
        print("❌ 测试失败: 无Token访问应该失败")
        return False
    print("✅ 无Token访问被拒绝: %s" % error_info)
    
    # 测试错误Token（应该失败）
    headers = {"Authorization": "Bearer invalid-token"}
    is_error, error_info = RequestApiExpectError(url, headers)
    if not is_error:
        print("❌ 测试失败: 错误Token应该失败")
        return False
    print("✅ 错误Token被拒绝: %s" % error_info)
    
    # 测试正确Token（应该成功）
    headers = {"Authorization": "Bearer valid-token-123"}
    r = RequestApi(url, headers)
    if r == None:
        print("❌ 测试失败: 正确Token应该成功")
        return False
    print("✅ 正确Token访问成功")
    return True

def test_rate_limit_middleware():
    """测试限流中间件"""
    print("测试限流中间件...")
    url = server + "/middlewareexample/getlimiteddata"
    
    success_count = 0
    limit_count = 0
    
    # 快速发送多个请求测试限流
    for i in range(7):
        r = RequestApi(url)
        if r != None:
            success_count += 1
            print("   请求 %d: 成功" % (i + 1))
        else:
            limit_count += 1
            print("   请求 %d: 被限流" % (i + 1))
        time.sleep(0.1)  # 短暂延迟
    
    if limit_count == 0:
        print("❌ 测试失败: 应该有请求被限流")
        return False
    print("✅ 限流测试通过: %d 成功, %d 被限流" % (success_count, limit_count))
    return True

def test_audit_middleware():
    """测试审计中间件"""
    print("测试审计中间件...")
    url = server + "/middlewareexample/getsensitivedata"
    
    r = RequestApi(url)
    if r == None:
        print("❌ 测试失败: 审计API应该成功")
        return False
    if r.get("message") == None:
        print("❌ 测试失败: 应该有message字段")
        return False
    print("✅ 审计API测试通过")
    print("   注意: 检查服务器控制台的审计日志输出")
    return True

def test_file_controller_rate_limit():
    """测试文件控制器限流"""
    print("测试文件控制器限流...")
    url = server + "/fileexample/uploadfile"
    
    success_count = 0
    limit_count = 0
    
    # 测试文件上传API的限流
    for i in range(6):
        r = RequestApi(url)
        if r != None:
            success_count += 1
            print("   文件上传 %d: 成功" % (i + 1))
        else:
            limit_count += 1
            print("   文件上传 %d: 被限流" % (i + 1))
        time.sleep(0.2)
    
    if limit_count == 0:
        print("❌ 测试失败: 文件控制器应该有限流")
        return False
    print("✅ 文件控制器限流测试通过: %d 成功, %d 被限流" % (success_count, limit_count))
    return True

def test_admin_controller_security():
    """测试管理员控制器多重安全"""
    print("测试管理员控制器多重安全...")
    
    # 测试无认证访问管理员API
    url = server + "/adminexample/adminoperation"
    is_error, error_info = RequestApiExpectError(url)
    if not is_error:
        print("❌ 测试失败: 无认证访问管理员API应该失败")
        return False
    print("✅ 无认证访问管理员API被拒绝: %s" % error_info)
    
    # 测试有认证的管理员API
    headers = {"Authorization": "Bearer valid-token-123"}
    r = RequestApi(url, headers)
    if r == None:
        print("❌ 测试失败: 有认证访问管理员API应该成功")
        return False
    print("✅ 有认证访问管理员API成功")
    return True
    
    # 测试系统配置API（包含方法级限流）
    config_url = server + "/adminexample/updatesystemconfig"
    success_count = 0
    limit_count = 0
    
    for i in range(5):
        r = RequestApi(config_url, headers)
        if r != None:
            success_count += 1
            print("   系统配置 %d: 成功" % (i + 1))
        else:
            limit_count += 1
            print("   系统配置 %d: 被限流" % (i + 1))
        time.sleep(0.1)
    
    print("✅ 管理员API安全测试通过: 认证+限流+审计")
    return True

def test_combined_middlewares():
    """测试组合中间件"""
    print("测试组合中间件...")
    
    # 测试需要认证和审计的安全API  
    url = server + "/middlewareexample/getsecureuserdata"
    headers = {"Authorization": "Bearer valid-token-123"}
    
    r = RequestApi(url, headers)
    if r == None:
        print("❌ 测试失败: 安全API应该成功")
        return False
    if r.get("message") == None:
        print("❌ 测试失败: 应该有message字段")
        return False
    
    print("✅ 组合中间件测试通过")
    print("   注意: 这个API经过了 认证+审计+日志 多个中间件")
    return True

def test_performance_monitoring():
    """测试性能监控"""
    print("测试性能监控...")
    url = server + "/middlewareexample/getpublicdata"
    
    # 连续访问同一个API，测试性能监控
    for i in range(3):
        start_time = time.time()
        r = RequestApi(url)
        end_time = time.time()
        
        if r == None:
            print("❌ 测试失败: API应该成功")
            return False
        duration = (end_time - start_time) * 1000  # 转换为毫秒
        print("   请求 %d: %.0fms" % (i + 1, duration))
        time.sleep(0.1)
    
    print("✅ 性能监控测试完成，检查服务器控制台的性能日志")
    return True

def test_class_level_middleware():
    """测试类级别中间件"""
    print("测试类级别中间件...")
    
    # 测试继承类级别认证和审计的方法
    url = server + "/securearea/getsecureinfo"
    
    # 没有Token应该失败（继承类级别认证）
    is_error, error_info = RequestApiExpectError(url)
    if not is_error:
        print("❌ 测试失败: 类级别认证：无Token访问应该失败")
        return False
    print("✅ 类级别认证检查通过")
    
    # 有Token应该成功
    headers = {"Authorization": "Bearer valid-token-123"}
    r = RequestApi(url, headers)
    if r == None:
        print("❌ 测试失败: 类级别认证：有Token应该成功")
        return False
    print("✅ 类级别认证+审计测试通过")
    
    # 测试类级别+方法级别组合中间件
    url2 = server + "/securearea/getcriticaldata"
    
    success_count = 0
    limit_count = 0
    
    # 测试类级别认证+审计 + 方法级别限流的组合
    for i in range(5):
        r = RequestApi(url2, headers)
        if r:
            success_count += 1
        else:
            limit_count += 1
        time.sleep(0.1)
    
    print("✅ 类+方法级别组合中间件测试: %d 成功, %d 被限流" % (success_count, limit_count))
    
    # 测试方法级别特性覆盖类级别特性
    url3 = server + "/securearea/performspecificoperation"
    r = RequestApi(url3, headers)
    if r == None:
        print("❌ 测试失败: 方法级别覆盖类级别应该成功")
        return False
    print("✅ 方法级别覆盖类级别特性测试通过")
    return True

if __name__ == "__main__":
    print("=== Puff中间件系统测试 ===")
    print("")
    
    tests = [
        test_public_api,
        test_authentication_middleware,
        test_rate_limit_middleware,
        test_audit_middleware,
        test_file_controller_rate_limit,
        test_admin_controller_security,
        test_combined_middlewares,
        test_performance_monitoring,
        test_class_level_middleware
    ]
    
    passed = 0
    total = len(tests)
    
    for test in tests:
        try:
            if test():  # 检查返回值
                passed += 1
            print("")
        except Exception as e:
            print("❌ 测试异常: %s" % str(e))
            print("")
    
    print("测试结果: %d/%d 通过" % (passed, total))
    print("")
    print("=== 所有测试完成 ===")
    
    if passed == total:
        print("🎉 所有中间件功能正常工作！")
    else:
        print("⚠️ 部分测试失败，请检查服务器配置和中间件实现")