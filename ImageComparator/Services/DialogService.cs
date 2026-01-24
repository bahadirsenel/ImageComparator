using Ookii.Dialogs.Wpf;
using System;

namespace ImageComparator.Services
{
    /// <summary>
    /// Service for displaying file and folder dialogs.
    /// Wraps Ookii.Dialogs for easier testing and abstraction.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Shows a folder browser dialog.
        /// </summary>
        /// <param name="description">The description to show in the dialog.</param>
        /// <returns>The selected folder path, or null if cancelled.</returns>
        string ShowFolderBrowserDialog(string description);

        /// <summary>
        /// Shows a save file dialog.
        /// </summary>
        /// <param name="filter">The file filter (e.g., "*.mff|*.mff").</param>
        /// <param name="defaultExt">The default file extension.</param>
        /// <returns>The selected file path, or null if cancelled.</returns>
        string ShowSaveFileDialog(string filter, string defaultExt);

        /// <summary>
        /// Shows an open file dialog.
        /// </summary>
        /// <param name="filter">The file filter (e.g., "*.mff|*.mff").</param>
        /// <param name="defaultExt">The default file extension.</param>
        /// <returns>The selected file path, or null if cancelled.</returns>
        string ShowOpenFileDialog(string filter, string defaultExt);
    }

    /// <summary>
    /// Implementation of IDialogService using Ookii.Dialogs.
    /// </summary>
    public class DialogService : IDialogService
    {
        public string ShowFolderBrowserDialog(string description)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = description,
                RootFolder = Environment.SpecialFolder.MyPictures,
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            return dialog.ShowDialog() == true ? dialog.SelectedPath : null;
        }

        public string ShowSaveFileDialog(string filter, string defaultExt)
        {
            var dialog = new VistaSaveFileDialog
            {
                Filter = filter,
                DefaultExt = defaultExt,
                AddExtension = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string ShowOpenFileDialog(string filter, string defaultExt)
        {
            var dialog = new VistaOpenFileDialog
            {
                Filter = filter,
                DefaultExt = defaultExt,
                AddExtension = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
