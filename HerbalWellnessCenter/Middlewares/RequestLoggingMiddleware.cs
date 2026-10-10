using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HerbalWellnessCenter.Middlewares
{
    /// <summary>
    /// Middleware ghi nhận log tự động cho mọi HTTP Request, đo lường thời gian phản hồi (Performance)
    /// và kiểm soát các mã lỗi bảo mật (Audit Logging).
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var path = context.Request.Path;
            var method = context.Request.Method;
            var user = context.User.Identity?.Name ?? "Anonymous";

            try
            {
                // Cho phép request tiếp tục chạy qua Pipeline
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                long elapsedMs = stopwatch.ElapsedMilliseconds;

                // Phân loại mức độ cảnh báo dựa trên mã phản hồi HTTP
                if (statusCode >= 500)
                {
                    _logger.LogError("[CRITICAL ERROR] [{Method}] {Path} executed by '{User}' responded {StatusCode} in {Elapsed}ms",
                        method, path, user, statusCode, elapsedMs);
                }
                else if (statusCode >= 400)
                {
                    _logger.LogWarning("[CLIENT WARNING] [{Method}] {Path} executed by '{User}' responded {StatusCode} in {Elapsed}ms",
                        method, path, user, statusCode, elapsedMs);
                }
                else
                {
                    // Chỉ log thông tin ở mức Information với các request thông thường để tránh làm đầy rác file log
                    _logger.LogInformation("[ACCESS LOG] [{Method}] {Path} | User: {User} | Status: {StatusCode} | Time: {Elapsed}ms",
                        method, path, user, statusCode, elapsedMs);
                }
            }
        }
    }

}