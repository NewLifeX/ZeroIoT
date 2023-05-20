using IoTZero.Common;
using IoTZero.Services;
using Microsoft.AspNetCore.Mvc;
using NewLife.IoT.ThingModels;

namespace IoTZero.Controllers;

/// <summary>物模型控制器</summary>
[ApiController]
[Route("[controller]")]
public class ThingController : BaseController
{
    private readonly QueueService _queue;
    private readonly ThingService _thingService;

    /// <summary>实例化物模型控制器</summary>
    /// <param name="queue"></param>
    /// <param name="deviceService"></param>
    /// <param name="thingService"></param>
    /// <param name="hookService"></param>
    /// <param name="setting"></param>
    public ThingController(QueueService queue, MyDeviceService deviceService, ThingService thingService, IoTSetting setting) : base(deviceService, setting)
    {
        _queue = queue;
        _thingService = thingService;
    }

    #region 物模型
    /// <summary>设备数据上报</summary>
    /// <param name="model">模型</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(PostData))]
    public Int32 PostData(DataModels model)
    {
        var device = GetDevice(model.DeviceCode);

        var rs = _thingService.PostData(device, model, "PostData", UserHost);

        return rs;
    }

    /// <summary>批量设备数据上报，融合多个子设备数据批量上传</summary>
    /// <param name="models">模型</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(PostDatas))]
    public Int32 PostDatas(DataModels[] models)
    {
        var rs = 0;
        foreach (var model in models)
        {
            var device = GetDevice(model.DeviceCode);

            rs += _thingService.PostData(device, model, "PostData", UserHost);
        }

        return rs;
    }

    /// <summary>设备端响应服务调用</summary>
    /// <param name="model">服务</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(ServiceReply))]
    public Int32 ServiceReply(ServiceReplyModel model)
    {
        var rs = _thingService.ServiceReply(Device, model);

        return rs;
    }
    #endregion
}