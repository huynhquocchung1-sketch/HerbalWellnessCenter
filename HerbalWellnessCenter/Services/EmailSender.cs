using HerbalWellnessCenter.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace HerbalWellnessCenter.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // ==========================================
        // 1. HÀM GỬI EMAIL NỀN TẢNG (CORE SMTP)
        // ==========================================
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:Server"] ?? "smtp.gmail.com";
                var port = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "info@herbalwellness.com";
                var senderPassword = _configuration["EmailSettings:SenderPassword"] ?? "";
                var senderName = _configuration["EmailSettings:SenderName"] ?? "Herbal Wellness Center";

                using var client = new SmtpClient(smtpServer, port)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", email, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending email to {Email}", email);
            }
        }

        // ==========================================
        // 2. EMAIL XÁC NHẬN ĐẶT LỊCH KÈM VIETQR ĐỘNG
        // ==========================================
        public async Task SendBookingConfirmationEmailAsync(Booking booking, decimal depositAmount, string qrImageUrl)
        {
            string subject = $"[Herbal Wellness] Xác nhận phiếu đặt lịch hẹn #{booking.Id} - Trải nghiệm Dưỡng Sinh";

            string htmlMessage = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: 'Plus Jakarta Sans', Arial, sans-serif; background-color: #fbf9f5; margin: 0; padding: 20px; color: #1f2937; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); border: 1px solid #e5e7eb; }}
        .header {{ background-color: #234d20; padding: 30px 20px; text-align: center; color: #ffffff; }}
        .header h1 {{ margin: 0; font-family: 'Playfair Display', Georgia, serif; font-size: 24px; letter-spacing: 1px; color: #c5a059; }}
        .header p {{ margin: 5px 0 0 0; font-size: 13px; opacity: 0.9; font-style: italic; }}
        .content {{ padding: 30px 25px; }}
        .greeting {{ font-size: 18px; font-weight: bold; color: #234d20; margin-bottom: 15px; }}
        .info-card {{ background-color: #fcfbf7; border-left: 4px solid #c5a059; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .info-label {{ color: #6b7280; font-weight: 500; }}
        .info-value {{ font-weight: 700; color: #1f2937; text-align: right; }}
        .qr-section {{ text-align: center; background-color: #f4f6f3; padding: 25px; border-radius: 12px; margin: 25px 0; border: 1px dashed #234d20; }}
        .qr-title {{ color: #234d20; font-weight: bold; font-size: 16px; margin-bottom: 8px; }}
        .qr-code {{ width: 210px; height: 210px; border-radius: 12px; border: 2px solid #ffffff; box-shadow: 0 4px 10px rgba(0,0,0,0.1); margin: 12px 0; }}
        .footer {{ background-color: #142a12; color: #9ca3af; padding: 25px; text-align: center; font-size: 12px; line-height: 1.6; }}
        .footer a {{ color: #c5a059; text-decoration: none; }}
        .badge {{ background-color: #fff3cd; color: #856404; padding: 4px 12px; border-radius: 50px; font-weight: bold; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🌿 HERBAL WELLNESS CENTER</h1>
            <p>""Nature heals from within - Phục hồi Thân Tâm Trí""</p>
        </div>
        <div class=""content"">
            <div class=""greeting"">Kính gửi Quý khách {booking.CustomerName},</div>
            <p>Chân thành cảm ơn Quý khách đã lựa chọn dịch vụ dưỡng sinh cổ truyền tại <strong>Herbal Wellness Center</strong>. Yêu cầu đặt lịch của Quý khách đã được tiếp nhận thành công với thông tin chi tiết dưới đây:</p>
            
            <div class=""info-card"">
                <table width=""100%"" cellpadding=""6"" cellspacing=""0"" style=""border-collapse: collapse;"">
                    <tr>
                        <td class=""info-label"">Mã phiếu hẹn:</td>
                        <td class=""info-value"" style=""color: #234d20;"">#HERBAL-{booking.Id}</td>
                    </tr>
                    <tr>
                        <td class=""info-label"">Số điện thoại:</td>
                        <td class=""info-value"">{booking.PhoneNumber}</td>
                    </tr>
                    <tr>
                        <td class=""info-label"">Ngày trải nghiệm:</td>
                        <td class=""info-value"">{booking.BookingDate:dd/MM/yyyy}</td>
                    </tr>
                    <tr>
                        <td class=""info-label"">Khung giờ:</td>
                        <td class=""info-value"" style=""color: #d97706;"">{booking.BookingTime:hh\:mm}</td>
                    </tr>
                    <tr>
                        <td class=""info-label"">Số lượng khách:</td>
                        <td class=""info-value"">{booking.NumberOfGuests} Khách</td>
                    </tr>
                    <tr>
                        <td class=""info-label"">Trạng thái phiếu:</td>
                        <td class=""info-value""><span class=""badge"">{booking.Status}</span></td>
                    </tr>
                </table>
            </div>

            <!-- KHỐI QUÉT MÃ VIETQR NHẬN CỌC TỰ ĐỘNG -->
            <div class=""qr-section"">
                <div class=""qr-title"">XÁC NHẬN GIỮ CHỖ QUA VIETQR TỰ ĐỘNG</div>
                <p style=""font-size: 13px; color: #4b5563; margin: 4px 0 12px 0;"">Để đảm bảo khung giờ phục vụ tốt nhất, Quý khách vui lòng thanh toán khoản cọc <strong>100.000đ/Khách</strong>.</p>
                <div style=""font-size: 20px; font-weight: 800; color: #dc2626;"">{depositAmount:N0} VNĐ</div>
                
                <img src=""{qrImageUrl}"" alt=""VietQR Dat Coc"" class=""qr-code"" />
                
                <p style=""font-size: 12px; color: #6b7280; margin-top: 8px;"">
                    <i>* Nội dung chuyển khoản đã được tạo tự động: <strong>HERBAL{booking.Id}</strong>. Hệ thống sẽ tự động xác nhận ngay khi nhận tiền!</i>
                </p>
            </div>

            <p style=""font-size: 13px; color: #6b7280; line-height: 1.5;"">
                📍 <strong>Địa chỉ trung tâm:</strong> Làng Đại học ĐHQG-HCM, TP. Thủ Đức, TP. Hồ Chí Minh<br>
                📞 <strong>Hotline hỗ trợ Lễ tân:</strong> 090x xxx xxx
            </p>
        </div>
        <div class=""footer"">
            <p>&copy; 2026 Herbal Wellness Center. Tất cả quyền được bảo lưu.</p>
            <p>Trải nghiệm Dưỡng sinh Đông Y - Thiền Định & Chuông Xoay Cao Cấp</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(booking.Email, subject, htmlMessage);
        }

        // ==========================================
        // 3. EMAIL THÔNG BÁO DUYỆT CỌC THÀNH CÔNG
        // ==========================================
        public async Task SendDepositReceivedEmailAsync(Booking booking)
        {
            string subject = $"[Herbal Wellness] Xác nhận ĐÃ NHẬN TIỀN CỌC cho đơn #{booking.Id}";

            string htmlMessage = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #fbf9f5; padding: 20px; color: #1f2937; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; padding: 30px; border-top: 6px solid #16a34a; box-shadow: 0 4px 15px rgba(0,0,0,0.05); }}
        .title {{ color: #16a34a; font-size: 22px; font-weight: bold; margin-bottom: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""title"">Duyệt Tiền Cọc Thành Công! 🎉</div>
        <p>Xin chào <strong>{booking.CustomerName}</strong>,</p>
        <p>Herbal Wellness Center đã nhận thành công khoản tiền cọc giữ chỗ cho đơn đặt lịch mã <strong>#HERBAL-{booking.Id}</strong>.</p>
        <div style=""background: #f0fdf4; padding: 15px; border-radius: 8px; border: 1px solid #bbf7d0; margin: 15px 0;"">
            <p style=""margin: 5px 0;"">📅 <strong>Ngày hẹn:</strong> {booking.BookingDate:dd/MM/yyyy}</p>
            <p style=""margin: 5px 0;"">⏰ <strong>Khung giờ:</strong> {booking.BookingTime:hh\:mm}</p>
            <p style=""margin: 5px 0;"">👥 <strong>Số lượng:</strong> {booking.NumberOfGuests} Khách</p>
            <p style=""margin: 5px 0;"">✅ <strong>Trạng thái:</strong> ĐÃ ĐẶT CỌC GIỮ CHỖ</p>
        </div>
        <p>Đội ngũ Lễ tân và Kỹ thuật viên đã sẵn sàng không gian riêng tư để đón tiếp Quý khách. Vui lòng đến trước 10 phút để thưởng thức trà thảo mộc đón tiếp!</p>
        <br>
        <p>Trân trọng,<br><strong>Đội ngũ Herbal Wellness Center</strong></p>
    </div>
</body>
</html>";

            await SendEmailAsync(booking.Email, subject, htmlMessage);
        }

        // ==========================================
        // 4. EMAIL NHẮC LỊCH HẸN TỰ ĐỘNG
        // ==========================================
        public async Task SendBookingReminderEmailAsync(Booking booking)
        {
            string subject = $"[Nhắc Lịch Hẹn] Lịch trải nghiệm dưỡng sinh của Quý khách vào hôm nay #{booking.Id}";

            string htmlMessage = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
</head>
<body style=""font-family: Arial, sans-serif; background-color: #fbf9f5; padding: 20px;"">
    <div style=""max-width: 600px; margin: 0 auto; background: #fff; padding: 25px; border-radius: 12px; border-left: 6px solid #c5a059;"">
        <h2 style=""color: #234d20; margin-top: 0;"">Lịch Hẹn Trải Nghiệm Sắp Đến! 🌿</h2>
        <p>Thân gửi <strong>{booking.CustomerName}</strong>,</p>
        <p>Herbal Wellness Center xin nhắc lịch hẹn dưỡng sinh của Quý khách vào lúc <strong style=""color: #d97706; font-size: 16px;"">{booking.BookingTime:hh\:mm} hôm nay ({booking.BookingDate:dd/MM/yyyy})</strong>.</p>
        <p>📍 <strong>Địa chỉ:</strong> Làng Đại học ĐHQG-HCM, TP. Thủ Đức.</p>
        <p>Chúc Quý khách có một buổi thư giãn và phục hồi năng lượng trọn vẹn nhất!</p>
    </div>
</body>
</html>";

            await SendEmailAsync(booking.Email, subject, htmlMessage);
        }
    }
}