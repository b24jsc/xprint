using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Xprint.Data
{

    

    public class SqlDataConnectionDescription : DataConnection { }
    public class JsonDataConnectionDescription : DataConnection { }
    

    public abstract class DataConnection
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
    }


    public class ReportItem
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Dùng chuỗi để khớp với URL của DevExpress
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public byte[] LayoutData { get; set; } = Array.Empty<byte>();

        // Trường quan trọng để quản lý đa khách hàng
        public string TenantId { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ReportDbContext : DbContext
    {
        public DbSet<JsonDataConnectionDescription> JsonDataConnections { get; set; }
        public DbSet<SqlDataConnectionDescription> SqlDataConnections { get; set; }
        public DbSet<ReportItem> Reports { get; set; }
        public DbSet<UserItem> Users { get; set; }

        public ReportDbContext(DbContextOptions<ReportDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Đánh index cho TenantId để truy vấn nhanh khi hệ thống có nhiều khách hàng
            modelBuilder.Entity<ReportItem>().HasIndex(r => r.TenantId);
        }

        public void InitializeDatabase()
        {
            // Tự động khởi tạo cấu trúc bảng trên PostgreSQL
            Database.EnsureCreated();

            // Xóa các dữ liệu mẫu SQLite không cần thiết để giữ DB Xprint sạch sẽ
            SaveChanges();
        }
    }
}