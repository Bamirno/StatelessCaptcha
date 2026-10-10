using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace StatelessCaptcha;

/// <summary>
/// فیلتر اعتبارسنجی کپچا که کد و توکن را از درخواست استخراج کرده و بررسی می‌کند.
/// در صورت نامعتبر بودن، درخواست با وضعیت ۴۰۰ و کد خطای <c>CAPTCHA_INVALID</c> رد می‌شود.
/// </summary>
public sealed class CaptchaFilter : IAsyncActionFilter
{
    private readonly ICaptchaService _captchaService;
    private readonly CaptchaOptions _options;

    /// <summary>
    /// سازندهٔ فیلتر.
    /// </summary>
    /// <param name="captchaService">سرویس اعتبارسنجی کپچا.</param>
    /// <param name="options">تنظیمات کپچا.</param>
    public CaptchaFilter(ICaptchaService captchaService, IOptions<CaptchaOptions> options)
    {
        _captchaService = captchaService ?? throw new ArgumentNullException(nameof(captchaService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        // استخراج کد و توکن از Form (درخواست‌های multipart/form-urlencoded) یا Query String.
        string? code;
        string? token;

        if (request.HasFormContentType && request.Form.Count > 0)
        {
            code = request.Form[_options.CaptchaCodeFieldName].ToString();
            token = request.Form[_options.CaptchaTokenFieldName].ToString();
        }
        else
        {
            code = request.Query[_options.CaptchaCodeFieldName].ToString();
            token = request.Query[_options.CaptchaTokenFieldName].ToString();
        }

        if (!_captchaService.ValidateCaptcha(code, token))
        {
            context.Result = new BadRequestObjectResult(new
            {
                Error = "CAPTCHA_INVALID",
                Message = "کد امنیتی نامعتبر است یا منقضی شده است. لطفاً کد جدیدی دریافت کنید."
            });

            return;
        }

        await next();
    }
}
