using DevExpress.XtraReports.Web.ReportDesigner.Services;
using DevExpress.XtraReports.Web.WebDocumentViewer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using Xprint.Data;

namespace Xprint.Controllers {
    [Authorize]
    public class HomeController : Controller {
        private readonly ReportDbContext _context;
        public HomeController(ReportDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            // Lấy thông tin người đang đăng nhập
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var tenantId = User.FindFirst("TenantId")?.Value;

            var query = _context.Reports.AsQueryable();

            // CHỐT CHẶN BẢO MẬT: Khách hàng chỉ xem được số liệu của mình
            if (role != "SuperAdmin")
            {
                query = query.Where(r => r.TenantId == tenantId);
            }

            // Nhóm dữ liệu theo Khách hàng và đếm số lượng
            var stats = query
                .GroupBy(r => r.TenantId)
                .Select(g => new
                {
                    TenantId = g.Key,
                    TotalReports = g.Count(),
                    LastUpdated = g.Max(r => r.UpdatedAt)
                })
                .OrderByDescending(x => x.TotalReports)
                .ToList();

            return View(stats);
        }
        public IActionResult Error() {
            Models.ErrorModel model = new Models.ErrorModel();
            return View(model);
        }
        
        public IActionResult ReportDesigner(
            [FromServices] IReportDesignerModelBuilder reportDesignerModelBuilder, 
            [FromQuery] string reportName) {

            reportName = string.IsNullOrEmpty(reportName) ? "TestReport" : reportName;
            var designerModel = reportDesignerModelBuilder
                .Report(reportName)
                .BuildModel();
            return View(designerModel);
        }

        public IActionResult DocumentViewer(
            [FromServices] IWebDocumentViewerClientSideModelGenerator viewerModelGenerator,
            [FromQuery] string reportName) {
            reportName = string.IsNullOrEmpty(reportName) ? "TestReport" : reportName;
            var viewerModel = viewerModelGenerator.GetModel(reportName, CustomWebDocumentViewerController.DefaultUri);
            return View(viewerModel);
        }
    }
}