using ImageComparator.Services;
using ImageComparator.ViewModels;

namespace ImageComparator
{
    /// <summary>
    /// Provides ViewModels for Views with dependency injection.
    /// This class acts as a service locator pattern for ViewModel instantiation.
    /// </summary>
    public class ViewModelLocator
    {
        private static ViewModelLocator _instance;

        /// <summary>
        /// Gets the singleton instance of the ViewModelLocator.
        /// </summary>
        public static ViewModelLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ViewModelLocator();
                }
                return _instance;
            }
        }

        // Services
        private readonly IDialogService _dialogService;
        private readonly IFileService _fileService;

        // ViewModels
        private MainWindowViewModel _mainWindowViewModel;

        private ViewModelLocator()
        {
            // Initialize services
            _dialogService = new DialogService();
            _fileService = new FileService();
        }

        /// <summary>
        /// Gets the MainWindowViewModel instance.
        /// </summary>
        public MainWindowViewModel MainWindowViewModel
        {
            get
            {
                if (_mainWindowViewModel == null)
                {
                    _mainWindowViewModel = new MainWindowViewModel(_dialogService, _fileService);
                }
                return _mainWindowViewModel;
            }
        }

        /// <summary>
        /// Cleans up all ViewModels and resources.
        /// </summary>
        public static void Cleanup()
        {
            // Add cleanup logic if needed
        }
    }
}
