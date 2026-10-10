using HerbalWellnessCenter.Data;
using HerbalWellnessCenter.Models;
using HerbalWellnessCenter.Services;
using HerbalWellnessCenter.Middlewares; // Thêm namespace chứa RequestLoggingMiddleware
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. CẤU HÌNH CONNECTION STRING & DBCONTEXT TỰ ĐỘNG THỬ LẠI (ENABLE RETRY ON FAILURE)
// =========================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    }));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// =========================================================================
// 2. CẤU HÌNH IDENTITY & PHÂN QUYỀN ROLES (ADMIN, STAFF, CUSTOMER)
// =========================================================================
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// =========================================================================
// 3. CẤU HÌNH SESSION & CACHE
// =========================================================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// =========================================================================
// 4. ĐĂNG KÝ DỊCH VỤ SERVICE LAYER & DEPENDENCY INJECTION (ENTERPRISE PATTERN)
// =========================================================================
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();

// =========================================================================
// 5. CẤU HÌNH MVC CONTROLLERS & RAZOR PAGES
// =========================================================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// =========================================================================
// 6. CẤU HÌNH HTTP REQUEST PIPELINE (SECURITY & GLOBAL ERROR HANDLING)
// =========================================================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // Tự động bắt mã lỗi HTTP (404, 403, v.v.) và điều hướng về trang xử lý lỗi tùy chỉnh
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// =========================================================================
// KÍCH HOẠT CUSTOM MIDDLEWARE GIÁM SÁT HIỆU NĂNG & AUDIT LOG
// =========================================================================
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// =========================================================================
// 7. ĐỊNH TUYẾN ROUTES (AREAS ADMIN & DEFAULT)
// =========================================================================
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// =========================================================================
// 8. TỰ ĐỘNG KHỞI TẠO DATA CƠ BẢN (ROLES, ADMIN, CATEGORIES, SERVICES)
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Tự động Cập nhật Migration nếu DB chưa được khởi tạo
        context.Database.Migrate();

        // Tạo sẵn các Quyền (Roles)
        string[] roleNames = { "Admin", "Staff", "Customer" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Tạo tài khoản Admin mặc định
        var adminEmail = "admin@herbalwellness.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var createAdmin = await userManager.CreateAsync(adminUser, "Admin@123");
            if (createAdmin.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Khởi tạo 4 Phân khu không gian & Dịch vụ mẫu nếu CSDL trống
        if (!context.Categories.Any())
        {
            var catDongY = new Category
            {
                Name = "Đông y – Thảo dược",
                Description = "Bắt mạch tư vấn, ngâm tắm thảo mộc bồn gỗ, xông hơi trị liệu cổ truyền",
                ImageUrl = "https://images.unsplash.com/photo-1515377905703-c4788e51af15?q=80&w=600"
            };
            var catSpa = new Category
            {
                Name = "Spa – Massage",
                Description = "Massage trị liệu cơ thể, xoa bóp bấm huyệt thông kinh lạc vai gáy",
                ImageUrl = "https://images.unsplash.com/photo-1544161515-4ab6ce6db874?q=80&w=600"
            };
            var catBeauty = new Category
            {
                Name = "Beauty",
                Description = "Chăm sóc da mặt chuyên sâu và gội đầu dưỡng sinh bằng thảo dược tự nhiên",
                ImageUrl = "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?q=80&w=600"
            };
            var catWellness = new Category
            {
                Name = "Wellness – Thư giãn",
                Description = "Thiền định, trị liệu chuông xoay Tây Tạng và phục hồi năng lượng tâm trí",
                ImageUrl = "https://images.unsplash.com/photo-1506126613408-eca07ce68773?q=80&w=600"
            };

            context.Categories.AddRange(catDongY, catSpa, catBeauty, catWellness);
            await context.SaveChangesAsync();

            context.Services.AddRange(
                new Service { Title = "Liệu trình Ngâm bồn Thảo dược Cổ truyền", Description = "Bắt mạch & Ngâm bồn gỗ thông thảo mộc 24 vị", Price = 350000, DurationMinutes = 45, CategoryId = catDongY.Id },
                new Service { Title = "Massage Trị liệu Cổ Vai Gáy Chuyên sâu", Description = "Thông kinh lạc, xoa bóp bấm huyệt vai gáy hết đau mỏi", Price = 450000, DurationMinutes = 60, CategoryId = catSpa.Id },
                new Service { Title = "Gội đầu Dưỡng sinh Thảo mộc Thư giãn", Description = "Gội đầu bồ kết, xông da đầu & massage mặt tự nhiên", Price = 250000, DurationMinutes = 45, CategoryId = catBeauty.Id },
                new Service { Title = "Trị liệu Chuông xoay & Thiền định Tâm trí", Description = "Tĩnh tâm, giải tỏa stress với âm tần chuông xoay", Price = 500000, DurationMinutes = 60, CategoryId = catWellness.Id }
            );
            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra trong quá trình khởi tạo dữ liệu ban đầu.");
    }
}

app.Run();