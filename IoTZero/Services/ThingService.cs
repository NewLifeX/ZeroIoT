using IoT.Data;
using NewLife;
using NewLife.Caching;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Security;
using XCode;

namespace IoTZero.Services;

/// <summary>物模型服务</summary>
public class ThingService
{
    private readonly DataService _dataService;
    private readonly QueueService _queue;
    private readonly QueueService _queueService;
    private readonly ITracer _tracer;
    private static readonly ICache _cache = new MemoryCache();

    /// <summary>
    /// 实例化物模型服务
    /// </summary>
    /// <param name="dataService"></param>
    /// <param name="queue"></param>
    /// <param name="ruleService"></param>
    /// <param name="segmentService"></param>
    /// <param name="tracer"></param>
    public ThingService(DataService dataService, QueueService queue, QueueService queueService, ITracer tracer)
    {
        _dataService = dataService;
        _queue = queue;
        _queueService = queueService;
        _tracer = tracer;
    }

    #region 属性
    /// <summary>上报数据，写入属性表、数据表、分段数据表</summary>
    /// <param name="device"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="timestamp"></param>
    /// <param name="kind"></param>
    /// <param name="ip"></param>
    /// <returns></returns>
    public DeviceProperty PostData(Device device, String name, Object value, Int64 timestamp, String kind, String ip)
    {
        var dp = PostProperty(device, name, value, timestamp, ip);

        // 记录数据流水，使用经过处理的属性数值字段
        if (dp != null)
            _dataService.AddData(dp.DeviceId, timestamp, dp.Name, dp.Value, kind, ip);

        return dp;
    }

    /// <summary>设备属性上报</summary>
    /// <param name="device">设备</param>
    /// <param name="items">名值对</param>
    /// <param name="ip">IP地址</param>
    /// <returns></returns>
    public Int32 PostProperty(Device device, PropertyModel[] items, String ip)
    {
        if (items == null) return -1;

        var rs = 0;
        foreach (var item in items)
        {
            PostData(device, item.Name, item.Value, 0, "PostProperty", ip);

            rs++;
        }

        //todo 触发指定设备的联动策略

        return rs;
    }

    /// <summary>设备属性上报</summary>
    /// <param name="device">设备</param>
    /// <param name="name">属性名</param>
    /// <param name="value">数值</param>
    /// <param name="timestamp">时间戳</param>
    /// <param name="ip">IP地址</param>
    /// <returns></returns>
    public DeviceProperty PostProperty(Device device, String name, Object value, Int64 timestamp, String ip)
    {
        using var span = _tracer?.NewSpan("PostProperty", $"{device.Id}-{name}-{value}");

        var entity = GetProperty(device, name);
        if (entity == null)
        {
            var key = $"{device.Id}###{name}";
            entity = DeviceProperty.GetOrAdd(key,
                k => DeviceProperty.FindByDeviceIdAndName(device.Id, name),
                k => new DeviceProperty
                {
                    DeviceId = device.Id,
                    Name = name,
                    NickName = name,
                    Enable = true,

                    CreateTime = DateTime.Now,
                    CreateIP = ip
                });
        }

        // 检查是否锁定
        if (!entity.Enable) return null;

        //todo 检查数据是否越界

        //todo 修正数字精度，小数点位数

        entity.Name = name;
        entity.Value = value?.ToString();

        var hasDirty = (entity as IEntity).Dirtys[nameof(entity.Value)];

        var now = DateTime.Now;
        //entity.TraceId = DefaultSpan.Current?.TraceId;
        entity.UpdateTime = now;
        entity.UpdateIP = ip;

        // 属性上报直接更新，数据上报异步更新
        if (entity.Id == 0)
            entity.Save();
        else
            entity.SaveAsync();

        return entity;
    }

    /// <summary>获取设备属性对象，长时间缓存，便于加速属性保存</summary>
    /// <param name="device"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private DeviceProperty GetProperty(Device device, String name)
    {
        var key = $"{device.Id}###{name}";
        if (_cache.TryGetValue<DeviceProperty>(key, out var property)) return property;

        using var span = _tracer?.NewSpan("GetProperty", $"{device.Id}-{name}");

        var entity = device.Properties.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
        if (entity != null)
            _cache.Set(key, entity, 3600);

        return entity;
    }
    #endregion

    #region 服务调用
    /// <summary>调用服务</summary>
    /// <param name="device"></param>
    /// <param name="command"></param>
    /// <param name="argument"></param>
    /// <param name="expire"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public ServiceModel InvokeService(Device device, String command, String argument, DateTime expire)
    {
        var traceId = DefaultSpan.Current?.TraceId;

        var log = new ServiceModel
        {
            Id = Rand.Next(),
            Name = command,
            InputData = argument,
            Expire = expire,
            TraceId = traceId,
        };

        return log;
    }

    /// <summary>服务响应</summary>
    /// <param name="device"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Int32 ServiceReply(Device device, ServiceReplyModel model)
    {
        //var log = DeviceServiceLog.FindById(model.Id);
        //if (log == null) return null;

        //// 防止越权
        //if (log.DeviceId != device.Id)
        //    throw new InvalidOperationException($"[{device}]越权访问[{log.DeviceName}]的服务");

        //log.Status = model.Status;
        //log.OutputData = model.Data;
        //log.Update();

        // 推入服务响应队列，让服务调用方得到响应
        _queue.Publish(model);

        return 1;
    }

    /// <summary>
    /// 异步调用服务，并返回结果
    /// </summary>
    /// <param name="device"></param>
    /// <param name="command"></param>
    /// <param name="argument"></param>
    /// <param name="expire"></param>
    /// <returns></returns>
    public async Task<ServiceReplyModel> InvokeAsync(Device device, String command, String argument, DateTime expire)
    {
        var log = InvokeService(device, command, argument, expire);

        //var model = log.ToServiceModel();
        _queueService.Publish(log);

        return await _queueService.ConsumeOneAsync(log.Id);
    }
    #endregion
}