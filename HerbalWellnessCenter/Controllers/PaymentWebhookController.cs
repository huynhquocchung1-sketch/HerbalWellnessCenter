using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HerbalWellnessCenter.Data;
using HerbalWellnessCenter.Models;

namespace HerbalWellnessCenter.Controllers.Api
{
    [Route("api/webhook")]
    [ApiController]
    public class PaymentWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentWebhookController> _logger;

        // Token bảo mật nội bộ (Có thể cấu hình trong appsettings.json nếu cần thiết)
        private const string SecretWebhookToken = "HerbalWellness_SecuredKey_2026";

        public PaymentWebhookController(ApplicationDbContext context, ILogger<PaymentWebhookController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// API Endpoint nhận Webhook biến động số dư từ cổng thanh toán VietQR / SePay / Casso
        /// </summary>
        [HttpPost("vietqr")]
        public async Task<IActionResult> ReceiveVietQRWebhook([FromBody] SePayWebhookPayload? payload)
        {
            try
            {
                // 1. Kiểm tra tính hợp lệ của dữ liệu đầu vào
                if (payload == null)
                {
                    _logger.LogWarning("Webhook nhận được dữ liệu trống (null payload).");
                    return BadRequest(new { success = false, message = "Payload không hợp lệ." });
                }

                // 2. Kiểm tra chiều giao dịch (Chỉ xử lý tiền vào: số tiền > 0 và không phải giao dịch chuyển đi "out")
                if (payload.transferAmount <= 0 || (payload.transferType != null && payload.transferType.Equals("out", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Bỏ qua giao dịch chi tiền hoặc số tiền <= 0. ID giao dịch: {Id}", payload.id);
                    return Ok(new { success = true, message = "Đã bỏ qua giao dịch chi tiền." });
                }

                _logger.LogInformation("Nhận biến động số dư: +{Amount} VNĐ | Nội dung: '{Content}' | Mã GD: {Ref}",
                    payload.transferAmount, payload.content, payload.referenceNumber);

                // 3. Chống trùng lặp giao dịch (Idempotency Check)
                string transactionRef = !string.IsNullOrEmpty(payload.referenceNumber) ? payload.referenceNumber : payload.id.ToString();
                var existingBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.TransactionCode == transactionRef);

                if (existingBooking != null)
                {
                    _logger.LogWarning("Giao dịch {Ref} đã được xử lý trước đó cho đơn hàng #{BookingId}.", transactionRef, existingBooking.Id);
                    return Ok(new { success = true, message = "Giao dịch đã được xử lý trước đó." });
                }

                // 4. Bóc tách Mã đơn hàng (Booking ID) từ nội dung chuyển khoản
                int bookingId = ExtractBookingId(payload.content);

                if (bookingId <= 0)
                {
                    _logger.LogWarning("Không thể tìm thấy mã đơn hàng hợp lệ trong nội dung chuyển khoản: '{Content}'", payload.content);
                    return Ok(new { success = false, message = "Không tìm thấy mã đơn hàng phù hợp trong nội dung." });
                }

                // 5. Truy vấn đơn hàng trong Cơ sở dữ liệu
                var booking = await _context.Bookings
                    .Include(b => b.Service)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    _logger.LogWarning("Không tồn tại đơn hàng có ID = #{BookingId} trong hệ thống.", bookingId);
                    return Ok(new { success = false, message = $"Không tìm thấy đơn hàng #{bookingId}." });
                }

                // Kiểm tra nếu đơn đã cọc hoặc thanh toán rồi
                if (booking.PaymentStatus == "Đã cọc" || booking.PaymentStatus == "Đã thanh toán")
                {
                    _logger.LogInformation("Đơn hàng #{BookingId} đã ở trạng thái thanh toán: {Status}", bookingId, booking.PaymentStatus);
                    return Ok(new { success = true, message = "Đơn hàng đã được thanh toán từ trước." });
                }

                // 6. Cập nhật trạng thái đơn hàng sang "Đã cọc" và xác nhận đơn
                booking.PaymentStatus = "Đã cọc";
                booking.DepositAmount = payload.transferAmount;
                booking.TransactionCode = transactionRef;
                booking.Status = "Đã xác nhận";
                booking.UpdatedAt = DateTime.Now;

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Thành công! Đơn hàng #{BookingId} đã được duyệt cọc tự động qua VietQR. Số tiền: {Amount} VNĐ",
                    bookingId, payload.transferAmount);

                return Ok(new
                {
                    success = true,
                    message = $"Đã xác nhận thanh toán thành công cho đơn hàng #{bookingId}",
                    bookingId = booking.Id,
                    amount = payload.transferAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng xảy ra khi xử lý Webhook thanh toán.");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống nội bộ.", error = ex.Message });
            }
        }

        /// <summary>
        /// Hàm hỗ trợ bóc tách số ID đơn hàng từ chuỗi nội dung chuyển khoản an toàn với Nullable
        /// </summary>
        private int ExtractBookingId(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return 0;

            string upperContent = content.ToUpper();

            // Dùng Regex tìm chuỗi số nằm sau các từ khóa như DATLICH, HERBAL, DH, ID
            Match match = Regex.Match(upperContent, @"(?:DATLICH|HERBAL|DH|ID)?\s*#?(\d+)");

            if (match.Success && match.Groups[1].Success)
            {
                if (int.TryParse(match.Groups[1].Value, out int id))
                {
                    return id;
                }
            }

            // Fallback: Lấy toàn bộ số đầu tiên xuất hiện trong chuỗi nội dung
            var fallbackMatch = Regex.Match(content, @"\d+");
            if (fallbackMatch.Success && int.TryParse(fallbackMatch.Value, out int fallbackId))
            {
                return fallbackId;
            }

            return 0;
        }
    }

    /// <summary>
    /// Model định dạng dữ liệu nhận từ Webhook SePay / Casso / Ngân hàng nội địa
    /// </summary>
    public class SePayWebhookPayload
    {
        public long id { get; set; }
        public string? gateway { get; set; }
        public string? transactionDate { get; set; }
        public string? accountNumber { get; set; }
        public string? subAccount { get; set; }
        public decimal transferAmount { get; set; }
        public string? transferType { get; set; } // "in" hoặc "out"
        public string? content { get; set; }
        public string? referenceNumber { get; set; } // Mã tham chiếu giao dịch ngân hàng
        public decimal accumulated { get; set; }
    }
}