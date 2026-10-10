using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HerbalWellnessCenter.Models
{
    /// <summary>
    /// Enum biểu diễn trạng thái vận hành thực tế của Kỹ thuật viên
    /// </summary>
    public enum TherapistStatus
    {
        Available = 0,   // Sẵn sàng tiếp khách
        Busy = 1,        // Đang thực hiện liệu trình
        OnLeave = 2,     // Nghỉ phép / Không ca
        Inactive = 3     // Tạm ngưng hoạt động
    }

    [Table("Therapists")]
    public class Therapist
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên Kỹ thuật viên.")]
        [StringLength(100, ErrorMessage = "Tên Kỹ thuật viên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và Tên")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ.")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
        [StringLength(15, ErrorMessage = "Số điện thoại không quá 15 ký tự.")]
        [Display(Name = "Số Điện Thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100)]
        [Display(Name = "Email Liên Hệ")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng chỉ định chuyên môn chính.")]
        [StringLength(150)]
        [Display(Name = "Chuyên Môn Trị Liệu")]
        public string Specialty { get; set; } = "Massage & Bấm huyệt Cổ Vai Gáy";

        [Range(0, 50, ErrorMessage = "Số năm kinh nghiệm từ 0 đến 50 năm.")]
        [Display(Name = "Số Năm Kinh Nghiệm")]
        public int YearsOfExperience { get; set; } = 3;

        [Range(0.0, 5.0, ErrorMessage = "Đánh giá từ 0.0 đến 5.0 sao.")]
        [Column(TypeName = "decimal(3, 2)")]
        [Display(Name = "Đánh Giá Trung Bình")]
        public decimal Rating { get; set; } = 5.0m;

        [StringLength(255)]
        [Display(Name = "Ảnh Đại Diện")]
        public string AvatarUrl { get; set; } = "/images/therapists/default-avatar.png";

        [Display(Name = "Trạng Thái Sẵn Sàng")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Trạng Thái Vận Hành")]
        public TherapistStatus Status { get; set; } = TherapistStatus.Available;

        [StringLength(500)]
        [Display(Name = "Ghi Chú Vận Hành")]
        public string? Note { get; set; }

        [Display(Name = "Ngày Tạo Hồ Sơ")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Lần Cập Nhật Cuối")]
        public DateTime? UpdatedAt { get; set; }

        // ==========================================
        // ENTITY FRAMEWORK NAVIGATION PROPERTIES
        // ==========================================

        /// <summary>
        /// Danh sách các lịch hẹn được phân công cho Kỹ thuật viên này
        /// </summary>
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}