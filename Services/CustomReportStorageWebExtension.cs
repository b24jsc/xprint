using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.XtraReports.UI;
using Microsoft.AspNetCore.Http;
using Xprint.PredefinedReports;
using Xprint.Data;
using DevExpress.DataAccess.Json;

namespace Xprint.Services
{
    public class CustomReportStorageWebExtension : DevExpress.XtraReports.Web.Extensions.ReportStorageWebExtension
    {
        protected ReportDbContext DbContext { get; set; }
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomReportStorageWebExtension(ReportDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            this.DbContext = dbContext;
            this._httpContextAccessor = httpContextAccessor;
        }

        private string GetCurrentTenantId()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user != null && user.Identity.IsAuthenticated)
            {
                var tenantClaim = user.FindFirst("TenantId");
                if (tenantClaim != null)
                {
                    return tenantClaim.Value;
                }
            }

            throw new UnauthorizedAccessException("Người dùng không thuộc tổ chức (Tenant) nào.");
        }

        public override bool CanSetData(string url)
        {
            return true;
        }

        public override bool IsValidUrl(string url)
        {
            return true;
        }

        // ĐÃ NÂNG CẤP HÀM NÀY ĐỂ "BẺ KHÓA" ĐƯỜNG DẪN JSON VẬT LÝ
        public override byte[] GetData(string url)
        {
            var tenantId = GetCurrentTenantId();

            // 1. Tìm layout trong DB
            var reportData = DbContext.Reports.FirstOrDefault(x => x.Id == url && x.TenantId == tenantId);

            XtraReport report;
            string jsonSchema = "";

            if (reportData != null)
            {
                // Nếu là báo cáo từ DB, lấy Layout và JSON Schema tương ứng
                using var ms = new MemoryStream(reportData.LayoutData);
                report = XtraReport.FromStream(ms);
                jsonSchema = reportData.JsonSchemaData;
            }
            else if (ReportsFactory.Reports.ContainsKey(url))
            {
                // Nếu là báo cáo mẫu (Predefined), lấy từ Factory
                report = ReportsFactory.Reports[url]();
            }
            else
            {
                // Trường hợp xấu nhất không thấy gì thì trả về mẫu trắng để Designer không bị sập
                report = new XtraReport();
            }

            // 2. "MA THUẬT": Tự động nhồi JSON vào danh sách Fields
            if (!string.IsNullOrWhiteSpace(jsonSchema))
            {
                // Tìm xem báo cáo có cái JsonDataSource nào chưa
                var jsonDS = report.ComponentStorage.OfType<JsonDataSource>().FirstOrDefault();

                if (jsonDS == null)
                {
                    // Nếu chưa có, tạo mới một cái tên là "Dữ liệu mẫu" để khách hàng kéo thả
                    jsonDS = new JsonDataSource { Name = "Dữ liệu mẫu (Auto)" };
                    report.ComponentStorage.Add(jsonDS);
                    report.DataSource = jsonDS;
                }
                jsonDS.ConnectionName = string.Empty;
                // Ghi đè cấu trúc JSON mới nhất từ Database vào
                jsonDS.JsonSource = new CustomJsonSource(jsonSchema);
                jsonDS.Fill(); // Dịch JSON thành các cột (Fields) ngay lập tức
            }

            // 3. Lưu lại và trả về byte array cho Designer hiển thị
            using var resultStream = new MemoryStream();
            report.SaveLayoutToXml(resultStream);
            return resultStream.ToArray();
        }

        public override Dictionary<string, string> GetUrls()
        {
            var tenantId = GetCurrentTenantId();

            var dbReports = DbContext.Reports
                .Where(x => x.TenantId == tenantId)
                .ToDictionary(x => x.Id, x => !string.IsNullOrEmpty(x.DisplayName) ? x.DisplayName : x.Name);

            var factoryReports = ReportsFactory.Reports
                .ToDictionary(x => x.Key, x => x.Key);

            var result = new Dictionary<string, string>();

            foreach (var item in factoryReports)
            {
                result[item.Key] = item.Value;
            }

            foreach (var item in dbReports)
            {
                result[item.Key] = item.Value;
            }

            return result;
        }

        public override void SetData(XtraReport report, string url)
        {
            var tenantId = GetCurrentTenantId();
            using var stream = new MemoryStream();
            report.SaveLayoutToXml(stream);

            var reportData = DbContext.Reports.FirstOrDefault(x => x.Id == url && x.TenantId == tenantId);

            if (reportData == null)
            {
                DbContext.Reports.Add(new ReportItem
                {
                    Id = url,
                    Name = url,
                    DisplayName = url,
                    LayoutData = stream.ToArray(),
                    TenantId = tenantId,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                reportData.LayoutData = stream.ToArray();
                reportData.UpdatedAt = DateTime.UtcNow;
            }
            DbContext.SaveChanges();
        }

        public override string SetNewData(XtraReport report, string defaultUrl)
        {
            var tenantId = GetCurrentTenantId();
            var newId = Guid.NewGuid().ToString();

            using var stream = new MemoryStream();
            report.SaveLayoutToXml(stream);

            DbContext.Reports.Add(new ReportItem
            {
                Id = newId,
                Name = defaultUrl,
                DisplayName = defaultUrl,
                LayoutData = stream.ToArray(),
                TenantId = tenantId,
                UpdatedAt = DateTime.UtcNow
            });

            DbContext.SaveChanges();
            return newId;
        }
    }
}