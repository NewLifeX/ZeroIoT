using IoTZero.Services;
using NewLife.Caching;
using NewLife.Cube;
using NewLife.Log;
using XCode;

// 日志输出到控制台，并拦截全局异常
XTrace.UseConsole();

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// 配置星尘。借助StarAgent，或者读取配置文件 config/star.config 中的服务器地址、应用标识、密钥
var star = services.AddStardust(null);

// 把数据目录指向上层，例如部署到 /root/iot/edge/，这些目录放在 /root/iot/
var set = NewLife.Setting.Current;
if (set.IsNew)
{
    set.LogPath = "../Log";
    set.DataPath = "../Data";
    set.BackupPath = "../Backup";
    set.Save();
}
var set2 = CubeSetting.Current;
if (set2.IsNew)
{
    set2.AvatarPath = "../Avatars";
    set2.UploadPath = "../Uploads";
    set2.Save();
}
var set3 = XCodeSetting.Current;
if (set3.IsNew)
{
    set3.ShowSQL = false;
    set3.EntityCacheExpire = 60;
    set3.SingleCacheExpire = 60;
    set3.Save();
}

// 注册服务
services.AddSingleton<ThingService>();
services.AddSingleton<DataService>();
services.AddSingleton<QueueService>();

services.AddHttpClient("hc", e => e.Timeout = TimeSpan.FromSeconds(5));

services.AddSingleton<ICache, MemoryCache>();

// 后台服务

services.AddControllersWithViews();

// 引入魔方
services.AddCube();

var app = builder.Build();

// 预热数据层，执行反向工程建表等操作
EntityFactory.InitConnection("Membership");
EntityFactory.InitConnection("Log");
EntityFactory.InitConnection("Cube");
EntityFactory.InitConnection("IoT");

// 使用Cube前添加自己的管道
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/CubeHome/Error");

// 使用魔方
app.UseCube(app.Environment);

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=CubeHome}/{action=Index}/{id?}");
});

app.Run();
