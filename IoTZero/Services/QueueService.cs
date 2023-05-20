using IoT.Data;
using NewLife;
using NewLife.Caching;
using NewLife.Caching.Queues;
using NewLife.Configuration;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Model;
using NewLife.Serialization;

namespace IoTZero.Services;

/// <summary>队列服务</summary>
public class QueueService
{
    #region 属性
    /// <summary>队列主机</summary>
    public ICache Host { get; set; }

    private FullRedis _redis;
    private FullRedis _redisProperty;
    private readonly ITracer _tracer;
    #endregion

    #region 构造
    /// <summary>
    /// 实例化队列服务
    /// </summary>
    public QueueService(IEnumerable<IConfigProvider> configs, ICache cache, ITracer tracer)
    {
        // 优先使用本地配置的redis连接字符串，配置中心的配置值作为兜底
        //var redisConnection = configs.FirstOrDefault(e => !e["redisQueue"].IsNullOrEmpty())?["redisQueue"];
        String redisConnection = null;
        foreach (var cfg in configs)
        {
            // 过滤异常配置数据
            if (cfg == null) continue;

            XTrace.WriteLine(cfg.GetType().FullName);

            redisConnection = cfg["RedisQueue"];
            if (!redisConnection.IsNullOrEmpty()) break;
        }
        if (!redisConnection.IsNullOrEmpty())
        {
            var rds = new FullRedis { Name = "Queue", Tracer = tracer };
            rds.Init(redisConnection);

            XTrace.WriteLine("启用Redis队列：{0}", rds.Server);

            _redis = rds;
            Host = rds;
        }

        Host ??= new MemoryCache();
        _redisProperty = cache as FullRedis;
        _tracer = tracer;
    }
    #endregion

    #region 命令队列
    /// <summary>获取消息Topic</summary>
    /// <param name="deviceCode"></param>
    /// <returns></returns>
    public String GetTopic(String deviceCode) => $"cmd:{deviceCode}";

    /// <summary>
    /// 获取指定设备的命令队列
    /// </summary>
    /// <param name="deviceCode"></param>
    /// <returns></returns>
    public IProducerConsumer<String> GetQueue(String deviceCode)
    {
        var q = Host.GetQueue<String>(GetTopic(deviceCode));
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
        var q = Host.GetQueue<String>($"service:{serviceLogId}");
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
        var q = Host.GetQueue<String>(topic);
        if (q is QueueBase qb) qb.TraceName = "ServiceLog";

        // 发送消息，并设置过期时间
        q.Add(model.ToJson());
        Host.SetExpire(topic, TimeSpan.FromMinutes(10));
    }
    #endregion

    #region 方法
    /// <summary>
    /// 获取Redis对象
    /// </summary>
    /// <returns></returns>
    public Redis GetRedis() => _redis;

    private IProducerConsumer<Object> _dataQueue;
    /// <summary>获取数据队列</summary>
    /// <returns></returns>
    public IProducerConsumer<Object> GetDataQueue() => _dataQueue ??= _redis?.GetStream<Object>("DeviceData");

    MyDeferredQueue _defData;
    /// <summary>添加到数据队列</summary>
    /// <param name="data"></param>
    public void AddData(DeviceData data)
    {
        var queue = GetDataQueue();
        if (queue != null)
        {
            // 使用延迟队列，批量写Redis队列
            if (_defData == null)
            {
                lock (this)
                {
                    _defData ??= new MyDeferredQueue("Data", queue, _tracer) { Host = this };
                }
            }

            _defData.TryAdd(data.Id + "", data);
        }
    }

    void SavePropertyToRedis(IList<Object> list)
    {
        if (list == null || list.Count == 0) return;

        var rds = _redisProperty;
        if (rds == null) return;

        using var span = _tracer?.NewSpan(nameof(SavePropertyToRedis), list.Count);
        try
        {
            rds.StartPipeline();

            // 按照设备分组，整组处理。暂时不用管道
            var ds = list.Cast<DeviceData>().GroupBy(e => e.DeviceId);
            foreach (var d in ds)
            {
                //var dic = d.ToDictionary(e => e.Name, e => e.Value);
                var dic = new Dictionary<String, String>();
                foreach (var item in d.OrderBy(e => e.Timestamp))
                {
                    dic.TryAdd(item.Name, item.Value);
                }

                var key = $"iot:{d.Key}";
                var hash = rds.GetDictionary<String>(key) as RedisHash<String, String>;
                hash.HMSet(dic);
                rds.SetExpire(key, TimeSpan.FromDays(1));
            }

            rds.StopPipeline(true);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, list);

            //XTrace.WriteException(ex);
        }
    }
    #endregion

    #region 延迟队列
    class MyDeferredQueue : DeferredQueue
    {
        private readonly ITracer _tracer;

        public IProducerConsumer<Object> Queue { get; set; }

        public QueueService Host { get; set; }

        public MyDeferredQueue(String name, IProducerConsumer<Object> queue, ITracer tracer)
        {
            Name = name;
            Queue = queue;
            _tracer = tracer;

            Period = 100;
            BatchSize = 100;
            MaxEntity = 100_000_000;
        }

        protected override Int32 ProcessAll(ICollection<Object> list)
        {
            if (!list.Any()) return 0;

            using var span = _tracer?.NewSpan($"DeferredQueue-{Name}", list.Count);

            return base.ProcessAll(list);
        }

        public override Int32 Process(IList<Object> list)
        {
            if (list.Count <= 0) return 0;

            var rs = Queue.Add(list.ToArray());

            // 如果是Data，需要解析并保存属性
            if (Host != null && Name == "Data") Host.SavePropertyToRedis(list);

            return rs;
        }
    }
    #endregion
}