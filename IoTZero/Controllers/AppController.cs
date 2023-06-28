using IoT.Data;
using IoTZero.Common;
using IoTZero.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife;
using NewLife.Cube;
using NewLife.IoT.Models;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Serialization;
using NewLife.Web;
using IActionFilter = Microsoft.AspNetCore.Mvc.Filters.IActionFilter;

namespace IoTZero.Controllers;

/// <summary>物模型Api控制器。用于应用系统调用</summary>
[ApiController]
[Route("[controller]")]
public class AppController : ControllerBase, IActionFilter
{
    /// <summary>用户主机</summary>
    public String UserHost => HttpContext.GetUserHost();

    /// <summary>令牌</summary>
    public String Token { get; set; }

    private readonly MyDeviceService _deviceService;
    private readonly ThingService _thingService;
    private readonly ITracer _tracer;
    private IDictionary<String, Object> _args;

    #region 构造
    /// <summary>
    /// 实例化应用管理服务
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="deviceService"></param>
    /// <param name="thingService"></param>
    /// <param name="tracer"></param>
    public AppController(MyDeviceService deviceService, ThingService thingService, ITracer tracer)
    {
        _deviceService = deviceService;
        _thingService = thingService;
        _tracer = tracer;
    }

    void IActionFilter.OnActionExecuting(ActionExecutingContext context)
    {
        _args = context.ActionArguments;

        // 访问令牌
        var request = context.HttpContext.Request;
        var token = request.Query["Token"] + "";
        if (token.IsNullOrEmpty()) token = (request.Headers["Authorization"] + "").TrimStart("Bearer ");
        if (token.IsNullOrEmpty()) token = request.Headers["X-Token"] + "";
        if (token.IsNullOrEmpty()) token = request.Cookies["Token"] + "";
        Token = token;

        try
        {
            if (!token.IsNullOrEmpty())
            {
                //todo 验证令牌有效性
            }

            //if (context.ActionDescriptor is ControllerActionDescriptor act && !act.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute)))
            //    throw new ApiException(403, "设备认证失败");
        }
        catch (Exception ex)
        {
            var traceId = DefaultSpan.Current?.TraceId;
            context.Result = ex is ApiException aex
                ? new JsonResult(new { code = aex.Code, data = aex.Message, traceId })
                : new JsonResult(new { code = 500, data = ex.Message, traceId });

            WriteError(ex, context);
        }
    }

    /// <summary>请求处理后</summary>
    /// <param name="context"></param>
    void IActionFilter.OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception != null) WriteError(context.Exception, context);

        var traceId = DefaultSpan.Current?.TraceId;

        if (context.Result != null)
            if (context.Result is ObjectResult obj)
            {
                //context.Result = new JsonResult(new { code = obj.StatusCode ?? 0, data = obj.Value });
                var rs = new { code = obj.StatusCode ?? 0, data = obj.Value, traceId };
                context.Result = new ContentResult
                {
                    Content = rs.ToJson(false, true, true),
                    ContentType = "application/json",
                    StatusCode = 200
                };
            }
            else if (context.Result is EmptyResult)
                context.Result = new JsonResult(new { code = 0, data = new { }, traceId });
            else if (context.Exception != null && !context.ExceptionHandled)
            {
                var ex = context.Exception.GetTrue();
                if (ex is ApiException aex)
                    context.Result = new JsonResult(new { code = aex.Code, data = aex.Message, traceId });
                else
                    context.Result = new JsonResult(new { code = 500, data = ex.Message, traceId });

                context.ExceptionHandled = true;

                // 输出异常日志
                if (XTrace.Debug) XTrace.WriteException(ex);
            }
    }

    private void WriteError(Exception ex, ActionContext context)
    {
        // 拦截全局异常，写日志
        var action = context.HttpContext.Request.Path + "";
        if (context.ActionDescriptor is ControllerActionDescriptor act) action = $"{act.ControllerName}/{act.ActionName}";

        XTrace.WriteLine("{0} {1}", action, ex.Message);
    }
    #endregion

    #region 物模型
    /// <summary>获取设备属性</summary>
    /// <param name="deviceId">设备编号</param>
    /// <param name="deviceCode">设备编码</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpGet(nameof(GetProperty))]
    public PropertyModel[] GetProperty(Int32 deviceId, String deviceCode)
    {
        var dv = Device.FindById(deviceId) ?? Device.FindByCode(deviceCode);
        if (dv == null) return null;

        return _thingService.QueryProperty(dv, null);
    }

    /// <summary>设置设备属性</summary>
    /// <param name="model">数据</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(SetProperty))]
    public async Task<ServiceReplyModel> SetProperty(DevicePropertyModel model)
    {
        var dv = Device.FindByCode(model.DeviceCode);
        if (dv == null) return null;

        throw new NotImplementedException();
    }

    /// <summary>调用设备服务</summary>
    /// <param name="service">服务</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(InvokeService))]
    public async Task<ServiceReplyModel> InvokeService(ServiceRequest service)
    {
        Device dv = null;
        if (service.DeviceId > 0) dv = Device.FindById(service.DeviceId);
        if (dv == null)
        {
            if (!service.DeviceCode.IsNullOrWhiteSpace())
                dv = Device.FindByCode(service.DeviceCode);
            else
                throw new ArgumentNullException(nameof(service.DeviceCode));
        }

        if (dv == null) throw new ArgumentException($"找不到该设备：DeviceId={service.DeviceId}，DeviceCode={service.DeviceCode}");

        return await _thingService.InvokeServiceAsync(dv, service.ServiceName, service.InputData, service.Expire, service.Timeout);
    }
    #endregion
}