using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Threading;
using System.Windows.Data;
using ReversiGameSystem.Properties;

namespace ReversiGameSystem.Localization;

/// <summary>
/// Provides localized text and updates WPF bindings when the language changes.
/// </summary>
public sealed class LocalizationManager : INotifyPropertyChanged
{
    private const string EnglishCultureName = "en-US";
    private const string SpanishCultureName = "es-MX";

    private CultureInfo _culture = CultureInfo.GetCultureInfo(EnglishCultureName);

    private LocalizationManager()
    {
        string cultureName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es"
            ? SpanishCultureName
            : EnglishCultureName;

        SetCulture(cultureName);
    }

    /// <summary>
    /// Gets the shared language selection for the application.
    /// </summary>
    public static LocalizationManager Instance { get; } = new LocalizationManager();

    /// <summary>
    /// Occurs when the selected culture or a localized value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the supported UI culture name.
    /// </summary>
    public string CultureName
    {
        get { return _culture.Name; }
        set
        {
            TrySetCulture(value);
        }
    }

    internal bool IsChangingCulture { get; private set; }

    /// <summary>
    /// Gets a localized string from the application resources.
    /// </summary>
    /// <param name="key">The resource key to look up.</param>
    public string this[string key]
    {
        get { return Resources.ResourceManager.GetString(key, _culture) ?? string.Empty; }
    }

    internal LanguageChangeResult TrySetCulture(string cultureName)
    {
        if (!IsSupportedCulture(cultureName))
        {
            return LanguageChangeResult.NotAvailable;
        }

        if (cultureName == CultureName)
        {
            return LanguageChangeResult.Success;
        }

        if (!HasCompleteResources(cultureName))
        {
            return LanguageChangeResult.ResourcesError;
        }

        SetCulture(cultureName);
        IsChangingCulture = true;
        try
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CultureName)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
        }
        finally
        {
            IsChangingCulture = false;
        }

        return LanguageChangeResult.Success;
    }

    private static bool IsSupportedCulture(string cultureName)
    {
        return cultureName == EnglishCultureName || cultureName == SpanishCultureName;
    }

    private static bool HasCompleteResources(string cultureName)
    {
        try
        {
            ResourceManager manager = Resources.ResourceManager;
            ResourceSet? source = manager.GetResourceSet(
                CultureInfo.InvariantCulture, true, false);
            CultureInfo requestedCulture = cultureName == EnglishCultureName
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(cultureName);
            ResourceSet? target = manager.GetResourceSet(requestedCulture, true, false);
            return source != null
                && target != null
                && ContainsAllResources(source, target);
        }
        catch (MissingManifestResourceException)
        {
            return false;
        }
        catch (MissingSatelliteAssemblyException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    private static bool ContainsAllResources(ResourceSet source, ResourceSet target)
    {
        foreach (DictionaryEntry entry in source)
        {
            if (entry.Key is string key && target.GetString(key) == null)
            {
                return false;
            }
        }

        return true;
    }

    private void SetCulture(string cultureName)
    {
        _culture = CultureInfo.GetCultureInfo(cultureName);
        Thread.CurrentThread.CurrentCulture = _culture;
        Thread.CurrentThread.CurrentUICulture = _culture;
        CultureInfo.DefaultThreadCurrentCulture = _culture;
        CultureInfo.DefaultThreadCurrentUICulture = _culture;
    }
}

internal enum LanguageChangeResult
{
    Success,
    NotAvailable,
    ResourcesError
}
