using HerbalWellnessCenter.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HerbalWellnessCenter.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Khai báo các DbSet đại diện cho các bảng trong CSDL
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<Therapist> Therapists { get; set; } = null!;
        public DbSet<Bed> Beds { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Cấu hình Bảng Category (4 Không gian chính)
            builder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
                entity.Property(c => c.Description).HasMaxLength(500);
                entity.Property(c => c.ImageUrl).HasMaxLength(255);
            });

            // 2. Cấu hình Bảng Service (Gói dịch vụ trị liệu)
            builder.Entity<Service>(entity =>
            {
                entity.ToTable("Services");
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Title).IsRequired().HasMaxLength(200);
                entity.Property(s => s.Description).HasMaxLength(1000);
                entity.Property(s => s.ImageUrl).HasMaxLength(255);

                // Khắc phục triệt để lỗi Decimal Precision
                entity.Property(s => s.Price)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                // Quan hệ 1 Category - Nhiều Services
                entity.HasOne(s => s.Category)
                      .WithMany(c => c.Services)
                      .HasForeignKey(s => s.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 3. Cấu hình Bảng Therapist (Kỹ thuật viên)
            builder.Entity<Therapist>(entity =>
            {
                entity.ToTable("Therapists");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
                entity.Property(t => t.PhoneNumber).IsRequired().HasMaxLength(15);
                entity.Property(t => t.Email).HasMaxLength(100);
                entity.Property(t => t.Specialty).HasMaxLength(150);
                entity.Property(t => t.AvatarUrl).HasMaxLength(255);
                entity.Property(t => t.Rating).HasColumnType("decimal(3,2)");

                entity.HasIndex(t => t.IsAvailable);
            });

            // 4. Cấu hình Bảng Bed (Giường / Phòng trị liệu)
            builder.Entity<Bed>(entity =>
            {
                entity.ToTable("Beds");
                entity.HasKey(b => b.Id);
                entity.Property(b => b.BedNumber).IsRequired().HasMaxLength(50);
                entity.Property(b => b.RoomName).IsRequired().HasMaxLength(100);
                entity.Property(b => b.FloorLevel).HasMaxLength(50);

                entity.HasIndex(b => b.IsOccupied);
            });

            // 5. Cấu hình Bảng Booking (Đơn đặt lịch hẹn & Điều phối tài nguyên)
            builder.Entity<Booking>(entity =>
            {
                entity.ToTable("Bookings");
                entity.HasKey(b => b.Id);

                entity.Property(b => b.CustomerName).IsRequired().HasMaxLength(100);
                entity.Property(b => b.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(b => b.Email).IsRequired().HasMaxLength(100);
                entity.Property(b => b.Status).HasMaxLength(50).HasDefaultValue("Chờ xác nhận");
                entity.Property(b => b.Note).HasMaxLength(1000);

                // Tối ưu tốc độ tìm kiếm (Indexes) cho trang Admin
                entity.HasIndex(b => b.BookingDate);
                entity.HasIndex(b => b.PhoneNumber);
                entity.HasIndex(b => b.Status);

                // Quan hệ 1 Service - Nhiều Bookings
                entity.HasOne(b => b.Service)
                      .WithMany()
                      .HasForeignKey(b => b.ServiceId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Quan hệ Kỹ thuật viên phụ trách (Therapist)
                entity.HasOne(b => b.Therapist)
                      .WithMany(t => t.Bookings)
                      .HasForeignKey(b => b.TherapistId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Quan hệ Giường / Phòng điều phối (Bed)
                entity.HasOne(b => b.Bed)
                      .WithMany(bed => bed.Bookings)
                      .HasForeignKey(b => b.BedId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}