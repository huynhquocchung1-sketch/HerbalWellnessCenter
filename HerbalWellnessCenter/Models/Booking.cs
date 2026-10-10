using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HerbalWellnessCenter.Models
{
    [Table("Bookings")]
    public class Booking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Mã Đơn Hàng")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên khách hàng.")]
        [StringLength(100, ErrorMessage = "Tên khách hàng không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và Tên Khách Hàng")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ.")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
        [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự.")]
        [Display(Name = "Số Điện Thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ email nhận thông tin.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày trải nghiệm dịch vụ.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày Trải Nghiệm")]
        public DateTime BookingDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khung giờ phục vụ.")]
        [Display(Name = "Khung Giờ")]
        public TimeSpan BookingTime { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng khách tham gia.")]
        [Range(1, 20, ErrorMessage = "Số lượng khách mỗi đơn từ 1 đến 20 người.")]
        [Display(Name = "Số Lượng Khách")]
        public int NumberOfGuests { get; set; } = 1;

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
        [Display(Name = "Ghi Chú Yêu Cầu")]
        public string Note { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng Thái Đơn Hàng")]
        public string Status { get; set; } = "Chờ xác nhận"; // Chờ xác nhận, Đã cọc, Đang phục vụ, Hoàn thành, Đã hủy

        // ==========================================
        // TÍNH NĂNG TỰ ĐỘNG HÓA THANH TOÁN (WEBHOOK)
        // ==========================================

        [Required]
        [StringLength(30)]
        [Display(Name = "Trạng Thái Thanh Toán")]
        public string PaymentStatus { get; set; } = "Chưa cọc"; // "Chưa cọc", "Đã cọc", "Đã thanh toán"

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Số Tiền Cọc / Thanh Toán")]
        public decimal DepositAmount { get; set; } = 0;

        [StringLength(100)]
        [Display(Name = "Mã Giao Dịch Ngân Hàng (Ref/ID)")]
        public string? TransactionCode { get; set; }

        [StringLength(50)]
        [Display(Name = "Mã QR / Check-in ID")]
        public string? CheckInCode { get; set; }

        // ==========================================
        // KHÓA NGOẠI & QUAN HỆ LIÊN KẾT (RELATIONSHIPS)
        // ==========================================

        [Required(ErrorMessage = "Vui lòng chọn dịch vụ chăm sóc.")]
        [Display(Name = "Dịch Vụ Trị Liệu")]
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public virtual Service? Service { get; set; }

        // Phân công Kỹ thuật viên (Therapist)
        [Display(Name = "Mã Kỹ Thuật Viên Phụ Trách")]
        public int? TherapistId { get; set; }

        [ForeignKey("TherapistId")]
        public virtual Therapist? Therapist { get; set; }

        // Phân công Giường / Phòng (Bed)
        [Display(Name = "Mã Giường / Phòng Trị Liệu")]
        public int? BedId { get; set; }

        [ForeignKey("BedId")]
        public virtual Bed? Bed { get; set; }

        // ==========================================
        // THÔNG TIN KIỂM TOÁN (AUDIT TRAIL)
        // ==========================================

        [Display(Name = "Thời Gian Tạo Đơn")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Cập Nhật Lần Cuối")]
        public DateTime? UpdatedAt { get; set; }
        [Timestamp]
        [Display(Name = "Phiên Bản Dữ Liệu (RowVersion)")]
        public byte[]? RowVersion { get; set; }
    }
}