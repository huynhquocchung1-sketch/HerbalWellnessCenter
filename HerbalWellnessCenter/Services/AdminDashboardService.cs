using HerbalWellnessCenter.Data;
using HerbalWellnessCenter.Models;
using HerbalWellnessCenter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HerbalWellnessCenter.Services
{
    public interface IAdminDashboardService
    {
        Task UpdateStatusAsync(int bookingId, string newStatus);
        Task AssignResourceAsync(int bookingId, int therapistId, int bedId, string? status, byte[]? rowVersion);
        Task DeleteBookingAsync(int bookingId);
        Task AddTherapistAsync(string name, string phoneNumber, string? email, string specialty, int yearsOfExperience, decimal rating);
        Task AddBedAsync(string bedNumber, string roomName, string floorLevel);
    }

    /// <summary>
    /// Dịch vụ quản trị điều phối vận hành (Admin Dashboard Service)
    /// Đạt chuẩn Enterprise-grade, xử lý đồng thời, transaction và audit logging.
    /// </summary>
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AdminDashboardService> _logger;

        public AdminDashboardService(
            ApplicationDbContext context,
            IEmailSender emailSender,
            ILogger<AdminDashboardService> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task UpdateStatusAsync(int bookingId, string newStatus)
        {
            var booking = await _context.Bookings
                .Include(b => b.Bed)
                .Include(b => b.Therapist)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                _logger.LogWarning("[ADMIN SERVICE] Không tìm thấy phiếu hẹn #{BookingId} khi cập nhật trạng thái.", bookingId);
                throw new KeyNotFoundException("Không tìm thấy thông tin phiếu hẹn trong hệ thống!");
            }

            string oldStatus = booking.Status;
            booking.Status = newStatus;
            booking.UpdatedAt = DateTime.Now;

            // Tự động giải phóng tài nguyên (Giường & KTV) khi đơn hàng Hoàn thành hoặc Hủy
            if (newStatus == BookingStatusConstants.Completed || newStatus == BookingStatusConstants.Cancelled)
            {
                if (booking.Bed != null)
                {
                    booking.Bed.IsOccupied = false;
                    _context.Beds.Update(booking.Bed);
                    _logger.LogInformation("[RESOURCE] Đã giải phóng giường #{BedId} do đơn #{BookingId} chuyển sang trạng thái '{Status}'", booking.BedId, bookingId, newStatus);
                }

                if (booking.Therapist != null)
                {
                    booking.Therapist.IsAvailable = true;
                    _context.Therapists.Update(booking.Therapist);
                    _logger.LogInformation("[RESOURCE] Đã giải phóng kỹ thuật viên #{TherapistId} do đơn #{BookingId} chuyển sang trạng thái '{Status}'", booking.TherapistId, bookingId, newStatus);
                }
            }

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            // Gửi email xác nhận tự động khi duyệt cọc thành công
            if (oldStatus == BookingStatusConstants.Pending && newStatus == BookingStatusConstants.Deposited)
            {
                try
                {
                    await _emailSender.SendDepositReceivedEmailAsync(booking);
                    _logger.LogInformation("[EMAIL] Đã gửi thông báo xác nhận cọc thành công cho đơn #{BookingId}", bookingId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EMAIL ERROR] Lỗi gửi email xác nhận cọc cho đơn #{BookingId}", bookingId);
                }
            }

            _logger.LogInformation("[AUDIT] Đã cập nhật trạng thái đơn #{BookingId} từ '{OldStatus}' sang '{NewStatus}'", bookingId, oldStatus, newStatus);
        }

        public async Task AssignResourceAsync(int bookingId, int therapistId, int bedId, string? status, byte[]? rowVersion)
        {
            var booking = await _context.Bookings
                .Include(b => b.Therapist)
                .Include(b => b.Bed)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                throw new KeyNotFoundException("Không tìm thấy phiếu đặt lịch cần phân công.");
            }

            // Gán giá trị RowVersion để kích hoạt cơ chế Optimistic Concurrency của EF Core
            if (rowVersion != null)
            {
                _context.Entry(booking).Property("RowVersion").OriginalValue = rowVersion;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Giải phóng giường cũ nếu khách/lễ tân đổi sang giường mới
                if (booking.BedId.HasValue && booking.BedId != bedId)
                {
                    var oldBed = await _context.Beds.FindAsync(booking.BedId);
                    if (oldBed != null)
                    {
                        oldBed.IsOccupied = false;
                        _context.Beds.Update(oldBed);
                    }
                }

                // Gán tài nguyên mới vào đơn hàng
                booking.TherapistId = therapistId > 0 ? therapistId : null;
                booking.BedId = bedId > 0 ? bedId : null;
                booking.UpdatedAt = DateTime.Now;

                // Cập nhật trạng thái bận cho giường mới chọn
                if (bedId > 0)
                {
                    var newBed = await _context.Beds.FindAsync(bedId);
                    if (newBed != null)
                    {
                        newBed.IsOccupied = true;
                        _context.Beds.Update(newBed);
                    }
                }

                string oldStatus = booking.Status;
                if (!string.IsNullOrEmpty(status))
                {
                    booking.Status = status;
                }

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Gửi email thông báo nếu chuyển trạng thái sang đã cọc trong quá trình điều phối
                if (oldStatus == BookingStatusConstants.Pending && booking.Status == BookingStatusConstants.Deposited)
                {
                    try
                    {
                        await _emailSender.SendDepositReceivedEmailAsync(booking);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[EMAIL ERROR] Lỗi gửi email xác nhận đặt cọc qua phân công cho đơn #{BookingId}", bookingId);
                    }
                }

                _logger.LogInformation("[DISPATCH] Điều phối thành công đơn #{BookingId} - KTV ID: {TherapistId}, Giường ID: {BedId}", bookingId, therapistId, bedId);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning("[CONCURRENCY CONFLICT] Xung đột dữ liệu khi phân công đơn #{BookingId}", bookingId);
                throw new InvalidOperationException("Đơn đặt lịch này đã được một nhân viên khác cập nhật trước đó. Vui lòng tải lại trang!");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "[ERROR] Lỗi hệ thống khi phân công tài nguyên cho đơn #{BookingId}", bookingId);
                throw;
            }
        }

        public async Task DeleteBookingAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Bed)
                .Include(b => b.Therapist)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                throw new KeyNotFoundException("Không tìm thấy phiếu hẹn cần xóa!");
            }

            // Giải phóng tài nguyên trước khi xóa vĩnh viễn đơn hàng
            if (booking.Bed != null)
            {
                booking.Bed.IsOccupied = false;
                _context.Beds.Update(booking.Bed);
            }
            if (booking.Therapist != null)
            {
                booking.Therapist.IsAvailable = true;
                _context.Therapists.Update(booking.Therapist);
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            _logger.LogWarning("[SECURITY AUDIT] Đã xóa vĩnh viễn phiếu hẹn #{BookingId} khỏi hệ thống cơ sở dữ liệu.", bookingId);
        }

        public async Task AddTherapistAsync(string name, string phoneNumber, string? email, string specialty, int yearsOfExperience, decimal rating)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Họ tên và số điện thoại kỹ thuật viên không được để trống.");
            }

            var therapist = new Therapist
            {
                Name = name.Trim(),
                PhoneNumber = phoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? "chua-cap-nhat@herbalwellness.vn" : email.Trim(),
                Specialty = string.IsNullOrWhiteSpace(specialty) ? "Massage Đông Y" : specialty.Trim(),
                YearsOfExperience = yearsOfExperience,
                Rating = rating > 0 ? rating : 5.0m,
                IsAvailable = true
            };

            _context.Therapists.Add(therapist);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[MANAGEMENT] Đã thêm mới kỹ thuật viên: {TherapistName}", therapist.Name);
        }

        public async Task AddBedAsync(string bedNumber, string roomName, string floorLevel)
        {
            if (string.IsNullOrWhiteSpace(bedNumber))
            {
                throw new ArgumentException("Số hiệu giường/phòng không được để trống.");
            }

            var bed = new Bed
            {
                BedNumber = bedNumber.Trim(),
                RoomName = string.IsNullOrWhiteSpace(roomName) ? "Phòng Trị Liệu Chung" : roomName.Trim(),
                FloorLevel = string.IsNullOrWhiteSpace(floorLevel) ? "Tầng 1" : floorLevel.Trim(),
                IsOccupied = false
            };

            _context.Beds.Add(bed);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[MANAGEMENT] Đã thêm mới giường/phòng: {BedNumber} - {RoomName}", bed.BedNumber, bed.RoomName);
        }
    }
}