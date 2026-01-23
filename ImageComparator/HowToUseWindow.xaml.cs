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
            string fileName = LocalizationManager.CurrentLanguage == "en-US" ? "HowToUse_en.md" : "HowToUse_tr.md";
            contentTextBlock.Text = LoadContentFromFile(fileName);
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
