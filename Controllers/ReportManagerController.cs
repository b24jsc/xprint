using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xprint.Data;

namespace Xprint.Controllers
{
    [Authorize]
    public class ReportManagerController : Controller
    {
        private readonly ReportDbContext _context;

        public ReportManagerController(ReportDbContext context)
        {
            _context = context;
        }

        // 1. GIAO DIỆN DANH SÁCH
        public IActionResult Index()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var tenantId = User.FindFirst("TenantId")?.Value;

            var query = _context.Reports.AsQueryable();

            // Phân quyền: Khách chỉ thấy report của mình, B24 Admin thấy hết
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

            // Chỉ nạp danh sách Tenant cho Dropdown nếu người đó là SuperAdmin
            if (role == "SuperAdmin")
            {
                ViewBag.Tenants = _context.Users.Select(u => u.TenantId).Distinct().ToList();
            }

            return View(reports);
        }

        // 2. TÍNH NĂNG EXPORT (Tải file .repx về máy)
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
                if (uploadFile == null || uploadFile.Length == 0)
                    return BadRequest("Lỗi: Không nhận diện được file đính kèm. Vui lòng chọn lại file.");

                if (string.IsNullOrWhiteSpace(reportName) || string.IsNullOrWhiteSpace(tenantId))
                    return BadRequest("Lỗi: Tên báo cáo và Mã khách hàng không được để trống.");

                var extension = Path.GetExtension(uploadFile.FileName);
                if (string.IsNullOrEmpty(extension) || extension.ToLower() != ".repx")
                    return BadRequest("Lỗi bảo mật: Hệ thống chỉ chấp nhận định dạng file .repx");

                using var memoryStream = new MemoryStream();
                await uploadFile.CopyToAsync(memoryStream);
                var layoutBytes = memoryStream.ToArray();

                var existingReport = _context.Reports.FirstOrDefault(r => r.Name == reportName && r.TenantId == tenantId);

                if (existingReport != null)
                {
                    existingReport.LayoutData = layoutBytes;
                    existingReport.UpdatedAt = DateTime.UtcNow;
                    _context.Reports.Update(existingReport); // Ép EF Core hiểu là đang update
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
                return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
            }
        }

        // 4. TÍNH NĂNG XÓA (Delete Report)
        [HttpPost]
        [ValidateAntiForgeryToken] // Chống giả mạo Request từ bên ngoài
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
    }
}