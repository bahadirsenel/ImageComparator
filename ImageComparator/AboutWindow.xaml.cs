using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace ImageComparator
{
    /// <summary>
    /// About dialog window displaying application information.
    /// </summary>
    /// <remarks>
    /// Shows application version, developer information, license details,
    /// and links to the GitHub repository.
    /// </remarks>
    public partial class AboutWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AboutWindow"/> class.
        /// </summary>
        public AboutWindow()
        {
            InitializeComponent();
            LoadLocalization();
        }

        private void LoadLocalization()
        {
            Title = LocalizationManager.GetString("Dialog.AboutTitle");
            versionLabel.Text = LocalizationManager.GetString("Dialog.Version");
            licenseLabel.Text = LocalizationManager.GetString("Dialog.License");
            copyrightText.Text = LocalizationManager.GetString("Dialog.Copyright");
            descriptionText.Text = LocalizationManager.GetString("Dialog.Description");
            developerText.Text = LocalizationManager.GetString("Dialog.Developer");
            closeButton.Content = LocalizationManager.GetString("Dialog.CloseButton");
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
                string errorTitle = LocalizationManager.GetString("Dialog.Error");
                string errorMessage = LocalizationManager.GetString("Dialog.CouldNotOpenLink", ex.Message);
                MessageBox.Show(errorMessage, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
