using DevExpress.XtraReports.Web.ReportDesigner.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xprint.Data;
using DevExpress.DataAccess.Json;
namespace Xprint.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {   
        private readonly ReportDbContext _context;
        public ReportsController(ReportDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "SuperAdmin")
            {
                ViewBag.Tenants = _context.Users.Select(u => u.TenantId).Distinct().ToList();
            }
            return View();
        }

        public IActionResult Design(
            [FromServices] IReportDesignerModelBuilder reportDesignerModelBuilder,
            [FromQuery] string reportName)
        {

            reportName = string.IsNullOrEmpty(reportName) ? "TestReport" : reportName;
            var designerModel = reportDesignerModelBuilder
                .Report(reportName)
                .BuildModel();
            return View(designerModel);
        }

        [HttpGet]
        public IActionResult Export(string id)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var tenantId = User.FindFirst("TenantId")?.Value;

            var report = _context.Reports.FirstOrDefault(r => r.Id == id);
            if (report == null || report.LayoutData == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu báo cáo.";
                return RedirectToAction(nameof(Index));
            }

            // Bảo mật chéo: Khách không thể gõ ID bậy bạ trên URL để tải file của khách khác
            if (role != "SuperAdmin" && report.TenantId != tenantId)
            {
                TempData["Error"] = "Bạn không có quyền tải báo cáo này.";
                return RedirectToAction(nameof(Index));
            }

            string fileName = $"{report.Name}_{report.TenantId}.repx";
            return File(report.LayoutData, "application/octet-stream", fileName);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import([FromForm] IFormFile uploadFile, [FromForm] string reportName, [FromForm] string tenantId)
        {
            try
            {
                // 1. Validate cơ bản
                if (uploadFile == null || uploadFile.Length == 0)
                    return BadRequest("Lỗi: Không nhận diện được file đính kèm.");

                if (string.IsNullOrWhiteSpace(reportName) || string.IsNullOrWhiteSpace(tenantId))
                    return BadRequest("Lỗi: Tên báo cáo và Mã khách hàng không được để trống.");

                var extension = Path.GetExtension(uploadFile.FileName);
                if (string.IsNullOrEmpty(extension) || extension.ToLower() != ".repx")
                    return BadRequest("Lỗi bảo mật: Hệ thống chỉ chấp nhận định dạng file .repx");

                // 2. Chuyển file vào RAM
                using var memoryStream = new MemoryStream();
                await uploadFile.CopyToAsync(memoryStream);
                var layoutBytes = memoryStream.ToArray();

                // 3. Xử lý Database
                var existingReport = _context.Reports.FirstOrDefault(r => r.Name == reportName && r.TenantId == tenantId);

                if (existingReport != null)
                {
                    existingReport.LayoutData = layoutBytes;
                    existingReport.UpdatedAt = DateTime.UtcNow;
                    _context.Reports.Update(existingReport);
                }
                else
                {
                    _context.Reports.Add(new ReportItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = reportName,
                        DisplayName = reportName,
                        LayoutData = layoutBytes,
                        TenantId = tenantId,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                return Ok("Upload thành công!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi Server: " + ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                var tenantId = User.FindFirst("TenantId")?.Value;

                // 1. Tìm báo cáo trong Database
                var report = _context.Reports.FirstOrDefault(r => r.Id == id);
                if (report == null)
                {
                    return NotFound("Không tìm thấy báo cáo này trên hệ thống.");
                }

                // 2. Chốt chặn Bảo mật chéo: Khách nào chỉ được xóa file của khách đó (Trừ SuperAdmin)
                if (role != "SuperAdmin" && report.TenantId != tenantId)
                {
                    return Forbid("Lỗi bảo mật: Bạn không có quyền xóa mẫu in của khách hàng khác.");
                }

                // 3. Thực thi xóa
                _context.Reports.Remove(report);
                await _context.SaveChangesAsync();

                return Ok("Đã xóa báo cáo thành công.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi Server khi xóa: " + ex.Message);
            }
        }

        [HttpGet]
        public IActionResult GetReportsList()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var tenantId = User.FindFirst("TenantId")?.Value;

            var query = _context.Reports.AsQueryable();

            if (role != "SuperAdmin")
            {
                query = query.Where(r => r.TenantId == tenantId);
            }

            var reports = query.Select(r => new
            {
                r.Id,
                r.Name,
                r.TenantId,
                r.UpdatedAt,
                SizeKB = r.LayoutData != null ? r.LayoutData.Length / 1024 : 0
            }).ToList();

            return Json(reports);
        }

        [HttpGet]
        public IActionResult GetDetailPartial(string id)
        {
            var report = _context.Reports.FirstOrDefault(r => r.Id == id);
            if (report == null) return NotFound("<div class='alert alert-danger'>Không tìm thấy báo cáo.</div>");

            // Nếu JSON rỗng thì cho cái ngoặc nhọn mặc định
            if (string.IsNullOrWhiteSpace(report.JsonSchemaData))
            {
                report.JsonSchemaData = "{\n  \n}";
            }

            // Trả về HTML của file _Detail.cshtml, kèm theo dữ liệu Model
            return PartialView("_Detail", report);
        }

        [HttpGet]
        public IActionResult GetJsonSchema(string id)
        {
            var report = _context.Reports.FirstOrDefault(r => r.Id == id);
            if (report == null) return NotFound("Không tìm thấy báo cáo.");

            // Nếu chưa có JSON, trả về một mẫu mặc định cho họ dễ hình dung
            var jsonContent = string.IsNullOrWhiteSpace(report.JsonSchemaData)
                ? "{\n  \"Id\": 1,\n  \"Name\": \"Dữ liệu mẫu\"\n}"
                : report.JsonSchemaData;

            return Content(jsonContent, "application/json");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateJsonSchema([FromForm] string id, [FromForm] string jsonContent)
        {
            try
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                var tenantId = User.FindFirst("TenantId")?.Value;

                var report = _context.Reports.FirstOrDefault(r => r.Id == id);
                if (report == null) return NotFound("Không tìm thấy báo cáo.");

                // Bảo mật chéo
                if (role != "SuperAdmin" && report.TenantId != tenantId)
                    return Forbid("Bạn không có quyền sửa dữ liệu này.");

                // Kiểm tra xem chuỗi họ nhập vào có phải là JSON hợp lệ không (Basic Validation)
                if (!string.IsNullOrWhiteSpace(jsonContent))
                {
                    jsonContent = jsonContent.Trim();
                    if ((!jsonContent.StartsWith("{") || !jsonContent.EndsWith("}")) &&
                        (!jsonContent.StartsWith("[") || !jsonContent.EndsWith("]")))
                    {
                        return BadRequest("Dữ liệu không đúng định dạng JSON (phải bắt đầu bằng { hoặc [).");
                    }
                }

                report.JsonSchemaData = jsonContent;
                report.UpdatedAt = DateTime.UtcNow;

                _context.Reports.Update(report);
                await _context.SaveChangesAsync();

                return Ok("Đã lưu cấu trúc JSON thành công!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi Server: " + ex.Message);
            }
        }
    }
}
