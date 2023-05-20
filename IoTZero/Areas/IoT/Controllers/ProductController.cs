using System.ComponentModel;
using IoT.Data;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using NewLife.Web;

namespace IoTZero.Areas.IoT.Controllers;

[IoTArea]
[DisplayName("产品定义")]
[Menu(90, true, Icon = "fa-product-hunt")]
public class ProductController : EntityController<Product>
{
    static ProductController()
    {
        LogOnChange = true;

        ListFields.RemoveField("Secret", "DataFormat", "DynamicRegister", "FixedDeviceCode", "AuthType", "WhiteIP", "Remark");
        ListFields.RemoveCreateField();

        {
            var df = ListFields.GetField("DeviceCount") as ListField;
            df.DisplayName = "{DeviceCount}";
            df.Url = "/IoT/Device?productId={Id}";
            //df.DataVisible = (e, f) => (e as Product).DeviceCount > 0;
        }

        {
            var df = ListFields.AddListField("function", "UpdateUser");
            df.DisplayName = "功能定义";
            df.Url = "/IoT/ProductFunction?productId={Id}";
            df.Title = "产品的物模型属性";
        }

        {
            var df = ListFields.AddListField("tsl", "UPdateUser");
            df.DisplayName = "功能定义TSL";
            df.Url = "/IoT/TSL/Edit?productId={Id}";
            df.Title = "TSL模型";
        }

        {
            var df = ListFields.AddListField("publish", "UPdateUser");
            df.DisplayName = "功能发布";
            df.Url = "/IoT/ProductFunction/PublishBatch?productId={Id}";
            df.Title = "批量发布产品功能定义";
            //df.DataVisible = e => (e as Product).DeviceCount > 0;
        }

        {
            var df = ListFields.AddListField("rule", "UpdateUser");
            df.DisplayName = "规则策略";
            df.Url = "/IoT/RulePolicy?productId={Id}";
        }

        {
            var df = ListFields.AddListField("Log");
            df.DisplayName = "日志";
            df.Url = "/Admin/Log?category=产品&linkId={Id}";
        }
    }

    protected override IEnumerable<Product> Search(Pager p)
    {
        var id = p["Id"].ToInt(-1);
        if (id > 0)
        {
            var entity = Product.FindById(id);
            if (entity != null) return new[] { entity };
        }

        var code = p["code"];

        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        return Product.Search(code, start, end, p["Q"], p);
    }
}