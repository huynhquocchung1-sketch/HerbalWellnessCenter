using System.Text;
using HerbalWellnessCenter.Data;
using HerbalWellnessCenter.Models;
using HerbalWellnessCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HerbalWellnessCenter.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdminDashboardService _adminDashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDbContext context,
            IAdminDashboardService adminDashboardService,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _adminDashboardService = adminDashboardService;
            _logger = logger;
        }

        // =========================================================================
        // 1. TRANG ĐIỀU PHỐI LỄ TÂN & HỆ THỐNG KPI / ANALYTICS (TỐI ƯU HIỆU SUẤT TRUY VẤN)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1)
        {
            int pageSize = 10;
            page = page < 1 ? 1 : page;

            // Xây dựng câu truy vấn linh hoạt (AsNoTracking giúp tăng tốc độ đọc)
            var query = _context.Bookings
                .Include(b => b.Service)
                .ThenInclude(s => s!.Category)
                .Include(b => b.Therapist)
                .Include(b => b.Bed)
                .AsNoTracking()
                .AsQueryable();

            // Tìm kiếm nâng cao (Họ tên khách, SĐT, Mã đơn)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                query = query.Where(b => b.CustomerName.Contains(keyword)
                                      || b.PhoneNumber.Contains(keyword)
                                      || b.Id.ToString().Contains(keyword));
            }

            // Lọc theo trạng thái đơn hàng
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status);
            }

            // Lọc theo khoảng thời gian trải nghiệm
            if (fromDate.HasValue)
            {
                query = query.Where(b => b.BookingDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(b => b.BookingDate.Date <= toDate.Value.Date);
            }

            // =========================================================================
            // TÍNH TOÁN KPI TRỰC TIẾP TẠI DATABASE (SỬ DỤNG HẰNG SỐ AN TOÀN)
            // =========================================================================
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.PendingCount = await _context.Bookings.CountAsync(b => b.Status == BookingStatusConstants.Pending);
            ViewBag.DepositCount = await _context.Bookings.CountAsync(b => b.Status == BookingStatusConstants.Deposited);
            ViewBag.CompletedCount = await _context.Bookings.CountAsync(b => b.Status == BookingStatusConstants.Completed);
            ViewBag.CancelledCount = await _context.Bookings.CountAsync(b => b.Status == BookingStatusConstants.Cancelled);

            // Tổng số lượt khách đã và đang phục vụ (trừ đơn đã hủy)
            ViewBag.TotalGuests = await _context.Bookings
                .Where(b => b.Status != BookingStatusConstants.Cancelled)
                .SumAsync(b => (int?)b.NumberOfGuests) ?? 0;

            // Tổng doanh thu thực tế tính trực tiếp trên database từ các đơn Đã cọc hoặc Hoàn thành
            decimal totalRevenue = await _context.Bookings
                .Include(b => b.Service)
                .Where(b => b.Status == BookingStatusConstants.Deposited || b.Status == BookingStatusConstants.Completed)
                .SumAsync(b => (b.Service != null ? b.Service.Price : 350000m) * b.NumberOfGuests);

            ViewBag.TotalRevenue = totalRevenue;

            // Thống kê tỷ lệ lấp đầy cơ sở vật chất (Giường / Bồn)
            ViewBag.OccupiedBedsCount = await _context.Beds.CountAsync(b => b.IsOccupied);
            ViewBag.TotalBedsCount = await _context.Beds.CountAsync();

            // Nạp danh sách tài nguyên phục vụ modal phân công trực tiếp
            ViewBag.Therapists = await _context.Therapists.Where(t => t.IsAvailable).AsNoTracking().ToListAsync();
            ViewBag.Beds = await _context.Beds.AsNoTracking().ToListAsync();

            // Phân trang kết quả thực hiện tại Database
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = await query
                .OrderByDescending(b => b.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lưu trạng thái bộ lọc ra View
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(items);
        }

        // =========================================================================
        // 2. CẬP NHẬT TRẠNG THÁI (THÔNG QUA SERVICE LAYER)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            try
            {
                await _adminDashboardService.UpdateStatusAsync(id, newStatus);
                TempData["SuccessMessage"] = $"Cập nhật thành công phiếu #{id} thành '{newStatus}'.";
            }
            catch (KeyNotFoundException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật trạng thái đơn #{BookingId}", id);
                TempData["ErrorMessage"] = "Lỗi hệ thống khi cập nhật trạng thái đơn hàng.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 3. QUẢN LÝ NGUỒN LỰC TỰ ĐỘNG (RESOURCE ALLOCATION THÔNG QUA SERVICE)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignResource(int bookingId, int therapistId, int bedId, string? status, byte[]? rowVersion)
        {
            try
            {
                await _adminDashboardService.AssignResourceAsync(bookingId, therapistId, bedId, status, rowVersion);
                TempData["SuccessMessage"] = $"Đã điều phối KTV và Giường thành công cho đơn #HERBAL-{bookingId}!";
            }
            catch (InvalidOperationException ex)
            {
                // Bắt lỗi xung đột đồng thời (Concurrency)
                TempData["ErrorMessage"] = $"⚠️ {ex.Message}";
            }
            catch (KeyNotFoundException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi phân công tài nguyên cho đơn #{BookingId}", bookingId);
                TempData["ErrorMessage"] = "Lỗi hệ thống khi cập nhật phân công tài nguyên.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 4. QUẢN LÝ KỸ THUẬT VIÊN (THERAPISTS MANAGEMENT)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Therapists()
        {
            var list = await _context.Therapists.OrderBy(t => t.Id).AsNoTracking().ToListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult CreateTherapist()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTherapist(string name, string phoneNumber, string? email, string specialty, int yearsOfExperience, decimal rating = 5.0m)
        {
            try
            {
                await _adminDashboardService.AddTherapistAsync(name, phoneNumber, email, specialty, yearsOfExperience, rating);
                TempData["SuccessMessage"] = $"Đã thêm Kỹ thuật viên {name} thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Therapists));
        }

        // =========================================================================
        // 5. QUẢN LÝ GIƯỜNG / PHÒNG TRỊ LIỆU (BEDS MANAGEMENT)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Beds()
        {
            var list = await _context.Beds.OrderBy(b => b.Id).AsNoTracking().ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBed(string bedNumber, string roomName, string floorLevel)
        {
            try
            {
                await _adminDashboardService.AddBedAsync(bedNumber, roomName, floorLevel);
                TempData["SuccessMessage"] = $"Đã thêm giường/phòng {bedNumber} thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Beds));
        }

        // =========================================================================
        // 6. XÓA PHIẾU HẸN & GIẢI PHÓNG TÀI NGUYÊN (QUYỀN ADMIN)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            try
            {
                await _adminDashboardService.DeleteBookingAsync(id);
                TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn phiếu hẹn #{id} khỏi hệ thống.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 7. XUẤT BÁO CÁO EXCEL / CSV CHUẨN UTF-8 HỖ TRỢ TIẾNG VIỆT
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Bookings
                .Include(b => b.Service)
                .Include(b => b.Therapist)
                .Include(b => b.Bed)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(b => b.BookingDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(b => b.BookingDate.Date <= toDate.Value.Date);
            }

            var list = await query.OrderByDescending(b => b.Id).ToListAsync();

            var builder = new StringBuilder();
            builder.AppendLine("MaDon,HoTenKhachHang,SoDienThoai,Email,NgayHen,GioHen,SoKhach,KyThuatVien,GiuongPhong,DoanhThuDuKien,TrangThai,GhiChu");

            foreach (var b in list)
            {
                decimal revenue = (b.Service?.Price ?? 350000) * b.NumberOfGuests;
                string cleanNote = b.Note?.Replace(",", " - ").Replace("\r\n", " ") ?? "";
                string therapistName = b.Therapist?.Name ?? "Chưa phân công";
                string bedName = b.Bed != null ? $"{b.Bed.BedNumber} ({b.Bed.RoomName})" : "Chưa xếp giường";

                builder.AppendLine($"#HERBAL-{b.Id},{b.CustomerName},{b.PhoneNumber},{b.Email},{b.BookingDate:dd/MM/yyyy},{b.BookingTime:hh\\:mm},{b.NumberOfGuests},\"{therapistName}\",\"{bedName}\",{revenue},{b.Status},\"{cleanNote}\"");
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var data = Encoding.UTF8.GetBytes(builder.ToString());
            var resultData = preamble.Concat(data).ToArray();

            string fileName = $"BaoCao_LichHen_HerbalWellness_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(resultData, "text/csv; charset=utf-8", fileName);
        }
    }
}