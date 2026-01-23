using System;
using System.IO;
using System.Windows;

namespace ImageComparator
{
    public partial class HowToUseWindow : Window
    {
        public HowToUseWindow(bool isEnglish)
        {
            InitializeComponent();

            if (isEnglish)
            {
                Title = "How To Use";
                closeButton.Content = "Close";
                contentTextBlock.Text = LoadContentFromFile("HowToUse_en.md");
            }
            else
            {
                Title = "Nasıl Kullanılır";
                closeButton.Content = "Kapat";
                contentTextBlock.Text = LoadContentFromFile("HowToUse_tr.md");
            }
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
                    return $"Error: Could not find help file at {filePath}";
                }
            }
            catch (Exception ex)
            {
                return $"Error loading help content: {ex.Message}";
            }
        }
    }
}
