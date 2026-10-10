using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SkiaSharp;
using StatelessCaptcha;

// کلید تست حداقل ۳۲ کاراکتر.
const string SecretKey = "test-secret-key-for-unit-tests-32+";

static CaptchaService CreateService(Action<CaptchaOptions>? tweak = null)
{
    var options = new CaptchaOptions
    {
        SecretKey = SecretKey,
        CharacterCount = 6,
        ExpirationMinutes = 5
    };
    tweak?.Invoke(options);
    return new CaptchaService(new OptionsWrapper<CaptchaOptions>(options), new CaptchaGenerator());
}

var failures = 0;

void Assert(string name, bool condition)
{
    Console.WriteLine($"{(condition ? "PASS" : "FAIL")}  {name}");
    if (!condition)
    {
        Interlocked.Increment(ref failures);
    }
}

// ── تست ۱: تولید کپچا ──
var service = CreateService();
var captcha = service.GenerateCaptcha();
Assert("تصویر PNG تولید می‌شود", captcha.ImageBytes.Length > 0);
Assert("تصویر با امضای PNG شروع می‌شود",
    captcha.ImageBytes.Length > 8 &&
    captcha.ImageBytes[0] == 0x89 && captcha.ImageBytes[1] == 0x50 && captcha.ImageBytes[2] == 0x4E && captcha.ImageBytes[3] == 0x47);
Assert("توکن شامل دو جداکننده است", captcha.Token.Count(c => c == ':') == 2);
Assert("DataUri فرمت درست دارد", captcha.DataUri.StartsWith("data:image/png;base64,", StringComparison.Ordinal));

// ── تست ۲: استخراج کد از توکن و اعتبارسنجی موفق ──
var parts = captcha.Token.Split(':');
var code = parts[0];
Assert("اعتبارسنجی با کد درست موفق است", service.ValidateCaptcha(code, captcha.Token));
Assert("اعتبارسنجی به حروف بزرگ/کوچک حساس نیست", service.ValidateCaptcha(code.ToLowerInvariant(), captcha.Token));

// ── تست ۳: کد اشتباه ──
Assert("کد اشتباه رد می‌شود", !service.ValidateCaptcha("000000", captcha.Token));

// ── تست ۴: کد خالی یا توکن خالی ──
Assert("کد خالی رد می‌شود", !service.ValidateCaptcha("", captcha.Token));
Assert("توکن خالی رد می‌شود", !service.ValidateCaptcha(code, ""));
Assert("هر دو خالی رد می‌شوند", !service.ValidateCaptcha(null, null));

// ── تست ۵: توکن دستکاری‌شده (Tamper Protection) ──
Assert("توکن با کد جابجا‌شده رد می‌شود", !service.ValidateCaptcha(code, captcha.Token + "x"));
Assert("توکن ناقص رد می‌شود", !service.ValidateCaptcha(code, captcha.Token[..^3]));
Assert("توکن با بخش اضافی رد می‌شود", !service.ValidateCaptcha(code, captcha.Token + ":extra"));
Assert("توکن نامعتبر رد می‌شود", !service.ValidateCaptcha(code, "garbage"));

// ── تست ۶: انقضا ──
var expired = CreateService(o => o.ExpirationMinutes = 1);
var expiredCaptcha = expired.GenerateCaptcha();
Assert("توکن جدید هنوز منقضی نشده", expired.ValidateCaptcha(expiredCaptcha.Token.Split(':')[0], expiredCaptcha.Token));

// ساخت یک توکن منقضی‌شدهٔ دستی با بازنویسی تاریخ انقضا.
var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var payload = $"{expiredCaptcha.Token.Split(':')[0]}:{now - 10}";
using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(SecretKey));
var sig = System.Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload)))
    .TrimEnd('=').Replace('+', '-').Replace('/', '_');
var expiredToken = $"{payload}:{sig}";
Assert("توکن منقضی‌شده رد می‌شود", !expired.ValidateCaptcha(payload.Split(':')[0], expiredToken));

