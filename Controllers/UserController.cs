using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Xprint.Data;

namespace Xprint.Controllers
{
    //[Authorize(Roles = "SuperAdmin")] // Chốt chặn bảo mật
    public class UserController : Controller
    {
        private readonly ReportDbContext _context;

        public UserController(ReportDbContext context)
        {
            _context = context;
        }

        // Danh sách tài khoản
        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // Form thêm mới
        [HttpGet]
        public IActionResult Create() => View();

        // Xử lý lưu tài khoản mới
        [HttpPost]
        public IActionResult Create(UserItem model, string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword))
            {
                ViewBag.Error = "Mật khẩu không được để trống.";
                return View(model);
            }

            // Kiểm tra trùng lặp User
            if (_context.Users.Any(u => u.Username == model.Username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại!";
                return View(model);
            }

            // Băm mật khẩu trước khi lưu xuống DB
            model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            model.CreatedAt = System.DateTime.UtcNow;
            model.IsActive = true;

            _context.Users.Add(model);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}