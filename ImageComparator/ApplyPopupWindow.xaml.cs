using System.Windows;

namespace ImageComparator
{
    /// <summary>
    /// Confirmation dialog for applying pending operations (deletions and false positive markings).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This dialog displays:
    /// <list type="bullet">
    /// <item>Number of files marked for deletion</item>
    /// <item>Number of results marked as false positives</item>
    /// <item>Confirmation prompt before executing operations</item>
    /// </list>
    /// </para>
    /// <para>
    /// If the user confirms, calls <see cref="MainWindow.Apply"/> to execute the operations.
    /// </para>
    /// </remarks>
    public partial class ApplyPopupWindow : Window
    {
        MainWindow mainWindow;
        int deleteItemCount, markAsFalsePositiveItemCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyPopupWindow"/> class.
        /// </summary>
        /// <param name="mainWindow">Reference to the main window.</param>
        /// <param name="deleteItemCount">Number of items marked for deletion.</param>
        /// <param name="markAsFalsePositiveItemCount">Number of items marked as false positives.</param>
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