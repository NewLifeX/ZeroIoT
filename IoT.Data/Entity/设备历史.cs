﻿using System;
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

/// <summary>设备历史。记录设备上线下线等操作</summary>
[Serializable]
[DataObject]
[Description("设备历史。记录设备上线下线等操作")]
[BindIndex("IX_DeviceHistory_DeviceId_Id", false, "DeviceId,Id")]
[BindIndex("IX_DeviceHistory_DeviceId_Action_Id", false, "DeviceId,Action,Id")]
[BindTable("DeviceHistory", Description = "设备历史。记录设备上线下线等操作", ConnName = "IoT", DbType = DatabaseType.None)]
public partial class DeviceHistory
{
    #region 属性
    private Int64 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, false, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int64 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int32 _DeviceId;
    /// <summary>设备</summary>
    [DisplayName("设备")]
    [Description("设备")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("DeviceId", "设备", "")]
    public Int32 DeviceId { get => _DeviceId; set { if (OnPropertyChanging("DeviceId", value)) { _DeviceId = value; OnPropertyChanged("DeviceId"); } } }

    private String _Name;
    /// <summary>名称</summary>
    [DisplayName("名称")]
    [Description("名称")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Name", "名称", "", Master = true)]
    public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

    private String _Action;
    /// <summary>操作</summary>
    [DisplayName("操作")]
    [Description("操作")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Action", "操作", "")]
    public String Action { get => _Action; set { if (OnPropertyChanging("Action", value)) { _Action = value; OnPropertyChanged("Action"); } } }

    private Boolean _Success;
    /// <summary>成功</summary>
    [DisplayName("成功")]
    [Description("成功")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Success", "成功", "")]
    public Boolean Success { get => _Success; set { if (OnPropertyChanging("Success", value)) { _Success = value; OnPropertyChanged("Success"); } } }

    private String _TraceId;
    /// <summary>追踪。用于记录调用链追踪标识，在APM查找调用链</summary>
    [DisplayName("追踪")]
    [Description("追踪。用于记录调用链追踪标识，在APM查找调用链")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("TraceId", "追踪。用于记录调用链追踪标识，在APM查找调用链", "")]
    public String TraceId { get => _TraceId; set { if (OnPropertyChanging("TraceId", value)) { _TraceId = value; OnPropertyChanged("TraceId"); } } }

    private String _Creator;
    /// <summary>创建者。服务端设备</summary>
    [DisplayName("创建者")]
    [Description("创建者。服务端设备")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Creator", "创建者。服务端设备", "")]
    public String Creator { get => _Creator; set { if (OnPropertyChanging("Creator", value)) { _Creator = value; OnPropertyChanged("Creator"); } } }

    private DateTime _CreateTime;
    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    [Description("创建时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

    private String _CreateIP;
    /// <summary>创建地址</summary>
    [DisplayName("创建地址")]
    [Description("创建地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateIP", "创建地址", "")]
    public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

    private String _Remark;
    /// <summary>内容</summary>
    [DisplayName("内容")]
    [Description("内容")]
    [DataObjectField(false, false, true, 2000)]
    [BindColumn("Remark", "内容", "")]
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
            "DeviceId" => _DeviceId,
            "Name" => _Name,
            "Action" => _Action,
            "Success" => _Success,
            "TraceId" => _TraceId,
            "Creator" => _Creator,
            "CreateTime" => _CreateTime,
            "CreateIP" => _CreateIP,
            "Remark" => _Remark,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToLong(); break;
                case "DeviceId": _DeviceId = value.ToInt(); break;
                case "Name": _Name = Convert.ToString(value); break;
                case "Action": _Action = Convert.ToString(value); break;
                case "Success": _Success = value.ToBoolean(); break;
                case "TraceId": _TraceId = Convert.ToString(value); break;
                case "Creator": _Creator = Convert.ToString(value); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "Remark": _Remark = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion

    #region 关联映射
    #endregion

    #region 字段名
    /// <summary>取得设备历史字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>设备</summary>
        public static readonly Field DeviceId = FindByName("DeviceId");

        /// <summary>名称</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>操作</summary>
        public static readonly Field Action = FindByName("Action");

        /// <summary>成功</summary>
        public static readonly Field Success = FindByName("Success");

        /// <summary>追踪。用于记录调用链追踪标识，在APM查找调用链</summary>
        public static readonly Field TraceId = FindByName("TraceId");

        /// <summary>创建者。服务端设备</summary>
        public static readonly Field Creator = FindByName("Creator");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>内容</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得设备历史字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>设备</summary>
        public const String DeviceId = "DeviceId";

        /// <summary>名称</summary>
        public const String Name = "Name";

        /// <summary>操作</summary>
        public const String Action = "Action";

        /// <summary>成功</summary>
        public const String Success = "Success";

        /// <summary>追踪。用于记录调用链追踪标识，在APM查找调用链</summary>
        public const String TraceId = "TraceId";

        /// <summary>创建者。服务端设备</summary>
        public const String Creator = "Creator";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>内容</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
