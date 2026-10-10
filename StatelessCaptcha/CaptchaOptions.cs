namespace StatelessCaptcha;

/// <summary>
/// تمام تنظیمات قابل‌کانفیگ کپچا.
/// این گزینه‌ها از طریق <c>AddStatelessCaptcha</c> به کتابخانه داده می‌شوند؛
/// کتابخانه خودش هیچ‌چیزی را از Environment Variables یا فایل کانفیگ نمی‌خواند
/// و کلیدها را دقیقاً همان‌طور که مصرف‌کننده تحویل می‌دهد استفاده می‌کند.
/// </summary>
public class CaptchaOptions
{
    /// <summary>
    /// کلید امضای HMAC-SHA256. این کلید <b>اجباری</b> است و در زمان ثبت سرویس بررسی می‌شود.
    /// کلید باید یک رشتهٔ تصادفی و مخفی باشد و حداقل ۳۲ کاراکتر داشته باشد.
    /// هر کلیدی که بتواند یک توکن معتبر بسازد می‌تواند کپچا را دور بزند، پس آن را مثل یک رمز عبور مدیریت کنید.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// مدت اعتبار توکن به دقیقه. پس از این مدت توکن منقضی می‌شود. پیش‌فرض: ۵.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// تعداد کاراکترهای کد کپچا. پیش‌فرض: ۶.
    /// </summary>
    public int CharacterCount { get; set; } = 6;

    /// <summary>
    /// مجموعه کاراکترهای مجاز برای ساخت کد. پیش‌فرض: <see cref="CaptchaCharacterSet.Numeric"/>.
    /// </summary>
    public CaptchaCharacterSet CharacterSet { get; set; } = CaptchaCharacterSet.Numeric;

    /// <summary>
    /// نام فیلدی که کد واردشدهٔ کاربر در درخواست با آن شناسایی می‌شود. پیش‌فرض: "captchaCode".
    /// </summary>
    public string CaptchaCodeFieldName { get; set; } = "captchaCode";

    /// <summary>
    /// نام فیلدی که توکن کپچا در درخواست با آن شناسایی می‌شود. پیش‌فرض: "captchaToken".
    /// </summary>
    public string CaptchaTokenFieldName { get; set; } = "captchaToken";

    /// <summary>
    /// عرض تصویر کپچا به پیکسل. پیش‌فرض: ۲۰۰.
    /// </summary>
    public int Width { get; set; } = 200;

    /// <summary>
    /// ارتفاع تصویر کپچا به پیکسل. پیش‌فرض: ۷۰.
    /// </summary>
    public int Height { get; set; } = 70;

    /// <summary>
    /// رنگ پس‌زمینهٔ تصویر به فرمت هگز (#RRGGBB یا #AARRGGBB). پیش‌فرض: "#FFFFFF".
    /// </summary>
    public string BackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// لیست رنگ‌های متن به فرمت هگز. هر کاراکتر یک رنگ تصادفی از این لیست دریافت می‌کند.
    /// پیش‌فرض: شش رنگ متنوع.
    /// </summary>
    public List<string> TextColors { get; set; } = new()
    {
        "#0D47A1",
        "#1B5E20",
        "#B71C1C",
        "#4A148C",
        "#E65100",
        "#006064"
    };

    /// <summary>
    /// رنگ خطوط اعوجاج به فرمت هگز (#RRGGBB یا #AARRGGBB). پیش‌فرض: "#80808080".
    /// </summary>
    public string DistortionLineColor { get; set; } = "#80808080";

    /// <summary>
    /// تعداد خطوط اعوجاج رسم‌شده روی تصویر. پیش‌فرض: ۴.
    /// </summary>
    public int DistortionLineCount { get; set; } = 4;

    /// <summary>
    /// رنگ نقاط نویز به فرمت هگز (#RRGGBB یا #AARRGGBB). پیش‌فرض: "#D3D3D3".
    /// </summary>
    public string NoiseColor { get; set; } = "#D3D3D3";

    /// <summary>
    /// تعداد نقاط نویز رسم‌شده روی تصویر. پیش‌فرض: ۱۰۰.
    /// </summary>
    public int NoiseCount { get; set; } = 100;

    /// <summary>
    /// نام خانواده فونت مورد استفاده برای رسم کاراکترها. پیش‌فرض: "Arial".
    /// روی Linux باید فونتی که نصب می‌کنید با این مقدار مطابقت داشته باشد.
    /// </summary>
    public string FontFamily { get; set; } = "Arial";

    /// <summary>
    /// حداقل اندازه فونت کاراکترها به پیکسل. پیش‌فرض: ۲۰.
    /// </summary>
    public int MinFontSize { get; set; } = 20;

    /// <summary>
    /// حداکثر اندازه فونت کاراکترها به پیکسل. پیش‌فرض: ۳۲.
    /// </summary>
    public int MaxFontSize { get; set; } = 32;
}
