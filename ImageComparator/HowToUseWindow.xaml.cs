using System;
using System.IO;
using System.Windows;

namespace ImageComparator
{
    public partial class HowToUseWindow : Window
    {
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
