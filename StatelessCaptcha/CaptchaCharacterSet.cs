namespace StatelessCaptcha;

/// <summary>
/// مجموعه کاراکترهایی که کد کپچا می‌تواند از آن‌ها تشکیل شود.
/// </summary>
public enum CaptchaCharacterSet
{
    /// <summary>
    /// فقط ارقام ۰ تا ۹ (پیش‌فرض). مناسب‌ترین حالت برای ورودی کاربران و موبایل.
    /// </summary>
    Numeric,

    /// <summary>
    /// ارقام ۰ تا ۹ به‌همراه حروف بزرگ انگلیسی A تا Z.
    /// </summary>
    Alphanumeric
}
