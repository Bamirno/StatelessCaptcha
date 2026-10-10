# StatelessCaptcha

> کتابخانهٔ حرفه‌ای تولید و اعتبارسنجی **CAPTCHA** برای ASP.NET Core — کاملاً **Stateless**، بدون دیتابیس و بدون حافظهٔ سرور.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![SkiaSharp](https://img.shields.io/badge/SkiaSharp-3.116.1-orange)](https://github.com/mono/SkiaSharp)
[![License](https://img.shields.io/badge/license-MIT-blue)](#مجوز)

---

## 📖 فهرست

1. [معرفی و ویژگی‌ها](#۱-معرفی-و-ویژگیها)
2. [نصب](#۲-نصب)
3. [راه‌اندازی در Program.cs](#۳-راهاندازی-در-programcs)
4. [تنظیم appsettings.json](#۴-تنظیم-appsettingsjson)
5. [اکشن دریافت کپچا](#۵-اکشن-دریافت-کپچا)
6. [محافظت از اکشن با ‎[Captcha]‎](#۶-محافظت-از-اکشن-با-captcha)
7. [نمونهٔ فرانت‌اند](#۷-نمونهٔ-فرانتاند-html--javascript)
8. [تست با curl](#۸-تست-با-curl)
9. [جدول کامل تنظیمات](#۹-جدول-کامل-تنظیمات)
10. [تم‌های آماده](#۱۰-تمهای-آماده)
11. [نکات Docker](#۱۱-نکات-docker)
12. [نکات امنیتی](#۱۲-نکات-امنیتی)

---

## ۱) معرفی و ویژگی‌ها

**StatelessCaptcha** یک کتابخانهٔ Class Library برای ASP.NET Core 10 است که کپچا تولید و اعتبارسنجی می‌کند
**بدون آنکه هیچ چیزی روی سرور ذخیره کند**. کد کپچا همراه با زمان انقضا با **HMAC-SHA256** امضا می‌شود و
همهٔ اطلاعات لازم برای اعتبارسنجی داخل خود توکن قرار دارد.

### چرا Stateless؟

| روش سنتی | StatelessCaptcha |
|---|---|
| ذخیرهٔ کد در `Session` یا `IMemoryCache` | ❌ هیچ ذخیره‌سازی‌ای لازم نیست |
| نیاز به Redis/DB برای چندسروری | ✅ روی چند سرور بدون تنظیمات کار می‌کند |
| از دست رفتن کپچا با ری‌استارت یا بازیابی Session | ✅ توکن مستقل از سرور است |
| مصرف حافظه با افزایش کاربران | ✅ مصرف حافظهٔ صفر |

### ویژگی‌ها

- 🎯 **کاملاً Stateless** — بدون دیتابیس، بدون Session، بدون حافظهٔ سرور
- 🔐 **امضای HMAC-SHA256** با مقایسهٔ زمان-ثابت (`FixedTimeEquals`) برای جلوگیری از Timing Attack
- 🎨 **ظاهر کاملاً کانفیگ‌پذیر** — رنگ پس‌زمینه، رنگ متن‌ها، خطوط اعوجاج، نقاط نویز، فونت و ابعاد
- 🌈 **پشتیبانی از رنگ‌های هگز** `#RGB`، `#RRGGBB` و `#AARRGGBB` (با شفافیت)
- 🖼️ **SkiaSharp** — چندسکویی و سازگار با Linux/Docker (بدون `System.Drawing`)
- 🔢 **دو مجموعه کاراکتر** — `Numeric` و `Alphanumeric`
- 🏷️ **فیلتر آمادهٔ `[Captcha]`** — کد و توکن را از `Form` یا `Query String` می‌خواند
- ⚙️ **تزریق وابستگی (DI)** با `IOptions<CaptchaOptions>`
- 🔄 **سازگار با Load Balancer** — همهٔ سرورها فقط به یک `SecretKey` مشترک نیاز دارند
- 📦 **قابل انتشار به‌عنوان NuGet**
- 🚫 **بدون خواندن از Environment Variables** — کلید را مصرف‌کننده تحویل می‌دهد

### نحوهٔ کار

```
┌─────────┐                    ┌──────────────────────────────────────┐
│ Client  │  GET /api/captcha  │              Server                  │
│         │ ─────────────────► │                                      │
│         │                    │  1. تولید کد تصادفی (RNG امن)        │
│         │                    │  2. expiry = now + 5min              │
│         │                    │  3. sig = HMAC-SHA256(secret,        │
│         │                    │            "code:expiry")            │
│         │                    │  4. token = "code:expiry:sig"        │
│         │ ◄───────────────── │  5. رندر PNG با SkiaSharp            │
│         │  {image, token}    │                                      │
│         │                    │         (هیچ ذخیره‌سازی‌ای نیست)      │
└─────────┘                    └──────────────────────────────────────┘
     │
     │  POST /api/orders/submit
     │  captchaCode=470813 & captchaToken=470813:1791637967:2Cw4vu...
     ▼
┌──────────────────────────────────────────────────────────────────────┐
│  CaptchaFilter:                                                      │
│    ۱. Split توکن با ':' → باید دقیقاً ۳ بخش باشد                     │
│    ۲. بررسی انقضا (expiry < now → رد)                                │
│    ۳. محاسبهٔ مجدد HMAC + مقایسه با FixedTimeEquals                   │
│    ۴. مقایسهٔ کد کاربر با کد استخراج‌شده                              │
│    → معتبر: ادامهٔ زنجیره   |   نامعتبر: 400 CAPTCHA_INVALID          │
└──────────────────────────────────────────────────────────────────────┘
```

---

## ۲) نصب

```bash
dotnet add package StatelessCaptcha
```

### نصب وابستگی‌های بومی برای Linux / Docker

SkiaSharp برای اجرا روی Linux به کتابخانه‌های بومی نیاز دارد:

```bash
dotnet add package SkiaSharp.NativeAssets.Linux
```

اگر روی Alpine Linux اجرا می‌کنید، به‌جای آن از بستهٔ مخصوص musl استفاده کنید:

```bash
dotnet add package SkiaSharp.NativeAssets.Linux.NoDependencies
```

> 💡 روی Windows و macOS نیازی به این پکیج‌ها نیست.

---

## ۳) راه‌اندازی در Program.cs

```csharp
using StatelessCaptcha;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddStatelessCaptcha(options =>
{
    // ── کلید امضا (اجباری، حداقل ۳۲ کاراکتر) ──
    options.SecretKey = builder.Configuration["Captcha:SecretKey"]!;

    // ── تنظیمات رفتاری ──
    options.CharacterCount = 6;
    options.CharacterSet = CaptchaCharacterSet.Numeric;
    options.ExpirationMinutes = 5;

    // ── ابعاد تصویر ──
    options.Width = 220;
    options.Height = 80;

    // ── رنگ‌ها ──
    options.BackgroundColor = "#F5F5F5";
    options.TextColors = new() { "#0D47A1", "#1B5E20", "#B71C1C", "#4A148C" };
    options.DistortionLineColor = "#80808080";
    options.DistortionLineCount = 5;
    options.NoiseColor = "#CCCCCC";
    options.NoiseCount = 120;

    // ── فونت ──
    options.FontFamily = "Arial";
    options.MinFontSize = 24;
    options.MaxFontSize = 36;
});

var app = builder.Build();

app.UseStaticFiles();
app.MapControllers();
app.Run();
```

### روش جایگزین: اتصال مستقیم به یک بخش کانفیگ

اگر ترجیح می‌دهید همهٔ تنظیمات را در `appsettings.json` نگه دارید:

```csharp
// همهٔ مقادیر از بخش "Captcha" خوانده می‌شوند.
builder.Services.AddStatelessCaptcha(builder.Configuration.GetSection("Captcha"));

// یا با override کردن چند مقدار:
builder.Services.AddStatelessCaptcha(
    builder.Configuration.GetSection("Captcha"),
    options => options.Width = 260);
```

> **نکتهٔ مهم دربارهٔ کلید:** کتابخانه هیچ‌گاه خودش به دنبال کلید نمی‌رود و چیزی از
> Environment Variables نمی‌خواند. **شما** کلید را از هر جایی که صلاح می‌دانید
> (فایل کانفیگ، Key Vault، دیتابیس، سرویس رمزنگاری خودتان، …) می‌خوانید و به کتابخانه می‌دهید.
> تنها شرط این است که حداقل ۳۲ کاراکتر و برای همهٔ سرورها یکسان باشد.

---

## ۴) تنظیم appsettings.json

```json
{
  "Captcha": {
    "SecretKey": "your-32-char-secret-key-here!!"
  }
}
```

> ⚠️ در محیط عملیاتی، این کلید را در فایل کانفیگ داخل مخزن قرار ندهید. آن را از هر مکانیزم
> مدیریت راز که خودتان استفاده می‌کنید بخوانید و به `options.SecretKey` بدهید.

---

## ۵) اکشن دریافت کپچا

```csharp
using Microsoft.AspNetCore.Mvc;
using StatelessCaptcha;

[ApiController]
[Route("api/[controller]")]
public class CaptchaController : ControllerBase
{
    private readonly ICaptchaService _captchaService;

    public CaptchaController(ICaptchaService captchaService)
    {
        _captchaService = captchaService;
    }

    /// <summary>
    /// تولید کپچای جدید. خروجی شامل تصویر (Data URI) و توکن است.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var captcha = _captchaService.GenerateCaptcha();

        return Ok(new
        {
            image = captcha.DataUri,   // "data:image/png;base64,..."
            token = captcha.Token
        });
    }
}
```

### خروجی نمونه

```json
{
  "image": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAANwAAABQ...",
  "token": "470813:1791637967:2Cw4vuRAmGjDFpKINn5nLatWO-W8ILvI_uq4DQI5wzg"
}
```

اگر می‌خواهید تصویر خام PNG را برگردانید:

```csharp
[HttpGet("image.png")]
public IActionResult GetImage()
{
    var captcha = _captchaService.GenerateCaptcha();
    return File(captcha.ImageBytes, "image/png");
}
```

> 💡 برای این حالت باید توکن را جداگانه به کلاینت برسانید (پارامتر، هدر، یا کوکی).

---

## ۶) محافظت از اکشن با ‎[Captcha]‎

کافی است Attribute `[Captcha]` را روی اکشن (یا کل کنترلر) بگذارید:

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    public sealed class OrderRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        // نام این دو پراپرتی با CaptchaCodeFieldName / CaptchaTokenFieldName هم‌خوان است.
        public string CaptchaCode { get; set; } = string.Empty;
        public string CaptchaToken { get; set; } = string.Empty;
    }

    [HttpPost("submit")]
    [Captcha]                                   // ← محافظت
    public IActionResult Submit([FromForm] OrderRequest request)
    {
        // اگر به اینجا رسیدیم، کپچا معتبر بوده است.
        return Ok(new { success = true, message = "سفارش ثبت شد." });
    }
}
```

### رفتار فیلتر

- کد و توکن از **`Form`** خوانده می‌شوند اگر `Content-Type` فرم باشد.
- در غیر این صورت از **`Query String`** خوانده می‌شوند.
- نام فیلدها از `CaptchaCodeFieldName` و `CaptchaTokenFieldName` می‌آید.
- در صورت نامعتبر بودن، پاسخ زیر برگردانده می‌شود و اکشن **هرگز اجرا نمی‌شود**:

```json
{
  "error": "CAPTCHA_INVALID",
  "message": "کد امنیتی نامعتبر است یا منقضی شده است. لطفاً کد جدیدی دریافت کنید."
}
```

با کد وضعیت **HTTP 400**.

### اعتبارسنجی دستی (بدون فیلتر)

اگر می‌خواهید خودتان کنترل کنید:

```csharp
public class MyService
{
    private readonly ICaptchaService _captcha;

    public MyService(ICaptchaService captcha) => _captcha = captcha;

    public bool CheckIt(string? userCode, string? token)
        => _captcha.ValidateCaptcha(userCode, token);
}
```

---

## ۷) نمونهٔ فرانت‌اند (HTML + JavaScript)

```html
<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
    <meta charset="UTF-8">
    <title>فرم با کپچا</title>
</head>
<body>
    <form id="orderForm">
        <label>نام: <input type="text" name="name" required></label><br>
        <label>ایمیل: <input type="email" name="email" required></label><br>

        <!-- تصویر کپچا: با کلیک، تصویر جدید گرفته می‌شود -->
        <img id="captchaImage" alt="کد امنیتی" title="برای تغییر کلیک کنید"
             style="cursor:pointer; border:1px solid #ccc; border-radius:6px; vertical-align:middle;">

        <!-- فیلدهای کپچا با همان نام‌های پیش‌فرض -->
        <input type="text" name="captchaCode" id="captchaCode"
               placeholder="کد امنیتی" required autocomplete="off">
        <input type="hidden" name="captchaToken" id="captchaToken">

        <button type="submit">ارسال</button>
    </form>

    <script>
        const img = document.getElementById('captchaImage');
        const tokenField = document.getElementById('captchaToken');
        const codeField = document.getElementById('captchaCode');

        async function loadCaptcha() {
            const res = await fetch('/api/captcha');
            const data = await res.json();
            img.src = data.image;          // Data URI
            tokenField.value = data.token; // توکن امضاشده
            codeField.value = '';          // پاک کردن ورودی قبلی
        }

        // کلیک روی تصویر → کپچای جدید
        img.addEventListener('click', loadCaptcha);

        document.getElementById('orderForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            const formData = new FormData(e.target);

            const res = await fetch('/api/orders/submit', {
                method: 'POST',
                body: formData
            });

            if (res.ok) {
                alert('با موفقیت ارسال شد.');
            } else {
                const err = await res.json();
                alert(err.message || 'خطا');
                // ⚠️ توکن مصرف‌شده را دور بریزید و کپچای جدید بگیرید
                loadCaptcha();
            }
        });

        // بارگذاری اولیه
        loadCaptcha();
    </script>
</body>
</html>
```

نسخهٔ کامل و زیباتر این صفحه در پروژهٔ نمونه قرار دارد:
`Sample.WebApi/wwwroot/index.html`

---

## ۸) تست با curl

### مرحلهٔ ۱ — دریافت کپچا

```bash
curl -s http://localhost:5000/api/captcha
```

خروجی:

```json
{
  "image": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAANwAAABQ...",
  "token": "470813:1791637967:2Cw4vuRAmGjDFpKINn5nLatWO-W8ILvI_uq4DQI5wzg"
}
```

### مرحلهٔ ۲ — استخراج کد از توکن (برای تست)

در حالت واقعی کاربر کد را از تصویر می‌خواند، اما برای تست خودکار کد داخل توکن است:

```bash
TOKEN=$(curl -s http://localhost:5000/api/captcha | jq -r '.token')
CODE=$(echo "$TOKEN" | cut -d':' -f1)
echo "Code: $CODE"
```

### مرحلهٔ ۳ — ارسال فرم با کد و توکن

```bash
curl -s -X POST http://localhost:5000/api/orders/submit \
  -F "name=Ali" \
  -F "email=ali@example.com" \
  -F "amount=150000" \
  -F "captchaCode=$CODE" \
  -F "captchaToken=$TOKEN"
```

خروجی موفق:

```json
{"success":true,"message":"سفارش با موفقیت ثبت شد.","order":{"name":"Ali","email":"ali@example.com","amount":150000}}
```

### تست حالت‌های نامعتبر

```bash
# کد اشتباه → 400 CAPTCHA_INVALID
curl -s -X POST http://localhost:5000/api/orders/submit \
  -F "name=Ali" -F "email=a@b.com" -F "captchaCode=000000" -F "captchaToken=$TOKEN"

# توکن دستکاری‌شده → 400 CAPTCHA_INVALID
curl -s -X POST http://localhost:5000/api/orders/submit \
  -F "name=Ali" -F "email=a@b.com" -F "captchaCode=$CODE" -F "captchaToken=${TOKEN}x"

# بدون توکن → 400 CAPTCHA_INVALID
curl -s -X POST http://localhost:5000/api/orders/submit \
  -F "name=Ali" -F "email=a@b.com" -F "captchaCode=$CODE"
```

### ارسال از طریق Query String

```bash
curl -s -X POST "http://localhost:5000/api/orders/submit?captchaCode=$CODE&captchaToken=$TOKEN"
```

---

## ۹) جدول کامل تنظیمات

| تنظیم | نوع | پیش‌فرض | توضیح |
|---|---|---|---|
| `SecretKey` | `string` | — | **اجباری.** کلید امضای HMAC-SHA256، حداقل ۳۲ کاراکتر. باید روی همهٔ سرورها یکسان باشد. |
| `ExpirationMinutes` | `int` | `5` | مدت اعتبار توکن به دقیقه. |
| `CharacterCount` | `int` | `6` | تعداد کاراکترهای کد. |
| `CharacterSet` | `CaptchaCharacterSet` | `Numeric` | `Numeric` (ارقام ۰-۹) یا `Alphanumeric` (ارقام + A-Z). |
| `CaptchaCodeFieldName` | `string` | `"captchaCode"` | نام فیلد کد در Form/Query. |
| `CaptchaTokenFieldName` | `string` | `"captchaToken"` | نام فیلد توکن در Form/Query. |
| `Width` | `int` | `200` | عرض تصویر به پیکسل. |
| `Height` | `int` | `70` | ارتفاع تصویر به پیکسل. |
| `BackgroundColor` | `string` | `"#FFFFFF"` | رنگ پس‌زمینه (`#RGB` / `#RRGGBB` / `#AARRGGBB`). |
| `TextColors` | `List<string>` | ۶ رنگ متنوع | رنگ کاراکترها؛ هر کاراکتر یک رنگ تصادفی می‌گیرد. |
| `DistortionLineColor` | `string` | `"#80808080"` | رنگ خطوط اعوجاج (با alpha). |
| `DistortionLineCount` | `int` | `4` | تعداد خطوط اعوجاج. |
| `NoiseColor` | `string` | `"#D3D3D3"` | رنگ نقاط نویز. |
| `NoiseCount` | `int` | `100` | تعداد نقاط نویز. |
| `FontFamily` | `string` | `"Arial"` | نام فونت کاراکترها. |
| `MinFontSize` | `int` | `20` | حداقل اندازهٔ فونت. |
| `MaxFontSize` | `int` | `32` | حداکثر اندازهٔ فونت. |

### مقادیر پیش‌فرض `TextColors`

```csharp
new() { "#0D47A1", "#1B5E20", "#B71C1C", "#4A148C", "#E65100", "#006064" }
```

### فرمت رنگ‌ها

| فرمت | مثال | توضیح |
|---|---|---|
| `#RGB` | `#F00` | مخفف؛ هر رقم ۱۷ برابر می‌شود → `#FF0000` |
| `#RRGGBB` | `#0D47A1` | بدون شفافیت |
| `#AARRGGBB` | `#80808080` | آلفا اول می‌آید (`AA` = شفافیت) |

اگر رشتهٔ رنگ نامعتبر باشد، به‌جای پرتاب استثنا از یک رنگ پیش‌فرض امن استفاده می‌شود.

---

## ۱۰) تم‌های آماده

### 🌙 تم تیره (Dark)

```csharp
options.BackgroundColor = "#1E1E1E";
options.TextColors = new() { "#80DEEA", "#FFCC80", "#A5D6A7", "#F48FB1", "#CE93D8" };
options.DistortionLineColor = "#40FFFFFF";
options.DistortionLineCount = 5;
options.NoiseColor = "#33FFFFFF";
options.NoiseCount = 140;
options.FontFamily = "Arial";
options.MinFontSize = 26;
options.MaxFontSize = 38;
```

### 🌸 تم پاستلی (Pastel)

```csharp
options.BackgroundColor = "#FDF6E3";
options.TextColors = new() { "#B39DDB", "#81C784", "#FFB74D", "#E57373", "#4DB6AC" };
options.DistortionLineColor = "#30A1887F";
options.DistortionLineCount = 3;
options.NoiseColor = "#E8DFC8";
options.NoiseCount = 90;
options.MinFontSize = 24;
options.MaxFontSize = 34;
```

### ⬜ تم مینیمال (Minimal)

```csharp
options.BackgroundColor = "#FFFFFF";
options.TextColors = new() { "#212121" };
options.DistortionLineColor = "#20000000";
options.DistortionLineCount = 2;
options.NoiseColor = "#00000000";   // بدون نویز
options.NoiseCount = 0;
options.FontFamily = "Arial";
options.MinFontSize = 30;
options.MaxFontSize = 30;
options.Width = 200;
options.Height = 70;
```

### 🏢 تم سازمانی (Corporate)

```csharp
options.BackgroundColor = "#F5F7FA";
options.TextColors = new() { "#0D47A1", "#1565C0", "#283593", "#37474F" };
options.DistortionLineColor = "#50808080";
options.DistortionLineCount = 4;
options.NoiseColor = "#DDE3EA";
options.NoiseCount = 100;
options.MinFontSize = 24;
options.MaxFontSize = 36;
```

---

## ۱۱) نکات Docker

### Dockerfile نمونه

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# ── پیش‌نیازهای بومی SkiaSharp ──
# fontconfig برای پیدا کردن فونت‌ها حیاتی است؛ بدون آن متن روی تصویر رسم نمی‌شود.
RUN apt-get update && apt-get install -y --no-install-recommends \
        fontconfig \
        libfontconfig1 \
        fonts-liberation \
        libfreetype6 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Sample.WebApi/Sample.WebApi.csproj", "Sample.WebApi/"]
COPY ["StatelessCaptcha/StatelessCaptcha.csproj", "StatelessCaptcha/"]
RUN dotnet restore "Sample.WebApi/Sample.WebApi.csproj"
COPY . .
WORKDIR "/src/Sample.WebApi"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Sample.WebApi.dll"]
```

### پکیج‌های مورد نیاز

```xml
<ItemGroup>
  <PackageReference Include="SkiaSharp" Version="3.116.1" />
  <!-- برای Linux/Docker: -->
  <PackageReference Include="SkiaSharp.NativeAssets.Linux" Version="3.116.1" />
</ItemGroup>
```

### نکات مهم

- **`fontconfig` و `libfontconfig1` الزامی هستند.** بدون آن‌ها SkiaSharp نمی‌تواند فونت را پیدا کند
  و تصویر بدون متن (یا با مربع خالی) تولید می‌شود.
- **فونت‌ها را نصب کنید.** اگر `FontFamily = "Arial"` گذاشته‌اید اما Arial روی ایمیج نیست،
  SkiaSharp به فونت پیش‌فرض برمی‌گردد. برای نتیجهٔ پایدار از `fonts-liberation` استفاده کنید
  و `FontFamily = "Liberation Sans"` بگذارید، یا فونت دلخواه را در ایمیج کپی کنید.
- **بررسی فونت‌های نصب‌شده:**
  ```bash
  fc-list | head -20
  ```
- برای ایمیج‌های **Alpine** از `SkiaSharp.NativeAssets.Linux.NoDependencies` استفاده کنید.
- اگر می‌خواهید از فونت سفارشی خودتان استفاده کنید:
  ```dockerfile
  COPY ./fonts/MyFont.ttf /usr/share/fonts/truetype/myfont/MyFont.ttf
  RUN fc-cache -f -v
  ```

---

## ۱۲) نکات امنیتی

### 🔑 مدیریت کلید

- **`SecretKey` را هرگز در کد هاردکد نکنید** و در مخزن git قرار ندهید.
- کلید را از مکانیزم مدیریت راز خودتان بخوانید و به `options.SecretKey` بدهید
  (کتابخانه خودش هیچ منبعی را نمی‌خواند).
- از یک کلید تصادفی حداقل ۳۲ بایتی استفاده کنید:
  ```bash
  # تولید یک کلید تصادفی ۴۸ کاراکتری
  openssl rand -base64 36
  ```
- **چرخش کلید (Key Rotation):** کتابخانه فقط یک کلید فعال دارد. برای چرخش بدون قطعی سرویس،
  دو نسخهٔ اپلیکیشن را موقتاً با کلیدهای متفاوت اجرا نکنید؛ بلکه کلید را در همهٔ سرورها
  هم‌زمان به‌روزرسانی کنید (توکن‌های قدیمی در این فاصله باطل می‌شوند).

### 🔒 الزامی بودن HTTPS

توکن را فقط روی **HTTPS** منتقل کنید. در غیر این صورت مهاجم می‌تواند توکن را شنود کند و
پیش از کاربر از آن استفاده کند (Replay Attack).

```csharp
app.UseHttpsRedirection();
app.UseHsts();
```

### 🚦 Rate Limiting روی اکشن تولید کپچا

بدون محدودیت نرخ، مهاجم می‌تواند هزاران کپچا بگیرد و منابع سرور (CPU برای رندر PNG) را بسوزاند:

```csharp
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("captcha-generator", window =>
    {
        window.Window = TimeSpan.FromSeconds(10);
        window.PermitLimit = 20;
    });
});

app.UseRateLimiter();

// روی اکشن:
[HttpGet]
[EnableRateLimiting("captcha-generator")]
public IActionResult Get() { ... }
```

همچنین روی اکشن‌های محافظت‌شده، محدودیت نرخ بر اساس IP اعمال کنید تا حملهٔ Brute Force
روی کد ۶ رقمی (تنها ۱۰⁶ حالت) عملی نباشد.

### 🛡️ محدودیت ذاتی کپچا

- **کپچا جایگزین CSRF Token نیست.** کپچا ثابت می‌کند «یک انسان فرم را پر کرده»؛
  CSRF ثابت می‌کند «درخواست از سایت خودمان آمده». این دو مکمل یکدیگرند، نه جانشین.
- **کپچا جایگزین احراز هویت نیست.** اکشن‌های حساس باید `[Authorize]` هم داشته باشند.
- **کپچا ضد ربات قطعی نیست.** کپچای تصویری ساده در برابر OCR و سرویس‌های حل کپچا
  آسیب‌پذیر است. برای محافظت حیاتی، CAPTCHA را با یک لایهٔ تحلیلی رفتار کاربر ترکیب کنید.

### ⏱️ Timeout کوتاه توکن

- مقدار پیش‌فرض `ExpirationMinutes = 5` را حفظ کنید. توکن طولانی‌تر، پنجرهٔ حملهٔ
  Replay و Brute Force را بزرگ‌تر می‌کند.
- برای فرم‌های حساس می‌توانید آن را به ۲ دقیقه کاهش دهید.
- **توکن یک‌بارمصرف نیست:** چون هیچ وضعیتی ذخیره نمی‌شود، یک توکن معتبر تا لحظهٔ انقضا
  چندین بار قابل استفاده است. اگر نیاز به یک‌بارمصرف بودن دارید، به‌ناچار باید وضعیت را
  جایی ذخیره کنید که با فلسفهٔ Stateless این کتابخانه در تعارض است.
- **پس از هر تلاش (موفق یا ناموفق)، کپچای جدید صادر کنید.** این کار حملهٔ Brute Force را
  به تعداد درخواست‌های مجاز محدود می‌کند.

### 🔍 نکات پیاده‌سازی امنیتی کتابخانه

| مکانیزم | توضیح |
|---|---|
| `RandomNumberGenerator` | تولید کد با RNG رمزنگارانه (نه `System.Random`) |
| `HMACSHA256` | امضای توکن؛ کد بدون کلید قابل جعل نیست |
| `CryptographicOperations.FixedTimeEquals` | مقایسهٔ زمان-ثابت امضا برای جلوگیری از Timing Attack |
| Base64Url | امضا بدون `+`، `/` و `=` تا در URL و Form بی‌مشکل منتقل شود |
| `try/catch` سراسری | هر ورودی نامعتبر → `false`، بدون نشت اطلاعات یا استثنا |
| بررسی طول امضا | مقایسه فقط روی بایت‌های هم‌طول انجام می‌شود |

---

## 🧪 پروژهٔ نمونه و تست‌ها

این مخزن شامل دو پروژهٔ کمکی است:

| پروژه | توضیح |
|---|---|
| `Sample.WebApi` | Web API کامل با کنترلر کپچا، اکشن محافظت‌شده و صفحهٔ دموی HTML |
| `StatelessCaptcha.Tests` | ۳۲ تست خودکار برای تولید، اعتبارسنجی، انقضا، دستکاری توکن و تجزیهٔ رنگ |

اجرای تست‌ها:

```bash
dotnet run --project StatelessCaptcha.Tests
```

اجرای نمونهٔ Web API:

```bash
dotnet run --project Sample.WebApi --urls http://localhost:5000
```

سپس در مرورگر باز کنید: `http://localhost:5000/index.html`

---

## 📄 مجوز

MIT
