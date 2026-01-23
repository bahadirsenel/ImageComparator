using System;
using System.Windows;

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
                copyrightText.Text = "Copyright © 2016";
                descriptionText.Text = "Image Comparator is a powerful tool for comparing and analyzing images. " +
                                      "It helps you identify similar or duplicate images in your collection.";
                developerText.Text = "Developed by Bahadır Şenel\nGitHub: github.com/bahadirsenel/ImageComparator";
                closeButton.Content = "Close";
            }
            else
            {
                // Turkish
                Title = "Hakkında";
                versionLabel.Text = "Sürüm:";
                licenseLabel.Text = "Lisans:";
                copyrightText.Text = "Telif Hakkı © 2016";
                descriptionText.Text = "Image Comparator, görselleri karşılaştırmak ve analiz etmek için güçlü bir araçtır. " +
                                      "Koleksiyonunuzdaki benzer veya yinelenen görselleri belirlemenize yardımcı olur.";
                developerText.Text = "Geliştirici: Bahadır Şenel\nGitHub: github.com/bahadirsenel/ImageComparator";
                closeButton.Content = "Kapat";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
