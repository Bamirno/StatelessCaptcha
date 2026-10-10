using Microsoft.AspNetCore.Mvc;

namespace StatelessCaptcha;

/// <summary>
/// فیلتر اعتبارسنجی کپچا. این Attribute روی اکشن‌ها یا کل کنترلرها قرار می‌گیرد
/// و درخواست‌ها را فقط در صورت معتبر بودن کد و توکن کپچا عبور می‌دهد.
/// </summary>
/// <remarks>
/// از <see cref="Microsoft.AspNetCore.Mvc.TypeFilterAttribute"/> ارث‌بری می‌کند تا
/// <see cref="CaptchaFilter"/> بتواند از تزریق وابستگی‌ها (DI) استفاده کند.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CaptchaAttribute : TypeFilterAttribute
{
    /// <summary>
    /// مشخص می‌کند که فیلتر کپچا روی این اکشن/کنترلر اعمال شود.
    /// </summary>
    public CaptchaAttribute()
        : base(typeof(CaptchaFilter))
    {
        Order = 10; // قبل از فیلترهای احراز هویت سفارشی اجرا نشود، اما زودتر از فیلترهای دامنه.
    }
}
