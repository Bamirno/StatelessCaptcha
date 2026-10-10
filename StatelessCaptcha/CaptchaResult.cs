namespace StatelessCaptcha;

/// <summary>
/// نتیجهٔ تولید کپچا.
/// </summary>
public sealed class CaptchaResult
{
    /// <summary>
    /// تصویر کپچا به فرمت PNG به‌صورت آرایه‌ای از بایت‌ها.
    /// </summary>
    public required byte[] ImageBytes { get; init; }

    /// <summary>
    /// توکن امضاشده که باید در سمت کلاینت نگه داشته شود
    /// و هنگام ارسال درخواست به‌همراه کد واردشده برگردانده شود.
    /// این توکن stateless است و هیچ اطلاعاتی روی سرور ذخیره نمی‌کند.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// تصویر به‌صورت Data URI آماده برای استفاده مستقیم در تگ <c>&lt;img&gt;</c>.
    /// </summary>
    public string DataUri => $"data:image/png;base64,{Convert.ToBase64String(ImageBytes)}";
}
