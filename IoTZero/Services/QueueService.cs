using NewLife;
using NewLife.Caching;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Serialization;

namespace IoTEdge.Services;

/// <summary>队列服务</summary>
public class QueueService
{
    #region 属性
    /// <summary>
    /// 队列主机
    /// </summary>
    public ICache Host { get; set; }

    private readonly ITracer _tracer;
    #endregion

    #region 构造
    /// <summary>
    /// 实例化队列服务
    /// </summary>
    public QueueService(ICache cache, ITracer tracer)
    {
        Host = cache;
        _tracer = tracer;
    }
    #endregion

    /// <summary>
    /// 获取指定设备的命令队列
    /// </summary>
    /// <returns></returns>
    public IProducerConsumer<String> GetQueue() => Host.GetQueue<String>("deviceService");

    /// <summary>
    /// 向指定设备发送命令
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public Int32 Publish(ServiceModel model)
    {
        using var span = _tracer?.NewSpan(nameof(Publish), model);

        var q = GetQueue();
        return q.Add(model.ToJson());
    }

    IProducerConsumer<String> _consumer;
    /// <summary>
    /// 消费服务队列
    /// </summary>
    /// <returns></returns>
    public async Task<ServiceModel> ConsumeAsync()
    {
        _consumer ??= GetQueue();

        var msg = await _consumer.TakeOneAsync(15);
        if (msg.IsNullOrEmpty()) return null;

        return msg.ToJsonEntity<ServiceModel>();
    }

    /// <summary>
    /// 获取指定设备的服务响应队列
    /// </summary>
    /// <param name="serviceLogId"></param>
    /// <returns></returns>
    public IProducerConsumer<String> GetReplyQueue(Int64 serviceLogId) => Host.GetQueue<String>($"service:{serviceLogId}");

    /// <summary>
    /// 发送消息到服务响应队列
    /// </summary>
    /// <param name="model"></param>
    public void Publish(ServiceReplyModel model)
    {
        var topic = $"service:{model.Id}";
        var queue = Host.GetQueue<String>(topic);

        // 发送消息，并设置过期时间
        queue.Add(model.ToJson());

        Host.SetExpire(topic, TimeSpan.FromMinutes(10));
    }

    /// <summary>
    /// 消费响应消息
    /// </summary>
    /// <param name="serviceLogId"></param>
    /// <returns></returns>
    public async Task<ServiceReplyModel> ConsumeOneAsync(Int64 serviceLogId)
    {
        var consumer = GetReplyQueue(serviceLogId);

        var msg = await consumer.TakeOneAsync(15);
        if (msg.IsNullOrEmpty()) return null;

        return msg.ToJsonEntity<ServiceReplyModel>();
    }
}