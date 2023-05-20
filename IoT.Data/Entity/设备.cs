using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace IoT.Data;

/// <summary>设备。归属于某个产品下的具体设备。物联网平台为设备颁发产品内唯一的证书DeviceName。设备可以直接连接物联网平台，也可以作为子设备通过网关连接物联网平台。</summary>
[Serializable]
[DataObject]
[Description("设备。归属于某个产品下的具体设备。物联网平台为设备颁发产品内唯一的证书DeviceName。设备可以直接连接物联网平台，也可以作为子设备通过网关连接物联网平台。")]
[BindIndex("IU_Device_Code", true, "Code")]
[BindIndex("IX_Device_ProductId", false, "ProductId")]
[BindIndex("IX_Device_Uuid", false, "Uuid")]
[BindIndex("IX_Device_UpdateTime", false, "UpdateTime")]
[BindTable("Device", Description = "设备。归属于某个产品下的具体设备。物联网平台为设备颁发产品内唯一的证书DeviceName。设备可以直接连接物联网平台，也可以作为子设备通过网关连接物联网平台。", ConnName = "IoT", DbType = DatabaseType.None)]
public partial class Device
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private String _Name;
    /// <summary>名称</summary>
    [DisplayName("名称")]
    [Description("名称")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Name", "名称", "", Master = true)]
    public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

    private String _Code;
    /// <summary>编码。设备唯一证书DeviceName，用于设备认证，在注册时由系统生成</summary>
    [DisplayName("编码")]
    [Description("编码。设备唯一证书DeviceName，用于设备认证，在注册时由系统生成")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Code", "编码。设备唯一证书DeviceName，用于设备认证，在注册时由系统生成", "")]
    public String Code { get => _Code; set { if (OnPropertyChanging("Code", value)) { _Code = value; OnPropertyChanged("Code"); } } }

    private Int32 _ProductId;
    /// <summary>产品</summary>
    [DisplayName("产品")]
    [Description("产品")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ProductId", "产品", "")]
    public Int32 ProductId { get => _ProductId; set { if (OnPropertyChanging("ProductId", value)) { _ProductId = value; OnPropertyChanged("ProductId"); } } }

    private Boolean _Enable;
    /// <summary>启用</summary>
    [DisplayName("启用")]
    [Description("启用")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Enable", "启用", "")]
    public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

    private Boolean _Online;
    /// <summary>在线</summary>
    [DisplayName("在线")]
    [Description("在线")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Online", "在线", "")]
    public Boolean Online { get => _Online; set { if (OnPropertyChanging("Online", value)) { _Online = value; OnPropertyChanged("Online"); } } }

    private String _Uuid;
    /// <summary>唯一标识。硬件标识，或其它能够唯一区分设备的标记</summary>
    [DisplayName("唯一标识")]
    [Description("唯一标识。硬件标识，或其它能够唯一区分设备的标记")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Uuid", "唯一标识。硬件标识，或其它能够唯一区分设备的标记", "")]
    public String Uuid { get => _Uuid; set { if (OnPropertyChanging("Uuid", value)) { _Uuid = value; OnPropertyChanged("Uuid"); } } }

    private String _Location;
    /// <summary>位置。场地安装位置，或者经纬度</summary>
    [Category("登录信息")]
    [DisplayName("位置")]
    [Description("位置。场地安装位置，或者经纬度")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Location", "位置。场地安装位置，或者经纬度", "")]
    public String Location { get => _Location; set { if (OnPropertyChanging("Location", value)) { _Location = value; OnPropertyChanged("Location"); } } }

    private Int32 _PollingTime;
    /// <summary>采集间隔。默认1000ms</summary>
    [Category("参数设置")]
    [DisplayName("采集间隔")]
    [Description("采集间隔。默认1000ms")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("PollingTime", "采集间隔。默认1000ms", "")]
    public Int32 PollingTime { get => _PollingTime; set { if (OnPropertyChanging("PollingTime", value)) { _PollingTime = value; OnPropertyChanged("PollingTime"); } } }

    private Int32 _CreateUserId;
    /// <summary>创建者</summary>
    [Category("扩展")]
    [DisplayName("创建者")]
    [Description("创建者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CreateUserId", "创建者", "")]
    public Int32 CreateUserId { get => _CreateUserId; set { if (OnPropertyChanging("CreateUserId", value)) { _CreateUserId = value; OnPropertyChanged("CreateUserId"); } } }

    private DateTime _CreateTime;
    /// <summary>创建时间</summary>
    [Category("扩展")]
    [DisplayName("创建时间")]
    [Description("创建时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

    private String _CreateIP;
    /// <summary>创建地址</summary>
    [Category("扩展")]
    [DisplayName("创建地址")]
    [Description("创建地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateIP", "创建地址", "")]
    public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

    private Int32 _UpdateUserId;
    /// <summary>更新者</summary>
    [Category("扩展")]
    [DisplayName("更新者")]
    [Description("更新者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UpdateUserId", "更新者", "")]
    public Int32 UpdateUserId { get => _UpdateUserId; set { if (OnPropertyChanging("UpdateUserId", value)) { _UpdateUserId = value; OnPropertyChanged("UpdateUserId"); } } }

    private DateTime _UpdateTime;
    /// <summary>更新时间</summary>
    [Category("扩展")]
    [DisplayName("更新时间")]
    [Description("更新时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("UpdateTime", "更新时间", "")]
    public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

    private String _UpdateIP;
    /// <summary>更新地址</summary>
    [Category("扩展")]
    [DisplayName("更新地址")]
    [Description("更新地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UpdateIP", "更新地址", "")]
    public String UpdateIP { get => _UpdateIP; set { if (OnPropertyChanging("UpdateIP", value)) { _UpdateIP = value; OnPropertyChanged("UpdateIP"); } } }

    private String _Remark;
    /// <summary>描述</summary>
    [Category("扩展")]
    [DisplayName("描述")]
    [Description("描述")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Remark", "描述", "")]
    public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
    #endregion

    #region 获取/设置 字段值
    /// <summary>获取/设置 字段值</summary>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public override Object this[String name]
    {
        get => name switch
        {
            "Id" => _Id,
            "Name" => _Name,
            "Code" => _Code,
            "ProductId" => _ProductId,
            "Enable" => _Enable,
            "Online" => _Online,
            "Uuid" => _Uuid,
            "Location" => _Location,
            "PollingTime" => _PollingTime,
            "CreateUserId" => _CreateUserId,
            "CreateTime" => _CreateTime,
            "CreateIP" => _CreateIP,
            "UpdateUserId" => _UpdateUserId,
            "UpdateTime" => _UpdateTime,
            "UpdateIP" => _UpdateIP,
            "Remark" => _Remark,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToInt(); break;
                case "Name": _Name = Convert.ToString(value); break;
                case "Code": _Code = Convert.ToString(value); break;
                case "ProductId": _ProductId = value.ToInt(); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "Online": _Online = value.ToBoolean(); break;
                case "Uuid": _Uuid = Convert.ToString(value); break;
                case "Location": _Location = Convert.ToString(value); break;
                case "PollingTime": _PollingTime = value.ToInt(); break;
                case "CreateUserId": _CreateUserId = value.ToInt(); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "UpdateUserId": _UpdateUserId = value.ToInt(); break;
                case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                case "Remark": _Remark = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion

    #region 关联映射
    #endregion

    #region 字段名
    /// <summary>取得设备字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>名称</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>编码。设备唯一证书DeviceName，用于设备认证，在注册时由系统生成</summary>
        public static readonly Field Code = FindByName("Code");

        /// <summary>产品</summary>
        public static readonly Field ProductId = FindByName("ProductId");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>在线</summary>
        public static readonly Field Online = FindByName("Online");

        /// <summary>唯一标识。硬件标识，或其它能够唯一区分设备的标记</summary>
        public static readonly Field Uuid = FindByName("Uuid");

        /// <summary>位置。场地安装位置，或者经纬度</summary>
        public static readonly Field Location = FindByName("Location");

        /// <summary>采集间隔。默认1000ms</summary>
        public static readonly Field PollingTime = FindByName("PollingTime");

        /// <summary>创建者</summary>
        public static readonly Field CreateUserId = FindByName("CreateUserId");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>更新者</summary>
        public static readonly Field UpdateUserId = FindByName("UpdateUserId");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        /// <summary>描述</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得设备字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>名称</summary>
        public const String Name = "Name";

        /// <summary>编码。设备唯一证书DeviceName，用于设备认证，在注册时由系统生成</summary>
        public const String Code = "Code";

        /// <summary>产品</summary>
        public const String ProductId = "ProductId";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>在线</summary>
        public const String Online = "Online";

        /// <summary>唯一标识。硬件标识，或其它能够唯一区分设备的标记</summary>
        public const String Uuid = "Uuid";

        /// <summary>位置。场地安装位置，或者经纬度</summary>
        public const String Location = "Location";

        /// <summary>采集间隔。默认1000ms</summary>
        public const String PollingTime = "PollingTime";

        /// <summary>创建者</summary>
        public const String CreateUserId = "CreateUserId";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>更新者</summary>
        public const String UpdateUserId = "UpdateUserId";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";

        /// <summary>描述</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
