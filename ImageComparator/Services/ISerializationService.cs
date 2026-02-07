using ImageComparator.Models;

namespace ImageComparator.Services
{
    /// <summary>
    /// Service for serializing and deserializing application state
    /// </summary>
    public interface ISerializationService
    {
        /// <summary>
        /// Serialize application state to a file
        /// </summary>
        /// <param name="filePath">Path to save the file</param>
        /// <param name="settings">Application settings to serialize</param>
        void Serialize(string filePath, AppSettings settings);

        /// <summary>
        /// Deserialize application state from a file
        /// </summary>
        /// <param name="filePath">Path to the file to load</param>
        /// <returns>Deserialized application settings</returns>
        AppSettings Deserialize(string filePath);
    }
}
