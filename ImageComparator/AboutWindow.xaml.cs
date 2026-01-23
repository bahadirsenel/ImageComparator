using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace ImageComparator
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow(bool isEnglish)
        {
            InitializeComponent();
            
            if (isEnglish)
            {
                // English
                Title = "About";
                versionLabel.Text = "Version:";
                licenseLabel.Text = "License:";
                copyrightText.Text = "Copyright © 2026";
                descriptionText.Text = "Image Comparator is a powerful tool for comparing and analyzing images. " +
                                      "It helps you identify similar or duplicate images in your collection.";
                developerText.Text = "Developed by Mustafa Bahadır Şenel";
                closeButton.Content = "Close";
            }
            else
            {
                // Turkish
                Title = "Hakkında";
                versionLabel.Text = "Sürüm:";
                licenseLabel.Text = "Lisans:";
                copyrightText.Text = "Telif Hakkı © 2026";
                descriptionText.Text = "Image Comparator, görselleri karşılaştırmak ve analiz etmek için güçlü bir araçtır. " +
                                      "Koleksiyonunuzdaki benzer veya yinelenen görselleri belirlemenize yardımcı olur.";
                developerText.Text = "Geliştirici: Mustafa Bahadır Şenel";
                closeButton.Content = "Kapat";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GithubLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
