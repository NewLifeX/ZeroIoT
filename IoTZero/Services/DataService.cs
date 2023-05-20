using IoT.Data;
using NewLife;
using NewLife.Log;

namespace IoTEdge.Services;

/// <summary>数据服务</summary>
public class DataService
{
    private readonly ITracer _tracer;

    /// <summary>实例化数据服务</summary>
    /// <param name="tracer"></param>
    public DataService(ITracer tracer) => _tracer = tracer;

    #region 方法
    /// <summary>
    /// 插入设备原始数据，异步批量操作
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="time"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="kind"></param>
    /// <param name="ip"></param>
    /// <returns></returns>
    public Int32 AddData(Int32 deviceId, Int64 time, String name, String value, String kind, String ip)
    {
        if (value.IsNullOrEmpty()) return 0;

        // 原始数据不再落库，仅用于解析
        if (name.StartsWithIgnoreCase("raw-", "channel-")) return 0;

        using var span = _tracer?.NewSpan("AddData", $"{deviceId}-{name}-{value}");

        //// 客户端时间
        //if (time.Year < 2000) time = DateTime.Now;
        //// 在按天分表加上并行插入的模式下，有一定几率出现主键冲突，因此提前生成雪花Id
        //var snow = DeviceData.Meta.Factory.Snow;

        var traceId = DefaultSpan.Current?.TraceId;

        var entity = new DeviceData()
        {
            //Id = snow.NewId(time),
            DeviceId = deviceId,
            Name = name,
            Value = value,
            Kind = kind,

            Timestamp = time,
            TraceId = traceId,
            Creator = Environment.MachineName,
            CreateTime = DateTime.Now,
            CreateIP = ip,
        };

        var rs = entity.SaveAsync() ? 1 : 0;

        //todo 推送数据

        return rs;
    }
    #endregion
}