using NewLife;
using NewLife.Log;
using NewLife.Model;

namespace IoTEdge;

internal class EdgeService : IHostedService
{
    private readonly ClientSetting _clientSetting;
    private readonly ITracer _tracer;
    private HttpDevice _device;

    public EdgeService(ClientSetting clientSetting, ITracer tracer)
    {
        _clientSetting = clientSetting;
        _tracer = tracer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 产品编码、产品密钥从IoT管理平台获取，设备编码支持自动注册
        var device = new HttpDevice(_clientSetting)
        {
            Tracer = _tracer,
            Log = XTrace.Log,
        };

        await device.Login();

        _device = device;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_device != null) await _device.Logout(nameof(StopAsync));

        _device.TryDispose();
    }
}
