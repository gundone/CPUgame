using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CPUgame.Core;

public static class LocalizationManager
{
    private static Dictionary<string, string> _strings = new();
    private static string _currentLanguage = "en";
    private static readonly string LocalizationFolder;
    private static readonly string SettingsFile;

    public static string CurrentLanguage => _currentLanguage;

    public static event Action? LanguageChanged;

    static LocalizationManager()
    {
        LocalizationFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization");
        SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    }

    public static void Initialize()
    {
        // Load saved language preference
        var savedLang = LoadSavedLanguage();
        LoadLanguage(savedLang);
    }

    public static void LoadLanguage(string languageCode)
    {
        var filePath = Path.Combine(LocalizationFolder, $"{languageCode}.json");

        if (!File.Exists(filePath))
        {
            // Fall back to English
            filePath = Path.Combine(LocalizationFolder, "en.json");
            languageCode = "en";
        }

        if (!File.Exists(filePath))
        {
            System.Diagnostics.Debug.WriteLine($"Localization file not found: {filePath}");
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data != null)
            {
                _strings = data;
                _currentLanguage = languageCode;
                SaveLanguagePreference(languageCode);
                LanguageChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load localization: {ex.Message}");
        }
    }

    private static string LoadSavedLanguage()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (settings != null && settings.TryGetValue("language", out var lang))
                {
                    return lang;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
        return "en";
    }

    private static void SaveLanguagePreference(string languageCode)
    {
        try
        {
            var settings = new Dictionary<string, string>();

            // Load existing settings first
            if (File.Exists(SettingsFile))
            {
                var existingJson = File.ReadAllText(SettingsFile);
                var existingSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson);
                if (existingSettings != null)
                {
                    settings = existingSettings;
                }
            }

            settings["language"] = languageCode;
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public static string Get(string key)
    {
        if (_strings.TryGetValue(key, out var value))
        {
            return value;
        }
        // Return key as fallback
        return key;
    }

    public static string Get(string key, params object[] args)
    {
        var format = Get(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }

    public static List<string> GetAvailableLanguages()
    {
        var languages = new List<string>();

        if (!Directory.Exists(LocalizationFolder))
            return languages;

        foreach (var file in Directory.GetFiles(LocalizationFolder, "*.json"))
        {
            var langCode = Path.GetFileNameWithoutExtension(file);
            languages.Add(langCode);
        }

        return languages;
    }

    public static string GetLanguageName(string langCode)
    {
        return langCode switch
        {
            "en" => "English",
            "ru" => "Русский",
            _ => langCode
        };
    }
}
