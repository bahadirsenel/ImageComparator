using System;
using System.IO;
using System.Windows;

namespace ImageComparator
{
    /// <summary>
    /// Help window displaying usage instructions for the application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Loads localized help content from markdown files based on the current language.
    /// Falls back to English if a language-specific file is not available.
    /// </para>
    /// <para>
    /// Help files are stored in the Resources directory with naming pattern: HowToUse_{languageSuffix}.md,
    /// where {languageSuffix} is a short code (for example, "en", "tr") derived from the current culture
    /// (for example, "en-US", "tr-TR"). See <see cref="GetHowToUseFileName(string)"/> for the exact
    /// mapping and fallback behavior.
    /// </para>
    /// </remarks>
    public partial class HowToUseWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HowToUseWindow"/> class.
        /// </summary>
        public HowToUseWindow()
        {
            InitializeComponent();
            LoadLocalization();
        }

        private void LoadLocalization()
        {
            Title = LocalizationManager.GetString("Dialog.HowToUseTitle");
            closeButton.Content = LocalizationManager.GetString("Dialog.HowToUseCloseButton");
            
            // Load content based on current language
            string fileName = GetHowToUseFileName(LocalizationManager.CurrentLanguage);
            contentTextBlock.Text = LoadContentFromFile(fileName);
        }

        /// <summary>
        /// Gets the appropriate HowToUse markdown file name based on the language code
        /// Falls back to English if a specific language file doesn't exist
        /// </summary>
        /// <param name="languageCode">The current language code (e.g., "en-US", "tr-TR")</param>
        /// <returns>The markdown file name to load</returns>
        private string GetHowToUseFileName(string languageCode)
        {
            // Map language codes to their file suffixes
            string fileSuffix;
            switch (languageCode)
            {
                case "en-US":
                    fileSuffix = "en";
                    break;
                case "tr-TR":
                    fileSuffix = "tr";
                    break;
                case "ja-JP":
                    fileSuffix = "ja";
                    break;
                case "es-ES":
                    fileSuffix = "es";
                    break;
                case "fr-FR":
                    fileSuffix = "fr";
                    break;
                case "de-DE":
                    fileSuffix = "de";
                    break;
                case "it-IT":
                    fileSuffix = "it";
                    break;
                case "pt-BR":
                    fileSuffix = "pt";
                    break;
                case "ru-RU":
                    fileSuffix = "ru";
                    break;
                case "zh-CN":
                    fileSuffix = "zh";
                    break;
                case "ko-KR":
                    fileSuffix = "ko";
                    break;
                case "ar-SA":
                    fileSuffix = "ar";
                    break;
                case "fa-IR":
                    fileSuffix = "fa";
                    break;
                case "hi-IN":
                    fileSuffix = "hi";
                    break;
                case "nl-NL":
                    fileSuffix = "nl";
                    break;
                case "pl-PL":
                    fileSuffix = "pl";
                    break;
                case "sv-SE":
                    fileSuffix = "sv";
                    break;
                case "nb-NO":
                    fileSuffix = "nb";
                    break;
                case "da-DK":
                    fileSuffix = "da";
                    break;
                default:
                    fileSuffix = "en";
                    break;
            }

            string fileName = $"HowToUse_{fileSuffix}.md";
            
            // Check if the specific language file exists, otherwise fall back to English
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(basePath, "Resources", fileName);
            
            if (!File.Exists(filePath) && fileSuffix != "en")
            {
                // Fall back to English if the specific language file doesn't exist
                fileName = "HowToUse_en.md";
            }
            
            return fileName;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Loads the content from the specified help file.
        /// </summary>
        /// <param name="fileName">The name of the help file to load.</param>
        /// <returns>
        /// The content of the help file, or an error message if the file cannot be loaded.
        /// </returns>
        private string LoadContentFromFile(string fileName)
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(basePath, "Resources", fileName);
                
                if (File.Exists(filePath))
                {
                    return File.ReadAllText(filePath);
                }
                else
                {
                    return LocalizationManager.GetString("Dialog.HelpFileNotFound", filePath);
                }
            }
            catch (Exception ex)
            {
                return LocalizationManager.GetString("Dialog.ErrorLoadingHelp", ex.Message);
            }
        }
    }
}
