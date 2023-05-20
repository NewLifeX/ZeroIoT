using System.ComponentModel;
using IoT.Data;
using IoTEdge.Models;
using IoTEdge.Services;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.IoT.Drivers;
using NewLife.Log;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace IoTEdge.Areas.IoT.Controllers;

[IoTArea]
[DisplayName("设备管理")]
[Menu(80, true, Icon = "fa-mobile")]
public class DeviceController : EntityController<Device>
{
    private readonly ITracer _tracer;
    private readonly PointImportService _pointService;

    static DeviceController()
    {
        LogOnChange = true;

        ListFields.RemoveField("Secret", "Uuid", "ProvinceId", "IP", "Period", "Address", "Location", "Logins", "LastLogin", "LastLoginIP", "OnlineTime", "RegisterTime", "Remark", "AreaName");
        ListFields.RemoveCreateField();
        ListFields.RemoveUpdateField();

        {
            var df = ListFields.AddListField("history", "Online");
            df.DisplayName = "历史";
            df.Url = "/IoT/DeviceHistory?deviceId={Id}";
        }

        {
            var df = ListFields.AddListField("property", "Online");
            df.DisplayName = "属性";
            df.Url = "/IoT/DeviceProperty?deviceId={Id}";
        }

        {
            var df = ListFields.AddListField("service", "Online");
            df.DisplayName = "服务";
            df.Url = "/IoT/DeviceService?deviceId={Id}";
        }

        {
            var df = ListFields.AddListField("data", "Online");
            df.DisplayName = "数据";
            df.Url = "/IoT/DeviceData?deviceId={Id}";
        }

        {
            var df = ListFields.AddListField("event", "Online");
            df.DisplayName = "事件";
            df.Url = "/IoT/DeviceEvent?deviceId={Id}";
        }
    }

    public DeviceController(ITracer tracer, PointImportService pointService)
    {
        _tracer = tracer;
        _pointService = pointService;
    }

    protected override IEnumerable<Device> Search(Pager p)
    {
        var id = p["Id"].ToInt(-1);
        if (id > 0)
        {
            var node = Device.FindById(id);
            if (node != null) return new[] { node };
        }

        var productId = p["productId"].ToInt(-1);
        var enable = p["enable"]?.ToBoolean();

        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        //// 如果没有指定产品和主设备，则过滤掉子设备
        //if (productId < 0 && parentId < 0) parentId = 0;

        return Device.Search(productId, enable, start, end, p["Q"], p);
    }

    protected override Boolean Valid(Device entity, DataObjectMethodType type, Boolean post)
    {
        var fs = type switch
        {
            DataObjectMethodType.Insert => AddFormFields,
            DataObjectMethodType.Update => EditFormFields,
            _ => null,
        };

        if (post)
        {
            // 使用驱动格式化配置数据
            var driver = entity.Product?.ProtocolType;
            if (!driver.IsNullOrEmpty())
                try
                {
                    var drv = DriverFactory.Create(driver, null);
                    var pm = drv?.GetDefaultParameter();
                    if (pm != null)
                        if (entity.Parameter.IsNullOrEmpty())
                            entity.Parameter = pm.ToJson(true);
                        else
                        {
                            // 添加缺失配置项
                            var dic = JsonParser.Decode(entity.Parameter);
                            var dic2 = pm.ToDictionary();
                            foreach (var item in dic2)
                                if (!dic.ContainsKey(item.Key)) dic[item.Key] = item.Value;
                            entity.Parameter = dic.ToJson(true);
                        }
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
        }

        return base.Valid(entity, type, post);
    }

    protected override Int32 OnInsert(Device entity)
    {
        var rs = base.OnInsert(entity);

        // 复制产品属性
        entity.Fix(true);

        entity.Product?.Fix();
        return rs;
    }

    protected override Int32 OnUpdate(Device entity)
    {
        var rs = base.OnUpdate(entity);

        entity.Fix(false);

        entity.Product?.Fix();

        return rs;
    }

    protected override Int32 OnDelete(Device entity)
    {
        // 删除设备时需要顺便把设备属性删除
        var dpList = DeviceProperty.FindAllByDeviceId(entity.Id);
        _ = dpList.Delete();

        var rs = base.OnDelete(entity);

        entity.Product?.Fix();

        return rs;
    }

    /// <summary>点位导入页面</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public ActionResult Import(Int32 id)
    {
        var dv = Device.FindById(id);

        return View("Import", dv);
    }

    /// <summary>点位数据上传导入</summary>
    /// <param name="model"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost]
    [EntityAuthorize(PermissionFlags.Insert)]
    public ActionResult Upload(PointImportModel model, IFormFile file)
    {
        using var span = _tracer?.NewSpan("DataImport", model);

        _pointService.Import(model, file.OpenReadStream());

        return RedirectToAction("Index", new { id = model.Id });
    }
}