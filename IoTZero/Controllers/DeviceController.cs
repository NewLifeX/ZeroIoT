using System.Net.WebSockets;
using IoT.Data;
using IoTZero.Common;
using IoTZero.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.IoT.Models;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Serialization;

namespace IoTZero.Controllers;

/// <summary>设备控制器</summary>
[ApiController]
[Route("[controller]")]
public class DeviceController : BaseController
{
    private readonly QueueService _queue;
    private readonly MyDeviceService _deviceService;
    private readonly ThingService _thingService;
    private readonly ITracer _tracer;

    /// <summary>实例化设备控制器</summary>
    /// <param name="queue"></param>
    /// <param name="deviceService"></param>
    /// <param name="thingService"></param>
    /// <param name="hookService"></param>
    /// <param name="setting"></param>
    /// <param name="tracer"></param>
    public DeviceController(QueueService queue, MyDeviceService deviceService, ThingService thingService, IoTSetting setting, ITracer tracer) : base(deviceService, setting)
    {
        _queue = queue;
        _deviceService = deviceService;
        _thingService = thingService;
        _tracer = tracer;
    }

    #region 登录
    /// <summary>设备登录</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [ApiFilter]
    [HttpPost(nameof(Login))]
    public LoginResponse Login(LoginInfo model)
    {
        var dv = Device ?? Device.FindByCode(model.Code);

        var rs = _deviceService.Login(model, "Http", UserHost);

        return rs;
    }

    /// <summary>设备注销</summary>
    /// <param name="reason">注销原因</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpGet(nameof(Logout))]
    public LogoutResponse Logout(String reason)
    {
        var device = Device;
        if (device != null) _deviceService.Logout(device, reason, "Http", UserHost);

        return new LogoutResponse
        {
            Name = device?.Name,
            Token = null,
        };
    }
    #endregion

    #region 心跳
    /// <summary>设备心跳</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(Ping))]
    public PingResponse Ping(PingInfo model)
    {
        var rs = new PingResponse
        {
            Time = model.Time,
            ServerTime = DateTime.UtcNow.ToLong(),
        };

        var device = Device;
        if (device != null)
        {
            rs.Period = device.Period;

            var olt = _deviceService.Ping(device, model, Token, UserHost);

            // 令牌有效期检查，10分钟内到期的令牌，颁发新令牌。
            // 这里将来由客户端提交刷新令牌，才能颁发新的访问令牌。
            var tm = _deviceService.ValidAndIssueToken(device.Code, Token);
            if (tm != null)
            {
                rs.Token = tm.AccessToken;

                _deviceService.WriteHistory(device, "刷新令牌", true, tm.ToJson(), UserHost);
            }
        }

        return rs;
    }
    #endregion

    #region 下行通知
    /// <summary>下行通知</summary>
    /// <returns></returns>
    [HttpGet("/Device/Notify")]
    public async Task Notify()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            await Handle(socket, Token);
        }
        else
            HttpContext.Response.StatusCode = 400;
    }

    private async Task Handle(WebSocket socket, String token)
    {
        var device = Device;
        if (device == null) throw new InvalidOperationException("未登录！");

        XTrace.WriteLine("WebSocket连接 {0}", device);
        _deviceService.WriteHistory(device, "WebSocket连接", true, socket.State + "", UserHost);

        var source = new CancellationTokenSource();
        _ = Task.Run(() => ConsumeMessage(socket, device, UserHost, source));
        try
        {
            var buf = new Byte[4 * 1024];
            while (socket.State == WebSocketState.Open)
            {
                var data = await socket.ReceiveAsync(new ArraySegment<Byte>(buf), default);
                if (data.MessageType == WebSocketMessageType.Close) break;
                if (data.MessageType == WebSocketMessageType.Text)
                {
                    var str = buf.ToStr(null, 0, data.Count);
                    XTrace.WriteLine("WebSocket接收 {0} {1}", device, str);
                    _deviceService.WriteHistory(device, "WebSocket接收", true, str, UserHost);
                }
            }

            source.Cancel();
            XTrace.WriteLine("WebSocket断开 {0}", device);
            _deviceService.WriteHistory(device, "WebSocket断开", true, socket.State + "", UserHost);

            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "finish", default);
        }
        catch (WebSocketException ex)
        {
            XTrace.WriteLine("WebSocket异常 {0}", device);
            XTrace.WriteLine(ex.Message);
        }
        finally
        {
            source.Cancel();
        }
    }

    private async Task ConsumeMessage(WebSocket socket, Device device, String ip, CancellationTokenSource source)
    {
        DefaultSpan.Current = null;
        var cancellationToken = source.Token;
        var queue = _queue.GetQueue(device.Code);
        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                ISpan span = null;
                var mqMsg = await queue.TakeOneAsync(30);
                if (mqMsg != null)
                {
                    // 埋点
                    span = _tracer?.NewSpan($"redismq:ServiceQueue", mqMsg);

                    // 解码
                    var dic = JsonParser.Decode(mqMsg);
                    span?.Detach(dic);
                    var msg = JsonHelper.Convert<ServiceModel>(dic);

                    if (msg == null || msg.Id == 0 || msg.Expire.Year > 2000 && msg.Expire < DateTime.Now)
                        _deviceService.WriteHistory(device, "WebSocket发送", false, "消息无效。" + mqMsg, ip);
                    else
                    {
                        _deviceService.WriteHistory(device, "WebSocket发送", true, mqMsg, ip);

                        // 向客户端传递埋点信息，构建完整调用链
                        msg.TraceId = span + "";

                        await socket.SendAsync(msg.ToJson().GetBytes(), WebSocketMessageType.Text, true, cancellationToken);
                    }
                }
                else
                    await Task.Delay(100, cancellationToken);
                span?.Dispose();
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }
        finally
        {
            source.Cancel();
        }
    }
    #endregion
}