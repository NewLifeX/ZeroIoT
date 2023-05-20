using IoT.Data;
using IoT.Data.Models;
using NewLife;
using NewLife.Caching;
using NewLife.IoT;
using NewLife.IoT.ThingModels;
using NewLife.IoT.ThingSpecification;
using NewLife.Log;
using NewLife.Reflection;
using XCode;

namespace IoTEdge.Services;

/// <summary>物模型服务</summary>
public class ThingService
{
    private readonly DataService _dataService;
    private readonly QueueService _queue;
    private readonly RuleService _ruleService;
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
    public ThingService(DataService dataService, QueueService queue, RuleService ruleService, QueueService queueService, ITracer tracer)
    {
        _dataService = dataService;
        _queue = queue;
        _ruleService = ruleService;
        _queueService = queueService;
        _tracer = tracer;
    }

    #region 属性
    private void VerifyModel(Device device, String name, FunctionKinds kind)
    {
        // 强校验产品，需要判断该属性是否在功能定义里面
        var prd = device.Product;
        if (prd != null && prd.VerifyModel)
        {
            if (!prd.Functions.Any(e => e.Enable && e.Identifier == name && e.Kind == kind))
                throw new Exception($"设备[{device}]的物模型不支持{kind}[{name}]");
        }
    }

    /// <summary>上报数据，写入属性表、数据表、分段数据表</summary>
    /// <param name="device"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="timestamp"></param>
    /// <param name="kind"></param>
    /// <param name="ip"></param>
    /// <returns></returns>
    public IDeviceProperty PostData(Device device, String name, Object value, Int64 timestamp, String kind, String ip)
    {
        var dp = PostProperty(device, name, value, timestamp, ip);

        // 记录数据流水，使用经过处理的属性数值字段
        if (dp != null)
        {
            _dataService.AddData(dp.DeviceId, timestamp, dp.Name, dp.Value, kind, ip);
        }

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

        // 触发指定设备的联动策略
        if (rs > 0) _ruleService.Execute(device.Id);

        return rs;
    }

    /// <summary>设备属性上报</summary>
    /// <param name="device">设备</param>
    /// <param name="name">属性名</param>
    /// <param name="value">数值</param>
    /// <param name="timestamp">时间戳</param>
    /// <param name="ip">IP地址</param>
    /// <returns></returns>
    public IDeviceProperty PostProperty(Device device, String name, Object value, Int64 timestamp, String ip)
    {
        using var span = _tracer?.NewSpan("PostProperty", $"{device.Id}-{name}-{value}");

        var entity = GetProperty(device, name);
        if (entity == null)
        {
            // 产品开启强校验物模型属性，不会自动创建属性
            if (device.Product.VerifyModel) return null;

            VerifyModel(device, name, FunctionKinds.Property);

            var key = $"{device.Id}###{name}";
            entity = DeviceProperty.GetOrAdd(key,
                k => DeviceProperty.FindByNameAndDeviceId(name, device.Id),
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

        //if (timestamp == 0) timestamp = DateTime.UtcNow.ToLong();

        // 检查数据是否越界
        var function = ProductFunction.FindById(entity.FunctionId);
        if (function != null && !Valid(function, device.Id, entity, value)) return null;

        // 修正数字精度，小数点位数
        value = FixData(value, function);

        entity.Name = name;
        //entity.Enable = true;
        entity.SetValue(value);

        var hasDirty = (entity as IEntity).Dirtys[nameof(entity.Value)];

        var now = DateTime.Now;
        entity.LastPost = now;
        entity.Timestamp = timestamp;
        entity.TraceId = DefaultSpan.Current?.TraceId;
        entity.UpdateTime = now;
        entity.UpdateIP = ip;

        // 如果短时间内数据没有变化（无脏数据），则不需要保存属性
        var period = 60;
        if (hasDirty || period <= 0 || entity.LastPost.AddSeconds(period) < now)
        {
            // 属性上报直接更新，数据上报异步更新
            if (entity.Id == 0)
                entity.Save();
            else
                entity.SaveAsync();
        }

        return entity;
    }

    /// <summary>修正数据精度</summary>
    /// <param name="value"></param>
    /// <param name="function"></param>
    /// <returns></returns>
    public static Object FixData(Object value, ProductFunction function)
    {
        // 修正数字精度，小数点位数
        if (function == null || function.Step <= 0 || !function.DataType.EqualIgnoreCase("float", "single", "double"))
            return value;

        // 计算小数点后位数
        var step = function.Step.ToString();
        var p = step.IndexOf('.');
        if (p > 0)
        {
            // 0.001，p=1，len=3
            var len = step.Length - 1 - p;
            if (len > 0)
            {
                var d = value.ToDouble();
                d = Math.Round(d, p);
                value = d;
            }
        }

        return value;
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

    /// <summary>检查数据是否越界</summary>
    /// <param name="function"></param>
    /// <param name="deviceId"></param>
    /// <param name="dp"></param>
    /// <param name="val"></param>
    /// <returns></returns>
    private Boolean Valid(ProductFunction function, Int32 deviceId, DeviceProperty dp, Object val)
    {
        // 判断是否溢出
        if (function.Max - function.Min > 0)
        {
            var d = val.ToDouble();
            if (d < function.Min || d > function.Max) return false;
        }

        // 最大间隔。超过该值时抛弃
        if (function.MaxStep > 0)
        {
            var key = $"thing:maxstep:{deviceId}:{function.Identifier}";
            var has = _cache.TryGetValue<Double>(key, out var lastValue);
            if (!has)
            {
                if (dp != null)
                {
                    lastValue = dp.Value.ToDouble();
                    has = true;
                }
            }

            // 记录最后一次数据，即使没有采用。如果连续来了两个超限值，第二个将可能被采用
            _cache.Set(key, val.ToDouble(), 3600);

            if (has)
            {
                var d2 = val.ToDouble() - lastValue;
                if (Math.Abs(d2) > function.MaxStep) return false;
            }
        }

        return true;
    }
    #endregion
}