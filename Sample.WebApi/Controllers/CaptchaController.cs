using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StatelessCaptcha;

namespace Sample.WebApi.Controllers;

/// <summary>
/// اکشن دریافت کپچا.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CaptchaController : ControllerBase
{
    private readonly ICaptchaService _captchaService;

    /// <summary>
    /// سازندهٔ کنترلر.
    /// </summary>
    /// <param name="captchaService">سرویس تولید کپچا.</param>
    public CaptchaController(ICaptchaService captchaService)
    {
        _captchaService = captchaService;
    }

    /// <summary>
    /// یک تصویر کپچای جدید تولید می‌کند.
    /// خروجی شامل تصویر به‌صورت Data URI و توکن است.
    /// </summary>
    [HttpGet]
    [EnableRateLimiting("captcha-generator")]
    public IActionResult Get()
    {
        var captcha = _captchaService.GenerateCaptcha();

        return Ok(new
        {
            image = captcha.DataUri,
            token = captcha.Token,
            expiresInSeconds = 300
        });
    }

    /// <summary>
    /// تصویر خام PNG را برمی‌گرداند (برای استفاده مستقیم در تگ img).
    /// </summary>
    [HttpGet("image.png")]
    [EnableRateLimiting("captcha-generator")]
    public IActionResult GetImage()
    {
        var captcha = _captchaService.GenerateCaptcha();

        // توکن در کوکی HttpOnly قرار می‌گیرد تا کلاینت نیازی به مدیریت آن نداشته باشد.
        // توجه: این فقط یک نمونه است. برای حالت Stateless خالص، توکن را در سمت کلاینت نگه دارید.
        Response.Cookies.Append("captchaToken", captcha.Token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddMinutes(5)
        });

        return File(captcha.ImageBytes, "image/png");
    }
}
