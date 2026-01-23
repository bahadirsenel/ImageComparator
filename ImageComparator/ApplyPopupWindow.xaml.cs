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

            if (mainWindow.isEnglish)
            {
                yesButton.Content = "Yes";
                noButton.Content = "No";
                warningTextBlock.Text = "";

                if (deleteItemCount > 0)
                {
                    warningTextBlock.Text += deleteItemCount + " file(s) will be deleted.\r\n";
                }

                if (markAsFalsePositiveItemCount > 0)
                {
                    warningTextBlock.Text += markAsFalsePositiveItemCount + " result(s) will be marked as false positive and won't be shown in future results.\r\n";
                }

                warningTextBlock.Text += "Do you want to continue?";
                Title = "Warning";
            }
            else
            {
                yesButton.Content = "Evet";
                noButton.Content = "Hayır";
                warningTextBlock.Text = "";

                if (deleteItemCount > 0)
                {
                    warningTextBlock.Text += deleteItemCount + " dosya silinecek.\r\n";
                }

                if (markAsFalsePositiveItemCount > 0)
                {
                    warningTextBlock.Text += markAsFalsePositiveItemCount + " sonuç hatalı olarak işaretlenecek ve daha sonraki aramalarınızda gösterilmeyecek.\r\n";
                }

                warningTextBlock.Text += "Devam etmek istiyor musunuz?";
                Title = "Uyarı";
            }
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