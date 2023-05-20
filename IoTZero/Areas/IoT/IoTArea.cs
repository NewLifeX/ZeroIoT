using System.ComponentModel;
using NewLife;
using NewLife.Cube;

namespace IoTEdge.Areas.IoT;

[DisplayName("网关配置")]
public class IoTArea : AreaBase
{
    public IoTArea() : base(nameof(IoTArea).TrimEnd("Area")) { }

    static IoTArea() => RegisterArea<IoTArea>();
}