using BusTicketSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using BusTicketSystem.Models;
using BusTicketSystem.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options=>options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Ví dụ: session hết hạn sau 30 phút không hoạt động
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication("CookieAuth") // "CookieAuth" là tên scheme, phải khớp với scheme dùng trong SignInAsync
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "BusTicketSystem.AuthCookie";
        options.LoginPath = "/Account/Login/Login"; // Đường dẫn đến trang đăng nhập
        options.AccessDeniedPath = "/Error?errorCode=AccessDenied"; // Trang hiển thị khi truy cập bị từ chối
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Thời gian cookie hết hạn
        options.SlidingExpiration = true; // Gia hạn cookie nếu người dùng hoạt động
    });

// builder.Services.Configure<MailerSendEmailSenderOptions>(builder.Configuration.GetSection("MailerSend"));
// builder.Services.AddTransient<IEmailSender,MailerSendEmailSender>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication(); // Thêm dòng này, và đặt trước UseAuthorization

app.UseAuthorization();
app.MapRazorPages();
app.Run();