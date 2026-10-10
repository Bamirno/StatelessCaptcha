using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StatelessCaptcha;

/// <summary>
/// متدهای الحاقی برای ثبت سرویس‌های کپچا در کانتینر DI.
/// </summary>
public static class CaptchaServiceCollectionExtensions
{
    /// <summary>
    /// ثبت سرویس‌های کپچای Stateless.
    /// </summary>
    /// <param name="services">کالکشن سرویس‌ها.</param>
    /// <param name="configureOptions">کانفیگ گزینه‌ها. کلید <c>SecretKey</c> در اینجا اجباری است.</param>
    /// <example>
    /// <code>
    /// builder.Services.AddStatelessCaptcha(options =&gt;
    /// {
    ///     options.SecretKey = "your-secret-key";
    ///     options.CharacterCount = 6;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddStatelessCaptcha(
        this IServiceCollection services,
        Action<CaptchaOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new CaptchaOptions();
        configureOptions(options);

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            throw new ArgumentException(
                "SecretKey برای کپچا اجباری است. آن را از طریق کانفیگ کالبک AddStatelessCaptcha تنظیم کنید.",
                nameof(configureOptions));
        }

        if (options.SecretKey.Length < 32)
        {
            throw new ArgumentException(
                "طول SecretKey باید حداقل ۳۲ کاراکتر باشد.",
                nameof(configureOptions));
        }

        services.Configure(configureOptions);
        services.AddSingleton<CaptchaGenerator>();
        services.AddSingleton<ICaptchaService, CaptchaService>();

        return services;
    }

    /// <summary>
    /// ثبت سرویس‌های کپچای Stateless با استفاده از یک بخش از <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="services">کالکشن سرویس‌ها.</param>
    /// <param name="configuration">بخش کانفیگ مربوط به کپچا (مثلاً <c>builder.Configuration.GetSection("Captcha")</c>).</param>
    /// <param name="configureOptions">کانفیگ اضافی روی گزینه‌ها (اختیاری).</param>
    public static IServiceCollection AddStatelessCaptcha(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CaptchaOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddStatelessCaptcha(options =>
        {
            configuration.Bind(options);
            configureOptions?.Invoke(options);
        });
    }
}
