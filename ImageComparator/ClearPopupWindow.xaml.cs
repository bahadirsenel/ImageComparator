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

            if (mainWindow.isEnglish)
            {
                saveResultsButton.Content = "Save";
                discardResultsButton.Content = "Discard";
                cancelResultsButton.Content = "Cancel";
                warningTextBlock.Text = "Are you sure you want to discard current results? You can save them to continue working on them later.";
                Title = "Warning";
            }
            else
            {
                saveResultsButton.Content = "Kaydet";
                discardResultsButton.Content = "Temizle";
                cancelResultsButton.Content = "İptal";
                warningTextBlock.Text = "Sonuçları temizlemek istediğinize emin misiniz? Sonuçlarınızı kaydedip daha sonra üzerlerinde çalışmaya devam edebilirsiniz.";
                Title = "Uyarı";
            }
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
