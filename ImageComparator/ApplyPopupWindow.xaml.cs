using System.Windows;

namespace ImageComparator
{
    public partial class ApplyPopupWindow : Window
    {
        MainWindow mainWindow;
        int deleteItemCount, markAsFalsePositiveItemCount;

        public ApplyPopupWindow(MainWindow mainWindow, int deleteItemCount, int markAsFalsePositiveItemCount)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;
            this.deleteItemCount = deleteItemCount;
            this.markAsFalsePositiveItemCount = markAsFalsePositiveItemCount;

            LoadLocalization();
        }

        private void LoadLocalization()
        {
            yesButton.Content = LocalizationManager.GetString("Dialog.YesButton");
            noButton.Content = LocalizationManager.GetString("Dialog.NoButton");
            Title = LocalizationManager.GetString("Dialog.ApplyTitle");

            warningTextBlock.Text = "";

            if (deleteItemCount > 0)
            {
                warningTextBlock.Text += LocalizationManager.GetString("Dialog.FilesWillBeDeleted", deleteItemCount);
            }

            if (markAsFalsePositiveItemCount > 0)
            {
                warningTextBlock.Text += LocalizationManager.GetString("Dialog.ResultsMarkedFalsePositive", markAsFalsePositiveItemCount);
            }

            warningTextBlock.Text += LocalizationManager.GetString("Dialog.ContinueConfirmation");
        }

        private void yesButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Apply(deleteItemCount, markAsFalsePositiveItemCount);
            Close();
        }

        private void noButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}