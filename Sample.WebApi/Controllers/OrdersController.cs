using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using StatelessCaptcha;

namespace Sample.WebApi.Controllers;

/// <summary>
/// اکشن محافظت‌شده با کپچا (نمونه).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    /// <summary>
    /// یک درخواست نمونه.
    /// </summary>
    public sealed class OrderRequest
    {
        /// <summary>نام مشتری.</summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>آدرس ایمیل.</summary>
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>مبلغ سفارش.</summary>
        public decimal Amount { get; set; }

        /// <summary>کد کپچای واردشده توسط کاربر.</summary>
        public string CaptchaCode { get; set; } = string.Empty;

        /// <summary>توکن کپچا (دریافت‌شده از api/captcha).</summary>
        public string CaptchaToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// ثبت سفارش. این اکشن فقط در صورت معتبر بودن کپچا اجرا می‌شود.
    /// کد و توکن کپچا می‌توانند از فرم یا Query String ارسال شوند.
    /// </summary>
    [HttpPost("submit")]
    [Captcha]
    public IActionResult Submit([FromForm] OrderRequest request)
    {
        return Ok(new
        {
            success = true,
            message = "سفارش با موفقیت ثبت شد.",
            order = new
            {
                request.Name,
                request.Email,
                request.Amount
            }
        });
    }
}
