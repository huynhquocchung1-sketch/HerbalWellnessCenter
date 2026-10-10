using System.Threading.Tasks;
using HerbalWellnessCenter.Models;

namespace HerbalWellnessCenter.Services
{
    public interface IEmailSender
    {
        /// <summary>
        /// Gửi email HTML tổng quát với tiêu đề và nội dung tùy chỉnh
        /// </summary>
        Task SendEmailAsync(string email, string subject, string htmlMessage);

        /// <summary>
        /// Gửi Email xác nhận đặt lịch hẹn kèm Mã VietQR động nhận cọc
        /// </summary>
        Task SendBookingConfirmationEmailAsync(Booking booking, decimal depositAmount, string qrImageUrl);

        /// <summary>
        /// Gửi Email thông báo đã duyệt tiền cọc thành công
        /// </summary>
        Task SendDepositReceivedEmailAsync(Booking booking);

        /// <summary>
        /// Gửi Email nhắc nhở lịch hẹn trước giờ trải nghiệm
        /// </summary>
        Task SendBookingReminderEmailAsync(Booking booking);
    }
}