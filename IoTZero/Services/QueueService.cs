using IoT.Data;
using NewLife.Caching;
using NewLife.Caching.Queues;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Serialization;
using Stardust.Services;

namespace IoTZero.Services;

/// <summary>队列服务</summary>
public class QueueService
{
    #region 属性
    ///// <summary>队列主机</summary>
    //public ICache Host { get; set; }

    private readonly CacheService _cacheService;
    private readonly ITracer _tracer;
    #endregion

    #region 构造
    /// <summary>
    /// 实例化队列服务
    /// </summary>
    public QueueService(/*IEnumerable<IConfigProvider> configs,*/ CacheService cacheService, ITracer tracer)
    {
        //// 优先使用本地配置的redis连接字符串，配置中心的配置值作为兜底
        ////var redisConnection = configs.FirstOrDefault(e => !e["redisQueue"].IsNullOrEmpty())?["redisQueue"];
        //String redisConnection = null;
        //foreach (var cfg in configs)
        //{
        //    // 过滤异常配置数据
        //    if (cfg == null) continue;

        //    XTrace.WriteLine(cfg.GetType().FullName);

        //    redisConnection = cfg["RedisQueue"];
        //    if (!redisConnection.IsNullOrEmpty()) break;
        //}
        //if (!redisConnection.IsNullOrEmpty())
        //{
        //    var rds = new FullRedis { Name = "Queue", Tracer = tracer };
        //    rds.Init(redisConnection);

        //    XTrace.WriteLine("启用Redis队列：{0}", rds.Server);

        //    _redis = rds;
        //    Host = rds;
        //}

        _cacheService = cacheService;
        //Host = cacheService.Cache;
        _tracer = tracer;
    }
    #endregion

    #region 命令队列
    ///// <summary>获取消息Topic</summary>
    ///// <param name="deviceCode"></param>
    ///// <returns></returns>
    //public String GetTopic(String deviceCode) => $"cmd:{deviceCode}";

    /// <summary>
    /// 获取指定设备的命令队列
    /// </summary>
    /// <param name="deviceCode"></param>
    /// <returns></returns>
    public IProducerConsumer<String> GetQueue(String deviceCode)
    {
        var q = _cacheService.GetQueue<String>($"cmd:{deviceCode}");
        if (q is QueueBase qb) qb.TraceName = "ServiceQueue";

        return q;
    }

    /// <summary>
    /// 向指定设备发送命令
    /// </summary>
    /// <param name="deviceCode"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    public Int32 Publish(String deviceCode, ServiceModel model)
    {
        using var span = _tracer?.NewSpan(nameof(Publish), $"{deviceCode} {model.ToJson()}");

        var q = GetQueue(deviceCode);
        return q.Add(model.ToJson());
    }

    /// <summary>
    /// 获取指定设备的服务响应队列
    /// </summary>
    /// <param name="serviceLogId"></param>
    /// <returns></returns>
    public IProducerConsumer<String> GetReplyQueue(Int64 serviceLogId)
    {
        var q = _cacheService.GetQueue<String>($"service:{serviceLogId}");
        if (q is QueueBase qb) qb.TraceName = "ServiceLog";

        return q;
    }

    /// <summary>
    /// 发送消息到服务响应队列
    /// </summary>
    /// <param name="model"></param>
    public void Publish(ServiceReplyModel model)
    {
        var topic = $"service:{model.Id}";
        var q = _cacheService.GetQueue<String>(topic);
        if (q is QueueBase qb) qb.TraceName = "ServiceLog";

        // 发送消息，并设置过期时间
        q.Add(model.ToJson());
        _cacheService.Cache.SetExpire(topic, TimeSpan.FromMinutes(10));
    }
    #endregion

    #region 方法
    private IProducerConsumer<Object> _dataQueue;
    /// <summary>获取数据队列</summary>
    /// <returns></returns>
    public IProducerConsumer<Object> GetDataQueue() => _dataQueue ??= _cacheService.GetQueue<Object>("DeviceData", "Group");

    /// <summary>添加到数据队列</summary>
    /// <param name="data"></param>
    public void AddData(DeviceData data)
    {
        var queue = GetDataQueue();
        queue?.Add(data);
    }
    #endregion
}