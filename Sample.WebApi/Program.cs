using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using StatelessCaptcha;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ───────────────────────────────────────────────────────────
// ثبت کپچای Stateless
// کلید مخفی از کانفیگ برنامه خوانده می‌شود و به کتابخانه داده می‌شود.
// کتابخانه خودش هیچ‌چیزی را از Environment Variables نمی‌خواند.
// ───────────────────────────────────────────────────────────
builder.Services.AddStatelessCaptcha(options =>
{
    options.SecretKey = builder.Configuration["Captcha:SecretKey"]!;
    options.CharacterCount = 6;
    options.CharacterSet = CaptchaCharacterSet.Numeric;
    options.ExpirationMinutes = 5;
    options.Width = 220;
    options.Height = 80;
    options.BackgroundColor = "#F5F5F5";
    options.TextColors = new() { "#0D47A1", "#1B5E20", "#B71C1C", "#4A148C" };
    options.DistortionLineColor = "#80808080";
    options.DistortionLineCount = 5;
    options.NoiseColor = "#CCCCCC";
    options.NoiseCount = 120;
    options.FontFamily = "Arial";
    options.MinFontSize = 24;
    options.MaxFontSize = 36;
});

//Rate Limiting ساده روی اکشن تولید کپچا (جلوگیری از بمباران درخواست)
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.AddFixedWindowLimiter("captcha-generator", window =>
    {
        window.Window = TimeSpan.FromSeconds(10);
        window.PermitLimit = 20;
    });
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRateLimiter();
app.MapControllers();

app.Run();
