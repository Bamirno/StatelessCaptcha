using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace StatelessCaptcha;

/// <summary>
/// پیاده‌سازی پیش‌فرض <see cref="ICaptchaService"/>.
/// این سرویس کاملاً Stateless است: کد داخل توکن امضا می‌شود و هیچ‌چیز روی سرور ذخیره نمی‌شود.
/// به همین دلیل در محیط‌های چندسروری (Load Balanced) بدون تنظیمات اضافی کار می‌کند.
/// </summary>
public sealed class CaptchaService : ICaptchaService
{
    // جداکنندهٔ بخش‌های توکن. از کاراکتری استفاده می‌شود که در Base64 و کدهای کپچا حضور ندارد.
    internal const char TokenSeparator = ':';

    private readonly CaptchaOptions _options;
    private readonly CaptchaGenerator _generator;
    private readonly byte[] _secretKeyBytes;

    /// <summary>
    /// سازندهٔ سرویس.
    /// </summary>
    /// <param name="options">تنظیمات کپچا.</param>
    /// <param name="generator">مولد تصویر کپچا.</param>
    public CaptchaService(IOptions<CaptchaOptions> options, CaptchaGenerator generator)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));

        if (string.IsNullOrEmpty(_options.SecretKey))
        {
            throw new ArgumentException("SecretKey تنظیم نشده است. کلید HMAC الزامی است.", nameof(options));
        }

        _secretKeyBytes = Encoding.UTF8.GetBytes(_options.SecretKey);
    }

    /// <inheritdoc/>
    public CaptchaResult GenerateCaptcha()
    {
        var code = GenerateCode();
        var token = BuildToken(code);

        var imageBytes = _generator.GenerateImage(code, _options);

        return new CaptchaResult
        {
            ImageBytes = imageBytes,
            Token = token
        };
    }

    /// <inheritdoc/>
    public bool ValidateCaptcha(string? userCode, string? token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userCode) || string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var parts = token.Split(TokenSeparator);
            if (parts.Length != 3)
            {
                return false;
            }

            var code = parts[0];
            var expiryText = parts[1];
            var signature = parts[2];

            // بررسی انقضا.
            if (!long.TryParse(expiryText, out var expiryUnix) || expiryUnix <= 0)
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (expiryUnix < now)
            {
                return false;
            }

            // محاسبهٔ مجدد امضا و مقایسهٔ زمان‌ثابت (جلوگیری از Timing Attack).
            var expectedSignature = ComputeSignature(code, expiryText);
            var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
            var actualBytes = Encoding.UTF8.GetBytes(signature);

            if (expectedBytes.Length != actualBytes.Length ||
                !CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
            {
                return false;
            }

            // مقایسهٔ کد واردشده با کد استخراج‌شده از توکن (بدون حساسیت به حروف بزرگ/کوچک).
            return string.Equals(code, userCode.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // هر خطای پیش‌بینی‌نشده به معنای توکن نامعتبر است.
            return false;
        }
    }

    /// <summary>
    /// تولید کد تصادفی با استفاده از <see cref="RandomNumberGenerator"/> (امن و رمزنگارانه).
    /// </summary>
    private string GenerateCode()
    {
        var count = Math.Clamp(_options.CharacterCount, 1, 32);
        var alphabet = _options.CharacterSet switch
        {
            CaptchaCharacterSet.Alphanumeric => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            _ => "0123456789"
        };

        Span<char> code = count <= 32 ? stackalloc char[count] : new char[count];
        Span<byte> randomBytes = count <= 32 ? stackalloc byte[count] : new byte[count];

        RandomNumberGenerator.Fill(randomBytes);

        for (var i = 0; i < count; i++)
        {
            // تبدیل بایت تصادفی به ایندکس، بدون خطای مقیاس‌بندی (modulo bias برای کپچا قابل چشم‌پوشی است).
            code[i] = alphabet[randomBytes[i] % alphabet.Length];
        }

        return code.ToString();
    }

    /// <summary>
    /// ساخت توکن به فرمت {code}:{expiry}:{signature}.
    /// </summary>
    private string BuildToken(string code)
    {
        var expiry = DateTimeOffset.UtcNow
            .AddMinutes(Math.Max(1, _options.ExpirationMinutes))
            .ToUnixTimeSeconds();

        var expiryText = expiry.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var signature = ComputeSignature(code, expiryText);

        return $"{code}{TokenSeparator}{expiryText}{TokenSeparator}{signature}";
    }

    /// <summary>
    /// محاسبهٔ امضای HMAC-SHA256 به‌صورت Base64 برای ترکیب «code:expiry».
    /// </summary>
    private string ComputeSignature(string code, string expiryText)
    {
        var payload = $"{code}{TokenSeparator}{expiryText}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(_secretKeyBytes);
        var hash = hmac.ComputeHash(payloadBytes);

        return Convert.ToBase64String(hash)
            .TrimEnd('=')          // حذف padding تا در URL/Form بدون مشکل باشد.
            .Replace('+', '-')
            .Replace('/', '_');    // Base64Url.
    }
}