// ── تست ۷: کلید متفاوت → توکن نامعتبر ──
var otherService = CreateService(o => o.SecretKey = "another-valid-secret-key-32-chars!!!");
Assert("توکن صادرشده با کلید دیگر رد می‌شود",
    !otherService.ValidateCaptcha(code, captcha.Token));

// ── تست ۸: مجموعه کاراکتر الفبایی-عددی ──
var alphaService = CreateService(o => o.CharacterSet = CaptchaCharacterSet.Alphanumeric);
var alphaCaptcha = alphaService.GenerateCaptcha();
Assert("کپچای الفبایی-عددی تولید می‌شود", alphaCaptcha.ImageBytes.Length > 0);
Assert("کد الفبایی-عددی اعتبارسنجی می‌شود",
    alphaService.ValidateCaptcha(alphaCaptcha.Token.Split(':')[0], alphaCaptcha.Token));

// ── تست ۹: تولید چندباره → کدهای متفاوت ──
var codes = new HashSet<string>();
for (var i = 0; i < 50; i++)
{
    codes.Add(service.GenerateCaptcha().Token.Split(':')[0]);
}
Assert("۵۰ کد تولیدشده یکتا هستند", codes.Count == 50);

// ── تست ۱۰: طول کد قابل‌کانفیگ ──
var len4 = CreateService(o => o.CharacterCount = 4);
var lenCode = len4.GenerateCaptcha().Token.Split(':')[0];
Assert("تعداد کاراکترها قابل تنظیم است", lenCode.Length == 4);

// ── تست ۱۱: استثناآزادی بودن ValidateCaptcha ──
Assert("توکن‌های عجیب استثنا نمی‌اندازند", !service.ValidateCaptcha("123456", "::"));
Assert("رشتهٔ تصادفی استثنا نمی‌اندازد", !service.ValidateCaptcha("123456", new string('a', 500)));

// ── تست ۱۲: DI validation (از طریق AddStatelessCaptcha) ──
bool Threw(Action<CaptchaOptions> config)
{
    var services = new ServiceCollection();
    try
    {
        services.AddStatelessCaptcha(config);
        return false;
    }
    catch (ArgumentException)
    {
        return true;
    }
}

Assert("SecretKey خالی در زمان ثبت استثنه می‌اندازد",
    Threw(o => o.SecretKey = ""));
Assert("SecretKey کوتاه در زمان ثبت استثنه می‌اندازد",
    Threw(o => o.SecretKey = "short"));
Assert("SecretKey خالی (null) در زمان ثبت استثنه می‌اندازد",
    Threw(o => o.SecretKey = null!));
Assert("ثبت موفق با کلید معتبر استثنه نمی‌اندازد",
    !Threw(o => o.SecretKey = SecretKey));

// ── تست ۱۳: ParseColor ──
var white = CaptchaGenerator.ParseColor("#FFFFFF", SKColors.Black);
Assert("ParseColor #FFFFFF را درست parses می‌کند", white == SKColors.White);
var red = CaptchaGenerator.ParseColor("#F00", SKColors.Black);
Assert("ParseColor #F00 (مخفف) را درست parses می‌کند", red == SKColors.Red);
var semi = CaptchaGenerator.ParseColor("#80808080", SKColors.Black);
Assert("ParseColor #AARRGGBB را درست parses می‌کند", semi.Alpha == 0x80 && semi.Red == 0x80);
Assert("رشتهٔ نامعتبر به رنگ پیش‌فرض برمی‌گردد", CaptchaGenerator.ParseColor("not-a-color", SKColors.Magenta) == SKColors.Magenta);
Assert("رشتهٔ null به رنگ پیش‌فرض برمی‌گردد", CaptchaGenerator.ParseColor(null, SKColors.Magenta) == SKColors.Magenta);

Console.WriteLine();
Console.WriteLine(failures == 0
    ? "✅ همهٔ تست‌ها موفق بودند."
    : $"❌ {failures} تست ناموفق بود.");
return failures == 0 ? 0 : 1;
