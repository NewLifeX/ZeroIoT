using IoT.Data;
using NewLife;
using NewLife.Caching;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;
using Stardust.Services;

namespace IoTZero.Services;

/// <summary>物模型服务</summary>
public class ThingService
{
    private readonly DataService _dataService;
    private readonly QueueService _queueService;
    private readonly MyDeviceService _deviceService;
    private readonly ITracer _tracer;
    private readonly ICache _cache;

    /// <summary>
    /// 实例化物模型服务
    /// </summary>
    /// <param name="dataService"></param>
    /// <param name="queueService"></param>
    /// <param name="deviceService"></param>
    /// <param name="cacheService"></param>
    /// <param name="tracer"></param>
    public ThingService(DataService dataService, QueueService queueService, MyDeviceService deviceService, CacheService cacheService, ITracer tracer)
    {
        _dataService = dataService;
        _queueService = queueService;
        _deviceService = deviceService;
        _cache = cacheService.InnerCache;
        _tracer = tracer;
    }

    #region 属性
    /// <summary>上报数据</summary>
    /// <param name="device"></param>
    /// <param name="model"></param>
    /// <param name="kind"></param>
    /// <param name="ip"></param>
    /// <returns></returns>
    public Int32 PostData(Device device, DataModels model, String kind, String ip)
    {
        var rs = 0;
        foreach (var item in model.Items)
        {
            var property = BuildDataPoint(device, item.Name, item.Value, item.Time, ip);
            if (property != null)
            {
                UpdateProperty(property);

                SaveHistory(device, property, item.Time, kind, ip);

                rs++;
            }
        }

        // 自动上线
        if (device != null) _deviceService.SetDeviceOnline(device, ip, kind);

        //todo 触发指定设备的联动策略

        return rs;
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
            var property = BuildDataPoint(device, item.Name, item.Value, 0, ip);
            if (property != null)
            {
                UpdateProperty(property);

                SaveHistory(device, property, 0, nameof(PostProperty), ip);

                rs++;
            }
        }

        // 自动上线
        if (device != null) _deviceService.SetDeviceOnline(device, ip, nameof(PostProperty));

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
    public DeviceProperty BuildDataPoint(Device device, String name, Object value, Int64 timestamp, String ip)
    {
        using var span = _tracer?.NewSpan(nameof(BuildDataPoint), $"{device.Id}-{name}-{value}");

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
        if (!entity.Enable)
        {
            _tracer?.NewError($"{nameof(BuildDataPoint)}-NotEnable", new { name, entity.Enable });
            return null;
        }

        //todo 检查数据是否越界

        //todo 修正数字精度，小数点位数

        entity.Name = name;
        entity.Value = value?.ToString();

        var now = DateTime.Now;
        entity.TraceId = DefaultSpan.Current?.TraceId;
        entity.UpdateTime = now;
        entity.UpdateIP = ip;

        return entity;
    }

    /// <summary>更新属性</summary>
    /// <param name="property"></param>
    /// <returns></returns>
    public Boolean UpdateProperty(DeviceProperty property)
    {
        if (property == null) return false;

        //todo 如果短时间内数据没有变化（无脏数据），则不需要保存属性
        //var hasDirty = (property as IEntity).Dirtys[nameof(property.Value)];

        // 新属性直接更新，其它异步更新
        if (property.Id == 0)
            property.Insert();
        else
            property.SaveAsync();

        return true;
    }

    /// <summary>保存历史数据，写入属性表、数据表、分段数据表</summary>
    /// <param name="device"></param>
    /// <param name="property"></param>
    /// <param name="timestamp"></param>
    /// <param name="kind"></param>
    /// <param name="ip"></param>
    public void SaveHistory(Device device, DeviceProperty property, Int64 timestamp, String kind, String ip)
    {
        using var span = _tracer?.NewSpan(nameof(SaveHistory), new { deviceName = device.Name, property.Name, property.Value, property.Type });
        try
        {
            // 记录数据流水，使用经过处理的属性数值字段
            var id = 0L;
            var data = _dataService.AddData(property.DeviceId, timestamp, property.Name, property.Value, kind, ip);
            if (data != null) id = data.Id;

            //todo 存储分段数据

            //todo 推送队列
        }
        catch (Exception ex)
        {
            span?.SetError(ex, property);

            throw;
        }
    }

    /// <summary>获取设备属性对象，长时间缓存，便于加速属性保存</summary>
    /// <param name="device"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private DeviceProperty GetProperty(Device device, String name)
    {
        var key = $"DeviceProperty:{device.Id}:{name}";
        if (_cache.TryGetValue<DeviceProperty>(key, out var property)) return property;

        using var span = _tracer?.NewSpan(nameof(GetProperty), $"{device.Id}-{name}");

        var entity = device.Properties.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
        if (entity != null)
            _cache.Set(key, entity, 3600);

        return entity;
    }

    /// <summary>查询设备属性。应用端调用</summary>
    /// <param name="device">设备编码</param>
    /// <param name="names">属性名集合</param>
    /// <returns></returns>
    public PropertyModel[] QueryProperty(Device device, String[] names)
    {
        var list = new List<PropertyModel>();
        foreach (var item in device.Properties)
        {
            // 如果未指定属性名，则返回全部
            if (item.Enable && (names == null || names.Length == 0 || item.Name.EqualIgnoreCase(names)))
                list.Add(new PropertyModel { Name = item.Name, Value = item.Value });
        }

        return list.ToArray();
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
        _queueService.Publish(model);

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
        var model = InvokeService(device, command, argument, expire);

        //var model = log.ToServiceModel();
        _queueService.Publish(device.Code, model);

        var rs = await _queueService.GetReplyQueue(model.Id).TakeOneAsync(5_000);
        return rs?.ToJsonEntity<ServiceReplyModel>();
    }
    #endregion
}