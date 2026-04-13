using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DevExpress.AspNetCore;
using DevExpress.AspNetCore.Reporting;
using DevExpress.Security.Resources;
using DevExpress.XtraReports.Web.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xprint.Services;
using Xprint.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. [THÊM MỚI QUAN TRỌNG] Đăng ký HttpContextAccessor
// Để class CustomReportStorage có thể đọc được thông tin (TenantId) của user đang đăng nhập
builder.Services.AddHttpContextAccessor();

builder.Services.AddDevExpressControls();
builder.Services.AddScoped<ReportStorageWebExtension, CustomReportStorageWebExtension>();
builder.Services.AddMvc();
builder.Services.ConfigureReportingServices(configurator => {
    if (builder.Environment.IsDevelopment())
    {
        configurator.UseDevelopmentMode();
    }
    configurator.ConfigureReportDesigner(designerConfigurator => {
        designerConfigurator.RegisterDataSourceWizardJsonConnectionStorage<CustomDataSourceWizardJsonDataConnectionStorage>(true);
        designerConfigurator.RegisterObjectDataSourceWizardTypeProvider<ObjectDataSourceWizardCustomTypeProvider>();
    });
    configurator.ConfigureWebDocumentViewer(viewerConfigurator => {
        viewerConfigurator.UseCachedReportSourceBuilder();
        viewerConfigurator.RegisterJsonDataConnectionProviderFactory<CustomJsonDataConnectionProviderFactory>();
        viewerConfigurator.RegisterConnectionProviderFactory<CustomSqlDataConnectionProviderFactory>();
    });
});

// 2. [SỬA LẠI] Kết nối PostgreSQL thay vì SQLite
// Đổi UseSqlite thành UseNpgsql và lấy đúng key "ReportDbConnection" trong appsettings.json
builder.Services.AddDbContext<ReportDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReportDbConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // Đường dẫn khi user chưa đăng nhập bị đá về
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Lưu phiên đăng nhập 8 tiếng
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    services.GetService<ReportDbContext>().InitializeDatabase();
}

var contentDirectoryAllowRule = DirectoryAccessRule.Allow(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Content")).FullName);
AccessSettings.ReportingSpecificResources.TrySetRules(contentDirectoryAllowRule, UrlAccessRule.Allow());
DevExpress.XtraReports.Configuration.Settings.Default.UserDesignerOptions.DataBindingMode = DevExpress.XtraReports.UI.DataBindingMode.Expressions;

app.UseDevExpressControls();
System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();


DevExpress.Utils.DeserializationSettings.RegisterTrustedClass(typeof(Xprint.Employees.DataSource));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ReportDbContext>();
    // Gõ nhẹ 1 câu lệnh để EF Core nạp Schema vào RAM trước
    dbContext.Database.CanConnect();
}

app.Run();