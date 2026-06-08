using System.Globalization;
using System.Resources;
using Microsoft.JSInterop;

namespace MusicTeacher.WebAssembly.Services;

public sealed class AppLocalizer(IJSRuntime jsRuntime)
{
    private const string StorageKey = "music-teacher-culture";
    private const string DefaultCultureName = "en";
    private static readonly ResourceManager ResourceManager = new(
        "MusicTeacher.WebAssembly.Resources.AppStrings",
        typeof(AppLocalizer).Assembly);

    public static readonly IReadOnlyList<SupportedCulture> SupportedCultures =
    [
        new("en", "LanguageEnglish"),
        new("nl", "LanguageDutch")
    ];

    public CultureInfo CurrentCulture => CultureInfo.CurrentUICulture;

    public string this[string key] => Get(key);

    public async Task InitializeAsync()
    {
        var storedCultureName = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        SetCurrentCulture(ResolveCulture(storedCultureName ?? CultureInfo.CurrentUICulture.Name));
    }

    public async Task SetCultureAsync(string cultureName)
    {
        var culture = ResolveCulture(cultureName);
        SetCurrentCulture(culture);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, culture.Name);
    }

    public string Format(string key, params object[] args)
        => string.Format(CultureInfo.CurrentUICulture, Get(key), args);

    public string GetCultureDisplayName(SupportedCulture culture)
        => Get(culture.DisplayNameKey);

    private static string Get(string key)
        => ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]";

    private static CultureInfo ResolveCulture(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return CultureInfo.GetCultureInfo(DefaultCultureName);
        }

        var supportedCulture = SupportedCultures.FirstOrDefault(culture =>
            string.Equals(culture.Name, cultureName, StringComparison.OrdinalIgnoreCase) ||
            cultureName.StartsWith(culture.Name + "-", StringComparison.OrdinalIgnoreCase));

        return CultureInfo.GetCultureInfo(supportedCulture?.Name ?? DefaultCultureName);
    }

    private static void SetCurrentCulture(CultureInfo culture)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}

public sealed record SupportedCulture(string Name, string DisplayNameKey);
