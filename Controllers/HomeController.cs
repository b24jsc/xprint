using DevExpress.XtraReports.Web.ReportDesigner.Services;
using DevExpress.XtraReports.Web.WebDocumentViewer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Xprint.Controllers {
    [Authorize]
    public class HomeController : Controller {
        public IActionResult Index() {
            return View();
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