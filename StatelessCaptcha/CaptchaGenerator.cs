using System.Globalization;
using System.Security.Cryptography;
using SkiaSharp;

namespace StatelessCaptcha;

/// <summary>
/// تولید تصویر کپچا با SkiaSharp (چندسکویی و سازگار با Linux/Docker).
/// </summary>
public sealed class CaptchaGenerator
{
    // حداکثر زاویهٔ چرخش کاراکترها به درجه (بین -۱۵ تا +۱۵).
    private const float MaxRotationDegrees = 15f;

    // شعاع نقاط نویز.
    private const float NoiseRadius = 1.2f;

    // ضخامت خطوط اعوجاج.
    private const float DistortionStrokeWidth = 1.5f;

    /// <summary>
    /// یک رشتهٔ هگز رنگی را به <see cref="SKColor"/> تبدیل می‌کند.
    /// فرمت‌های پشتیبانی‌شده: #RGB، #RRGGBB و #AARRGGBB (با کانال شفافیت).
    /// </summary>
    /// <param name="hex">رشتهٔ رنگ (مثلاً "#FFFFFF" یا "#80808080").</param>
    /// <param name="defaultColor">در صورت نامعتبر بودن ورودی، این رنگ برگردانده می‌شود.</param>
    /// <returns>رنگ SkiaSharp.</returns>
    public static SKColor ParseColor(string? hex, SKColor defaultColor)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return defaultColor;
        }

        var value = hex.AsSpan().Trim();
        if (value.StartsWith('#'))
        {
            value = value[1..];
        }

        // #RGB → مقیاس‌بندی به #RRGGBB
        if (value.Length == 3 && TryParseHex(value, out var rgb3))
        {
            var r = (byte)((rgb3 >> 8) & 0xF);
            var g = (byte)((rgb3 >> 4) & 0xF);
            var b = (byte)(rgb3 & 0xF);
            return new SKColor((byte)(r * 17), (byte)(g * 17), (byte)(b * 17));
        }

        // #RRGGBB
        if (value.Length == 6 && TryParseHex(value, out var rgb))
        {
            return new SKColor((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));
        }

        // #AARRGGBB
        if (value.Length == 8 && TryParseHex(value, out var argb))
        {
            return new SKColor((byte)((argb >> 24) & 0xFF), (byte)((argb >> 16) & 0xFF),
                               (byte)((argb >> 8) & 0xFF), (byte)(argb & 0xFF));
        }

        return defaultColor;
    }

    /// <summary>
    /// تصویر کپچا را از روی کد داده‌شده می‌سازد.
    /// </summary>
    /// <param name="code">کدی که باید روی تصویر نوشته شود.</param>
    /// <param name="options">تنظیمات ظاهری تصویر.</param>
    /// <returns>تصویر PNG به‌صورت آرایه‌ای از بایت‌ها.</returns>
    public byte[] GenerateImage(string code, CaptchaOptions options)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(options);

        var width = Math.Max(32, options.Width);
        var height = Math.Max(16, options.Height);
        var backgroundColor = ParseColor(options.BackgroundColor, SKColors.White);
        var noiseColor = ParseColor(options.NoiseColor, SKColors.LightGray);
        var lineColor = ParseColor(options.DistortionLineColor, new SKColor(0x80, 0x80, 0x80, 0x80));

        var textColors = options.TextColors
            .Select(static c => ParseColor(c, SKColors.Black))
            .Distinct()
            .ToArray();

        if (textColors.Length == 0)
        {
            textColors = [SKColors.Black];
        }

        var minFontSize = Math.Clamp(options.MinFontSize, 8, 200);
        var maxFontSize = Math.Clamp(Math.Max(options.MinFontSize, options.MaxFontSize), minFontSize, 400);
        var fontFace = SKTypeface.FromFamilyName(options.FontFamily, SKFontStyle.Bold) ?? SKTypeface.Default;

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(backgroundColor);

        // ۱) نقاط نویز (قبل از متن تا زیر کاراکترها قرار گیرند)
        DrawNoise(canvas, width, height, noiseColor, options.NoiseCount);

        // ۲) کاراکترهای کد
        DrawText(canvas, code, width, height, textColors, minFontSize, maxFontSize, fontFace);

        // ۳) خطوط اعوجاج (بالای متن)
        DrawDistortionLines(canvas, width, height, lineColor, options.DistortionLineCount);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static bool TryParseHex(ReadOnlySpan<char> value, out int result)
    {
        result = 0;
        foreach (var c in value)
        {
            int digit = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'f' => c - 'a' + 10,
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => -1
            };

            if (digit < 0)
            {
                result = 0;
                return false;
            }

            result = result * 16 + digit;
        }

        return true;
    }

    private static void DrawNoise(SKCanvas canvas, int width, int height, SKColor color, int count)
    {
        count = Math.Clamp(count, 0, 5000);
        if (count <= 0)
        {
            return;
        }

        using var paint = new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        // هر نقطه ۸ بایت تصادفی نیاز دارد (۴ برای X و ۴ برای Y).
        Span<byte> buffer = count <= 512 ? stackalloc byte[count * 8] : new byte[count * 8];
        RandomNumberGenerator.Fill(buffer);

        for (var i = 0; i < count; i++)
        {
            var offset = i * 8;
            var x = BitConverter.ToUInt32(buffer.Slice(offset, 4)) % (uint)width;
            var y = BitConverter.ToUInt32(buffer.Slice(offset + 4, 4)) % (uint)height;
            canvas.DrawCircle(x, y, NoiseRadius, paint);
        }
    }

    private static void DrawText(SKCanvas canvas, string code, int width, int height, SKColor[] textColors,
        float minFontSize, float maxFontSize, SKTypeface fontFace)
    {
        var count = code.Length;
        if (count == 0)
        {
            return;
        }

        var slotWidth = (float)width / count;

        // هر کاراکتر ۱۲ بایت تصادفی نیاز دارد: ۴ برای اندازه، ۴ برای زاویه، ۴ برای رنگ.
        Span<byte> seedBuffer = count <= 32 ? stackalloc byte[count * 12] : new byte[count * 12];
        RandomNumberGenerator.Fill(seedBuffer);

        for (var i = 0; i < count; i++)
        {
            var offset = i * 12;

            var fontSizeRange = (uint)(maxFontSize - minFontSize) + 1;
            var fontSize = minFontSize + (BitConverter.ToUInt32(seedBuffer.Slice(offset, 4)) % fontSizeRange);
            var angle = -MaxRotationDegrees + (BitConverter.ToUInt32(seedBuffer.Slice(offset + 4, 4)) % (uint)(MaxRotationDegrees * 2 + 1));
            var color = textColors[BitConverter.ToUInt32(seedBuffer.Slice(offset + 8, 4)) % (uint)textColors.Length];

            using var font = new SKFont(fontFace, fontSize);
            using var paint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            // مرکزافقی و مرکزعمودی اسلاتِ این کاراکتر.
            var centerX = slotWidth * i + slotWidth / 2f;
            var centerY = height / 2f;

            // محاسبهٔ عرض گلیف برای وسط‌چین کردن افقی.
            var glyph = code[i].ToString();
            var glyphWidth = font.MeasureText(glyph);

            // تنظیم خط مبنای متن برای مرکزچین شدن عمودی.
            font.GetFontMetrics(out var metrics);
            var baselineY = centerY - (metrics.Ascent + metrics.Descent) / 2f;

            canvas.Save();
            canvas.Translate(centerX, centerY);
            canvas.RotateDegrees(angle);
            canvas.DrawText(glyph, -glyphWidth / 2f, baselineY - centerY, font, paint);
            canvas.Restore();
        }
    }

    private static void DrawDistortionLines(SKCanvas canvas, int width, int height, SKColor color, int count)
    {
        count = Math.Clamp(count, 0, 32);
        if (count <= 0)
        {
            return;
        }

        using var paint = new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = DistortionStrokeWidth
        };

        // هر خط ۱۲ بایت تصادفی نیاز دارد.
        Span<byte> buffer = count <= 32 ? stackalloc byte[count * 12] : new byte[count * 12];
        RandomNumberGenerator.Fill(buffer);

        for (var i = 0; i < count; i++)
        {
            var offset = i * 12;
            var x1 = BitConverter.ToUInt32(buffer.Slice(offset, 4)) % (uint)width;
            var y1 = BitConverter.ToUInt32(buffer.Slice(offset + 4, 4)) % (uint)height;
            var x2 = BitConverter.ToUInt32(buffer.Slice(offset + 8, 4)) % (uint)width;
            var y2 = BitConverter.ToUInt32(buffer.Slice(offset + 4, 4)) % (uint)height;

            using var path = new SKPath();
            path.MoveTo(x1, y1);
            path.QuadTo(width / 2f, height / 2f, x2, y2);
            canvas.DrawPath(path, paint);
        }
    }
}
