using System.Diagnostics;
using HerbalWellnessCenter.Data;
using HerbalWellnessCenter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HerbalWellnessCenter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        // Giới hạn sức chứa tối đa của trung tâm (15 - 25 khách/lượt)
        private const int MAX_CAPACITY_PER_SLOT = 20;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // 1. TRANG CHỦ: Hiển thị 4 phân khu & Danh sách dịch vụ nổi bật
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.Include(c => c.Services).ToListAsync();
            var featuredServices = await _context.Services.Take(8).ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.FeaturedServices = featuredServices;

            return View();
        }

        // 2. XỬ LÝ ĐẶT LỊCH HẸN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(
            string CustomerName,
            string PhoneNumber,
            string? Email,
            string? ServiceCategory,
            int? ServiceId,
            DateTime BookingDate,
            TimeSpan BookingTime,
            int NumberOfGuests,
            string? Note)
        {
            // Validation 1: Kiểm tra thông tin bắt buộc
            if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(PhoneNumber))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Họ tên và Số điện thoại liên hệ!";
                return RedirectToAction("Index");
            }

            if (BookingDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Ngày đặt lịch không thể nhỏ hơn ngày hiện tại!";
                return RedirectToAction("Index");
            }

            if (NumberOfGuests <= 0 || NumberOfGuests > 10)
            {
                TempData["ErrorMessage"] = "Số lượng khách đặt cho một lượt từ 1 đến 10 người!";
                return RedirectToAction("Index");
            }

            // Validation 2: Kiểm tra sức chứa hiện tại của trung tâm (Sử dụng hằng số chuẩn)
            var currentBookingsCount = await _context.Bookings
                .Where(b => b.BookingDate.Date == BookingDate.Date
                         && b.BookingTime == BookingTime
                         && b.Status != BookingStatusConstants.Cancelled)
                .SumAsync(b => b.NumberOfGuests);

            if (currentBookingsCount + NumberOfGuests > MAX_CAPACITY_PER_SLOT)
            {
                TempData["ErrorMessage"] = $"Khung giờ {BookingTime:hh\\:mm} ngày {BookingDate:dd/MM/yyyy} đã đạt giới hạn sức chứa ({currentBookingsCount}/{MAX_CAPACITY_PER_SLOT} khách). Vui lòng chọn khung giờ khác!";
                return RedirectToAction("Index");
            }

            // Xác định Service ID tương ứng
            int selectedServiceId = 1;
            if (ServiceId.HasValue && ServiceId.Value > 0)
            {
                selectedServiceId = ServiceId.Value;
            }
            else if (!string.IsNullOrEmpty(ServiceCategory))
            {
                var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name.Contains(ServiceCategory));
                if (category != null)
                {
                    var defaultService = await _context.Services.FirstOrDefaultAsync(s => s.CategoryId == category.Id);
                    if (defaultService != null)
                    {
                        selectedServiceId = defaultService.Id;
                    }
                }
            }

            // Tính tiền đặt cọc (100.000 VNĐ / khách)
            decimal calculatedDeposit = NumberOfGuests * 100000m;

            // Khởi tạo đối tượng Đặt lịch mới chuẩn Clean Code
            var booking = new Booking
            {
                CustomerName = CustomerName.Trim(),
                PhoneNumber = PhoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? "guest@herbalwellness.com" : Email.Trim(),
                BookingDate = BookingDate,
                BookingTime = BookingTime == TimeSpan.Zero ? new TimeSpan(9, 0, 0) : BookingTime,
                NumberOfGuests = NumberOfGuests,
                Note = !string.IsNullOrEmpty(ServiceCategory) ? $"[Không gian: {ServiceCategory}] {Note}" : (Note ?? ""),
                Status = BookingStatusConstants.Pending,
                PaymentStatus = PaymentStatusConstants.Unpaid,
                DepositAmount = calculatedDeposit,
                ServiceId = selectedServiceId
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Chuyển sang trang Xác nhận đơn hàng & Mã VietQR
            return RedirectToAction("BookingConfirmation", new { id = booking.Id });
        }

        // 3. TRANG XÁC NHẬN ĐẶT LỊCH & MÃ VIETQR CHUYỂN KHOẢN CHÍNH THỨC
        public async Task<IActionResult> BookingConfirmation(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Service)
                .ThenInclude(s => s!.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Lấy số tiền cọc đã lưu hoặc tính toán dự phòng
            decimal depositAmount = booking.DepositAmount > 0 ? booking.DepositAmount : (booking.NumberOfGuests * 100000m);
            ViewBag.DepositAmount = depositAmount;

            // =========================================================================
            // THÔNG TIN TÀI KHOẢN NGÂN HÀNG CHÍNH THỨC
            // =========================================================================
            string bankId = "TCB"; // Techcombank
            string bankName = "Techcombank";
            string accountNo = "74868699999";
            string accountName = "HUYNH QUOC CHUNG";
            string addInfo = $"COC LICH HEN {booking.Id}";

            ViewBag.BankName = bankName;
            ViewBag.AccountNo = accountNo;
            ViewBag.AccountName = accountName;

            // Tạo link VietQR QuickLink (Tự động điền Số tiền + Nội dung + Chủ tài khoản)
            string qrPayload = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={depositAmount}&addInfo={Uri.EscapeDataString(addInfo)}&accountName={Uri.EscapeDataString(accountName)}";
            ViewBag.QrCodeUrl = qrPayload;

            return View(booking);
        }

        // 4. DANH SÁCH DỊCH VỤ THEO KHÔNG GIAN
        public async Task<IActionResult> Services(int? categoryId)
        {
            var query = _context.Services.Include(s => s.Category).AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(s => s.CategoryId == categoryId.Value);
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;

            return View(await query.ToListAsync());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}