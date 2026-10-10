using System.ComponentModel.DataAnnotations;

namespace HerbalWellnessCenter.Models
{
    /// <summary>
    /// Bộ hằng số chuẩn hóa trạng thái đơn hàng (Booking Status Constants) 
    /// Đạt chuẩn Enterprise-grade, dùng cho cơ sở dữ liệu và logic điều phối vận hành.
    /// </summary>
    public static class BookingStatusConstants
    {
        public const string Pending = "Chờ xác nhận";
        public const string Deposited = "Đã cọc";
        public const string InProgress = "Đang phục vụ";
        public const string Completed = "Hoàn thành";
        public const string Cancelled = "Đã hủy";

        /// <summary>
        /// Danh sách toàn bộ các trạng thái phục vụ cho việc binding Dropdown hoặc bộ lọc nâng cao.
        /// </summary>
        public static readonly string[] AllStatuses = new[]
        {
            Pending,
            Deposited,
            InProgress,
            Completed,
            Cancelled
        };

        /// <summary>
        /// Các trạng thái hợp lệ cho phép hủy đơn hàng.
        /// </summary>
        public static readonly string[] CancellableStatuses = new[]
        {
            Pending,
            Deposited
        };
    }

    /// <summary>
    /// Bộ hằng số trạng thái thanh toán (Payment Status Constants)
    /// </summary>
    public static class PaymentStatusConstants
    {
        public const string Unpaid = "Chưa cọc";
        public const string Deposited = "Đã cọc";
        public const string Paid = "Đã thanh toán";
        public const string Refunded = "Đã hoàn tiền";
    }

    /// <summary>
    /// Enum định nghĩa trạng thái phục vụ an toàn kiểu dữ liệu (Type-safe Enum) 
    /// khi cần sử dụng trong các logic nghiệp vụ khắt khe.
    /// </summary>
    public enum BookingStatusEnum
    {
        [Display(Name = "Chờ xác nhận")]
        Pending = 1,

        [Display(Name = "Đã cọc")]
        Deposited = 2,

        [Display(Name = "Đang phục vụ")]
        InProgress = 3,

        [Display(Name = "Hoàn thành")]
        Completed = 4,

        [Display(Name = "Đã hủy")]
        Cancelled = 5
    }

    /// <summary>
    /// Các phương thức tiện ích mở rộng (Extension Methods) 
    /// Hỗ trợ tự động hóa UI (màu sắc Badge, Icon Bootstrap) chuẩn Top-tier.
    /// </summary>
    public static class BookingStatusExtensions
    {
        /// <summary>
        /// Trả về class CSS định danh Badge tương ứng với từng trạng thái.
        /// </summary>
        public static string ToBadgeClass(this string status) => status switch
        {
            BookingStatusConstants.Pending => "badge-pending",
            BookingStatusConstants.Deposited => "badge-deposit",
            BookingStatusConstants.InProgress => "badge-info",
            BookingStatusConstants.Completed => "badge-completed",
            BookingStatusConstants.Cancelled => "badge-cancelled",
            _ => "badge-secondary"
        };

        /// <summary>
        /// Trả về chuỗi class Bootstrap v5 hoàn chỉnh để render nhanh badge trạng thái trên view.
        /// </summary>
        public static string ToBootstrapBadge(this string status) => status switch
        {
            BookingStatusConstants.Pending => "bg-warning bg-opacity-10 text-dark border border-warning px-3 py-2 fw-bold rounded-pill",
            BookingStatusConstants.Deposited => "bg-success bg-opacity-10 text-success border border-success px-3 py-2 fw-bold rounded-pill",
            BookingStatusConstants.InProgress => "bg-info bg-opacity-10 text-dark border border-info px-3 py-2 fw-bold rounded-pill",
            BookingStatusConstants.Completed => "bg-primary bg-opacity-10 text-primary border border-primary px-3 py-2 fw-bold rounded-pill",
            BookingStatusConstants.Cancelled => "bg-danger bg-opacity-10 text-danger border border-danger px-3 py-2 fw-bold rounded-pill",
            _ => "bg-secondary text-white px-3 py-2 fw-bold rounded-pill"
        };

        /// <summary>
        /// Trả về tên icon Bootstrap Icons phù hợp với từng trạng thái vận hành.
        /// </summary>
        public static string ToIconClass(this string status) => status switch
        {
            BookingStatusConstants.Pending => "bi-clock-history",
            BookingStatusConstants.Deposited => "bi-check-circle",
            BookingStatusConstants.InProgress => "bi-arrow-repeat",
            BookingStatusConstants.Completed => "bi-check2-all",
            BookingStatusConstants.Cancelled => "bi-x-circle",
            _ => "bi-info-circle"
        };
    }
}