using System.Windows;

namespace ImageComparator
{
    /// <summary>
    /// Confirmation dialog for clearing comparison results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This dialog provides three options:
    /// <list type="bullet">
    /// <item><b>Save Results:</b> Saves the current session before clearing</item>
    /// <item><b>Discard Results:</b> Clears results without saving</item>
    /// <item><b>Cancel:</b> Closes dialog without any action</item>
    /// </list>
    /// </para>
    /// <para>
    /// Called when the user attempts to clear results to prevent accidental data loss.
    /// </para>
    /// </remarks>
    public partial class ClearPopupWindow : Window
    {
        MainWindow mainWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearPopupWindow"/> class.
        /// </summary>
        /// <param name="mainWindow">Reference to the main window.</param>
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
