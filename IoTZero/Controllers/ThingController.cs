using IoTZero.Common;
using IoTZero.Services;
using Microsoft.AspNetCore.Mvc;
using NewLife.IoT.Models;
using NewLife.IoT.ThingModels;
using NewLife.IoT.ThingSpecification;

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
    /// <param name="setting"></param>
    public ThingController(QueueService queue, MyDeviceService deviceService, ThingService thingService, IoTSetting setting) : base(deviceService, setting)
    {
        _queue = queue;
        _thingService = thingService;
    }

    #region 设备属性
    /// <summary>上报设备属性</summary>
    /// <param name="model">属性集合</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(PostProperty))]
    public Int32 PostProperty(PropertyModels model) => throw new NotImplementedException();

    /// <summary>批量上报设备属性，融合多个子设备数据批量上传</summary>
    /// <param name="models">属性集合</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(PostProperties))]
    public Int32 PostProperties(PropertyModels[] models) => throw new NotImplementedException();

    /// <summary>获取设备属性</summary>
    /// <param name="deviceCode">设备编码</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpGet(nameof(GetProperty))]
    public PropertyModel[] GetProperty(String deviceCode) => throw new NotImplementedException();

    /// <summary>设备数据上报</summary>
    /// <param name="model">模型</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(PostData))]
    public Int32 PostData(DataModels model)
    {
        var device = GetDevice(model.DeviceCode);

        return _thingService.PostData(device, model, "PostData", UserHost);
    }

    /// <summary>批量设备数据上报，融合多个子设备数据批量上传</summary>
    /// <param name="models">模型</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(PostDatas))]
    public Int32 PostDatas(DataModels[] models) => throw new NotImplementedException();
    #endregion

    #region 设备事件
    /// <summary>设备事件上报</summary>
    /// <param name="model">模型</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(PostEvent))]
    public Int32 PostEvent(EventModels model) => throw new NotImplementedException();

    /// <summary>批量设备事件上报，融合多个子设备数据批量上传</summary>
    /// <param name="models">模型</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(PostEvents))]
    public Int32 PostEvents(EventModels[] models) => throw new NotImplementedException();
    #endregion

    #region 设备服务
    /// <summary>设备端响应服务调用</summary>
    /// <param name="model">服务</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(ServiceReply))]
    public Int32 ServiceReply(ServiceReplyModel model) => throw new NotImplementedException();
    #endregion

    #region 物模型
    /// <summary>获取设备所属产品的物模型</summary>
    /// <param name="deviceCode">设备编码</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpGet(nameof(GetSpecification))]
    public ThingSpec GetSpecification(String deviceCode) => throw new NotImplementedException();

    /// <summary>上报物模型</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(PostSpecification))]
    public IPoint[] PostSpecification(ThingSpecModel model) => throw new NotImplementedException();
    #endregion

    #region 设备影子
    /// <summary>上报设备影子</summary>
    /// <remarks>
    /// 设备影子是一个JSON文档，用于存储设备上报状态、应用程序期望状态信息。
    /// 每个设备有且只有一个设备影子，设备可以通过MQTT获取和设置设备影子来同步状态，该同步可以是影子同步给设备，也可以是设备同步给影子。
    /// 使用设备影子机制，设备状态变更，只需同步状态给设备影子一次，应用程序请求获取设备状态，不论应用程序请求数量，和设备是否联网在线，都可从设备影子中获取设备当前状态，实现应用程序与设备解耦。
    /// </remarks>
    /// <param name="model">数据</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpPost(nameof(PostShadow))]
    public Int32 PostShadow(ShadowModel model) => throw new NotImplementedException();

    /// <summary>获取设备影子</summary>
    /// <param name="deviceCode">设备编码</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpGet(nameof(GetShadow))]
    public String GetShadow(String deviceCode) => throw new NotImplementedException();
    #endregion

    #region 配置
    /// <summary>设备端查询配置信息</summary>
    /// <param name="deviceCode">设备编码</param>
    /// <returns></returns>
    [ApiFilter]
    [HttpGet(nameof(GetConfig))]
    public IDictionary<String, Object> GetConfig(String deviceCode) => throw new NotImplementedException();
    #endregion
}