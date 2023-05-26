using System.Reflection;
using IoT.Data;
using IoTZero.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife;
using NewLife.Cube;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Serialization;
using IActionFilter = Microsoft.AspNetCore.Mvc.Filters.IActionFilter;

namespace IoTZero.Controllers;

/// <summary>控制器基类</summary>
public abstract class BaseController : ControllerBase, IActionFilter
{
    /// <summary>用户主机</summary>
    public String UserHost => HttpContext.GetUserHost();

    /// <summary>令牌</summary>
    public String Token { get; set; }

    /// <summary>当前设备</summary>
    public Device Device { get; set; }

    private readonly MyDeviceService _deviceService;
    private readonly IoTSetting _setting;
    private IDictionary<String, Object> _args;

    /// <summary>实例化设备控制器</summary>
    /// <param name="deviceService"></param>
    /// <param name="setting"></param>
    public BaseController(MyDeviceService deviceService, IoTSetting setting)
    {
        _deviceService = deviceService;
        _setting = setting;
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
        //context.HttpContext.Items["Token"] = token;
        //if (!context.ActionArguments.ContainsKey("token")) context.ActionArguments.Add("token", token);
        Token = token;

        try
        {
            if (!token.IsNullOrEmpty())
            {
                var device = _deviceService.DecodeToken(token, _setting.TokenSecret);

                Device = device;
            }

            if (Device == null && context.ActionDescriptor is ControllerActionDescriptor act && !act.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute)))
                throw new ApiException(403, "设备认证失败");
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
    }

    private void WriteError(Exception ex, ActionContext context)
    {
        // 拦截全局异常，写日志
        var action = context.HttpContext.Request.Path + "";
        if (context.ActionDescriptor is ControllerActionDescriptor act) action = $"{act.ControllerName}/{act.ActionName}";

        _deviceService.WriteHistory(null, action, false, ex?.GetTrue() + Environment.NewLine + _args?.ToJson(), UserHost);
    }

    /// <summary>
    /// 查找子设备
    /// </summary>
    /// <param name="deviceCode"></param>
    /// <returns></returns>
    protected Device GetDevice(String deviceCode)
    {
        var dv = Device;
        if (dv == null) return null;

        if (deviceCode.IsNullOrEmpty() || dv.Code == deviceCode) return dv;

        var child = Device.FindByCode(deviceCode);

        //dv = dv.Childs.FirstOrDefault(e => e.Code == deviceCode);
        if (child == null || child.Id != dv.Id) throw new Exception($"非法设备编码，[{deviceCode}]并非当前登录设备[{Device}]的子设备");

        return child;
    }
}