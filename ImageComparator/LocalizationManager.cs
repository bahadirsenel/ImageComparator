using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace ImageComparator
{
    /// <summary>
    /// Manages application localization by loading language strings from JSON files.
    /// 
    /// How to add a new language:
    /// 1. Create a new JSON file in the Resources/Localization folder (e.g., "de-DE.json" for German)
    /// 2. Copy the structure from en-US.json and translate all the values
    /// 3. Add the JSON file to the .csproj as a Content item with CopyToOutputDirectory set to PreserveNewest
    /// 4. Add a new MenuItem in MainWindow.xaml for the new language
    /// 5. In the MenuItem's Click handler, call LocalizationManager.SetLanguage("de-DE")
    /// 
    /// The localization system will automatically:
    /// - Load the new language file
    /// - Update all UI strings
    /// - Raise the LanguageChanged event
    /// - Update the isEnglish flag for backward compatibility
    /// </summary>
    public static class LocalizationManager
    {
        private static Dictionary<string, string> _strings = new Dictionary<string, string>();
        private static string _currentLanguage = "en-US";

        /// <summary>
        /// Event raised when the language is changed
        /// </summary>
        public static event EventHandler LanguageChanged;

        /// <summary>
        /// Gets the current language code (e.g., "en-US", "tr-TR")
        /// </summary>
        public static string CurrentLanguage => _currentLanguage;

        /// <summary>
        /// Initializes the localization manager with the default language
        /// </summary>
        static LocalizationManager()
        {
            LoadLanguage("en-US");
        }

        /// <summary>
        /// Sets the current language and loads the corresponding resource file
        /// </summary>
        /// <param name="languageCode">Language code (e.g., "en-US", "tr-TR")</param>
        public static void SetLanguage(string languageCode)
        {
            if (_currentLanguage == languageCode)
                return;

            LoadLanguage(languageCode);
            _currentLanguage = languageCode;
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Gets a localized string by its key
        /// </summary>
        /// <param name="key">The string key (e.g., "Menu.File")</param>
        /// <returns>The localized string, or the key if not found</returns>
        public static string GetString(string key)
        {
            if (_strings.TryGetValue(key, out string value))
            {
                return value;
            }

            // Return the key if translation not found (for debugging)
            return $"[{key}]";
        }

        /// <summary>
        /// Gets a localized string with parameter substitution
        /// </summary>
        /// <param name="key">The string key</param>
        /// <param name="parameters">Parameters to substitute in the format string</param>
        /// <returns>The formatted localized string</returns>
        public static string GetString(string key, params object[] parameters)
        {
            string format = GetString(key);
            try
            {
                // Replace {0}, {1}, etc. with parameters
                for (int i = 0; i < parameters.Length; i++)
                {
                    format = format.Replace($"{{{i}}}", parameters[i]?.ToString() ?? "");
                }
                return format;
            }
            catch
            {
                return format;
            }
        }

        /// <summary>
        /// Loads language strings from a JSON file
        /// </summary>
        /// <param name="languageCode">Language code</param>
        private static void LoadLanguage(string languageCode)
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(basePath, "Resources", "Localization", $"{languageCode}.json");

                if (!File.Exists(filePath))
                {
                    // Fallback to en-US if file not found
                    if (languageCode != "en-US")
                    {
                        filePath = Path.Combine(basePath, "Resources", "Localization", "en-US.json");
                    }

                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException($"Language file not found: {filePath}");
                    }
                }

                string json = File.ReadAllText(filePath);
                var serializer = new JavaScriptSerializer();
                _strings = serializer.Deserialize<Dictionary<string, string>>(json);
            }
            catch (Exception ex)
            {
                // In case of error, initialize with empty dictionary
                _strings = new Dictionary<string, string>();
                System.Diagnostics.Debug.WriteLine($"Error loading language file: {ex.Message}");
            }
        }
    }
}
