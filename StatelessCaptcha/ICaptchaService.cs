namespace StatelessCaptcha;

/// <summary>
/// سرویس تولید و اعتبارسنجی کپچای کاملاً Stateless.
/// هیچ وضعیتی روی سرور نگه نمی‌دارد؛ اعتبارسنجی فقط با امضای HMAC داخل توکن انجام می‌شود.
/// </summary>
public interface ICaptchaService
{
    /// <summary>
    /// یک کد تصادفی تولید می‌کند، آن را امضا کرده و تصویر کپچا را می‌سازد.
    /// </summary>
    /// <returns>تصویر PNG و توکن امضاشده.</returns>
    CaptchaResult GenerateCaptcha();

    /// <summary>
    /// کد واردشدهٔ کاربر را در برابر توکن اعتبارسنجی می‌کند.
    /// این متد هیچ استثنایی به بیرون نمی‌فرستد؛ هر خطایی به معنای <c>false</c> است.
    /// </summary>
    /// <param name="userCode">کدی که کاربر وارد کرده است.</param>
    /// <param name="token">توکنی که هنگام تولید کپچا به کلاینت داده شده بود.</param>
    /// <returns><c>true</c> اگر کد و توکن معتبر و منقضی‌نشده باشند؛ در غیر این صورت <c>false</c>.</returns>
    bool ValidateCaptcha(string? userCode, string? token);
}
