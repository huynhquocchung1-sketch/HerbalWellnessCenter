using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HerbalWellnessCenter.Data;
using HerbalWellnessCenter.Models;
using HerbalWellnessCenter.Services;

namespace HerbalWellnessCenter.Controllers
{
    [Authorize(Roles = "Customer,Admin,Staff")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            ILogger<CustomerController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        // ==========================================
        // 1. TRANG QUẢN LÝ LỊCH HẸN CÁ NHÂN (MY BOOKINGS)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MyBookings(string statusFilter = "All")
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Truy cập MyBookings thất bại do người dùng chưa xác thực email.");
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var query = _context.Bookings
                .Where(b => b.Email == userEmail)
                .AsQueryable();

            // Bộ lọc nâng cao theo trạng thái đơn
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                query = query.Where(b => b.Status == statusFilter);
            }

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.Id)
                .ToListAsync();

            // Truyền trạng thái lọc sang View để đồng bộ giao diện
            ViewData["CurrentFilter"] = statusFilter;
            return View(bookings);
        }

        // ==========================================
        // 2. CHI TIẾT ĐƠN ĐẶT LỊCH
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userEmail = User.Identity?.Name;
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin phiếu đặt lịch!";
                return RedirectToAction(nameof(MyBookings));
            }

            // Kiểm tra bảo mật: Chỉ cho phép chủ nhân đơn hoặc Admin/Staff xem chi tiết
            if (booking.Email != userEmail && !User.IsInRole("Admin") && !User.IsInRole("Staff"))
            {
                _logger.LogWarning("Phát hiện truy cập trái phép vào đơn đặt lịch #{BookingId} bởi {User}", id, userEmail);
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào phiếu đặt lịch này.";
                return RedirectToAction(nameof(MyBookings));
            }

            return View(booking);
        }

        // ==========================================
        // 3. KHÁCH HÀNG TỰ HỦY LỊCH HẸN (CHỈ KHI CHỜ XÁC NHẬN)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id, string cancelReason)
        {
            var userEmail = User.Identity?.Name;
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiếu đặt lịch cần hủy.";
                return RedirectToAction(nameof(MyBookings));
            }

            // Kiểm tra quyền sở hữu
            if (booking.Email != userEmail && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thao tác trên đơn hàng này.";
                return RedirectToAction(nameof(MyBookings));
            }

            // Chỉ cho phép hủy khi đơn ở trạng thái "Chờ xác nhận"
            if (booking.Status != "Chờ xác nhận")
            {
                TempData["ErrorMessage"] = "Đơn đặt lịch đã được duyệt hoặc hoàn thành, không thể tự hủy trực tuyến. Vui lòng liên hệ Hotline.";
                return RedirectToAction(nameof(MyBookings));
            }

            try
            {
                booking.Status = "Đã hủy";
                if (!string.IsNullOrEmpty(cancelReason))
                {
                    booking.Note = $"{booking.Note} | [Khách hủy: {cancelReason}]";
                }

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                // Gửi email thông báo hủy lịch thành công cho khách hàng
                string subject = $"[Herbal Wellness] Xác nhận hủy phiếu đặt lịch #{booking.Id}";
                string htmlMessage = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                        <h2 style='color: #dc2626;'>Thông Báo Hủy Lịch Hẹn</h2>
                        <p>Xin chào <strong>{booking.CustomerName}</strong>,</p>
                        <p>Phiếu đặt lịch mã số <strong>#HERBAL-{booking.Id}</strong> của Quý khách đã được hủy thành công theo yêu cầu.</p>
                        <p>Lý do hủy: <em>{(string.IsNullOrEmpty(cancelReason) ? "Không có" : cancelReason)}</em></p>
                        <p>Hẹn gặp lại Quý khách trong các dịp trải nghiệm sắp tới tại Herbal Wellness Center!</p>
                    </div>";

                await _emailSender.SendEmailAsync(booking.Email, subject, htmlMessage);

                TempData["SuccessMessage"] = $"Đã hủy thành công phiếu đặt lịch #HERBAL-{booking.Id}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi khách hàng hủy đơn #{BookingId}", id);
                TempData["ErrorMessage"] = "Có lỗi hệ thống xảy ra khi xử lý yêu cầu hủy lịch.";
            }

            return RedirectToAction(nameof(MyBookings));
        }

        // ==========================================
        // 4. TRANG HỒ SƠ CÁ NHÂN (USER PROFILE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            return View(user);
        }
    }
}