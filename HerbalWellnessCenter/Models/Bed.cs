using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HerbalWellnessCenter.Models
{
    /// <summary>
    /// Enum quản lý trạng thái vận hành & vệ sinh của Giường/Bồn trị liệu
    /// </summary>
    public enum BedStatus
    {
        Available = 0,      // Trống - Sẵn sàng đón khách
        Occupied = 1,       // Đang có khách sử dụng
        Cleaning = 2,       // Đang dọn dẹp / Thay ga nệm & thảo dược
        Maintenance = 3     // Đang bảo trì / Tạm ngưng dịch vụ
    }

    /// <summary>
    /// Enum phân loại cơ sở vật chất trị liệu
    /// </summary>
    public enum BedType
    {
        StandardBed = 0,    // Giường massage / bấm huyệt tiêu chuẩn
        VipBed = 1,         // Giường VIP phòng riêng tư
        HerbalBathTub = 2,  // Bồn gỗ ngâm tắm thảo dược
        ShampooChair = 3    // Ghế/Giường gội đầu dưỡng sinh
    }

    [Table("Beds")]
    public class Bed
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã hoặc tên giường/bồn trị liệu.")]
        [StringLength(50, ErrorMessage = "Tên/Mã giường không được vượt quá 50 ký tự.")]
        [Display(Name = "Mã / Số Giường")]
        public string BedNumber { get; set; } = string.Empty; // Ví dụ: G-01, VIP-02, BON-01

        [Required(ErrorMessage = "Vui lòng nhập tên phòng trị liệu.")]
        [StringLength(100, ErrorMessage = "Tên phòng không vượt quá 100 ký tự.")]
        [Display(Name = "Phòng Trị Liệu")]
        public string RoomName { get; set; } = "Phòng Đông Y Thảo Dược 1";

        [StringLength(50)]
        [Display(Name = "Tầng / Phân Khu")]
        public string FloorLevel { get; set; } = "Tầng 1 - Khu Thảo Dược";

        [Display(Name = "Loại Thiết Bị / Giường")]
        public BedType Type { get; set; } = BedType.StandardBed;

        [Display(Name = "Đang Có Khách Sử Dụng")]
        public bool IsOccupied { get; set; } = false;

        [Display(Name = "Trạng Thái Vận Hành")]
        public BedStatus Status { get; set; } = BedStatus.Available;

        [StringLength(255)]
        [Display(Name = "Ghi Chú Vận Hành")]
        public string? Note { get; set; }

        [Display(Name = "Ngày Tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Lần Cập Nhật Cuối")]
        public DateTime? UpdatedAt { get; set; }

        // ==========================================
        // ENTITY FRAMEWORK NAVIGATION PROPERTIES
        // ==========================================

        /// <summary>
        /// Danh sách các lượt đặt lịch hẹn đã/đang sử dụng giường này
        /// </summary>
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}