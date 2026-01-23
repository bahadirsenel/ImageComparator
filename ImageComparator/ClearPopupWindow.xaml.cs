using System.Windows;

namespace ImageComparator
{
    public partial class ClearPopupWindow : Window
    {
        MainWindow mainWindow;

        public ClearPopupWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            LoadLocalization();
        }

        private void LoadLocalization()
        {
            saveResultsButton.Content = LocalizationManager.GetString("Dialog.SaveButton");
            discardResultsButton.Content = LocalizationManager.GetString("Dialog.DiscardButton");
            cancelResultsButton.Content = LocalizationManager.GetString("Dialog.CancelButton");
            warningTextBlock.Text = LocalizationManager.GetString("Dialog.ClearMessage");
            Title = LocalizationManager.GetString("Dialog.ClearTitle");
        }

        private void saveResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow.SaveResults())
            {
                mainWindow.Clear();
                Close();
            };
        }

        private void discardResultsButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Clear();
            Close();
        }

        private void cancelResultsButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
