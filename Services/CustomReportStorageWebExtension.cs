using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.XtraReports.UI;
using Microsoft.AspNetCore.Http; // Cần thiết để lấy thông tin đăng nhập
using Xprint.PredefinedReports;
using Xprint.Data;

namespace Xprint.Services
{
    public class CustomReportStorageWebExtension : DevExpress.XtraReports.Web.Extensions.ReportStorageWebExtension
    {
        protected ReportDbContext DbContext { get; set; }
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Bổ sung IHttpContextAccessor vào constructor
        public CustomReportStorageWebExtension(ReportDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            this.DbContext = dbContext;
            this._httpContextAccessor = httpContextAccessor;
        }

        // Hàm xử lý logic lấy ID khách hàng hiện tại
        private string GetCurrentTenantId()
        {
            // Đọc thông tin từ user đang đăng nhập qua HttpContext
            var user = _httpContextAccessor.HttpContext?.User;

            if (user != null && user.Identity.IsAuthenticated)
            {
                // Lấy TenantId đã được đính kèm lúc người dùng Login
                var tenantClaim = user.FindFirst("TenantId");
                if (tenantClaim != null)
                {
                    return tenantClaim.Value; // Ví dụ trả về: "SPORTS_MEDIC" hoặc "VCN"
                }
            }

            // Nếu không có, ném lỗi bắt buộc phải đăng nhập đúng chuẩn
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

        public override byte[] GetData(string url)
        {
            var tenantId = GetCurrentTenantId();

            // 1. Tìm báo cáo trong Database theo ID và TenantId
            var reportData = DbContext.Reports.FirstOrDefault(x => x.Id == url && x.TenantId == tenantId);
            if (reportData != null)
                return reportData.LayoutData;

            // 2. Nếu không có trong DB, tìm trong thư mục PredefinedReports mẫu của DevExpress
            if (ReportsFactory.Reports.ContainsKey(url))
            {
                using var ms = new MemoryStream();
                using XtraReport report = ReportsFactory.Reports[url]();
                report.SaveLayoutToXml(ms);
                return ms.ToArray();
            }

            throw new DevExpress.XtraReports.Web.ClientControls.FaultException(string.Format("Could not find report '{0}'.", url));
        }

        public override Dictionary<string, string> GetUrls()
        {
            var tenantId = GetCurrentTenantId();

            // 1. Lấy báo cáo từ Database (Key = ID ẩn, Value = Tên hiển thị)
            var dbReports = DbContext.Reports
                .Where(x => x.TenantId == tenantId)
                .ToDictionary(x => x.Id, x => !string.IsNullOrEmpty(x.DisplayName) ? x.DisplayName : x.Name);

            // 2. Lấy các báo cáo mẫu từ thư mục PredefinedReports (TestReport)
            var factoryReports = ReportsFactory.Reports
                .ToDictionary(x => x.Key, x => x.Key);

            // 3. Gộp 2 danh sách lại để hiển thị lên giao diện
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

            // Kiểm tra xem báo cáo đã tồn tại và có đúng là của khách hàng này không
            var reportData = DbContext.Reports.FirstOrDefault(x => x.Id == url && x.TenantId == tenantId);

            if (reportData == null)
            {
                // Đề phòng trường hợp ghi đè mẫu PredefinedReport, ta tạo bản copy vào DB
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
                // Cập nhật báo cáo có sẵn
                reportData.LayoutData = stream.ToArray();
                reportData.UpdatedAt = DateTime.UtcNow;
            }
            DbContext.SaveChanges();
        }

        public override string SetNewData(XtraReport report, string defaultUrl)
        {
            var tenantId = GetCurrentTenantId();

            // Sinh mã GUID duy nhất để làm URL/ID, tránh trùng lặp tên giữa các khách hàng
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

            // Trả về newId để giao diện web load đúng địa chỉ
            return newId;
        }
    }
}