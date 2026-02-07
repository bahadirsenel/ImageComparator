using Common.Helpers;
using ImageComparator.Models;
using System;
using System.IO;
using System.Text.Json;

namespace ImageComparator.Services
{
    /// <summary>
    /// Service for serializing and deserializing application state
    /// </summary>
    public class SerializationService : ISerializationService
    {
        /// <summary>
        /// Serialize application state to a file
        /// </summary>
        public void Serialize(string filePath, AppSettings settings)
        {
            try
            {
                // Get and create the folder path
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };

                string jsonString = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(filePath, jsonString);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                ErrorLogger.LogError("SerializationService.Serialize", ex);
                throw;
            }
        }

        /// <summary>
        /// Deserialize application state from a file
        /// </summary>
        public AppSettings Deserialize(string filePath)
        {
            try
            {
                // If file doesn't exist, return null
                if (!File.Exists(filePath))
                {
                    return null;
                }

                string jsonString = File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var settings = JsonSerializer.Deserialize<AppSettings>(jsonString, options);

                if (settings == null)
                {
                    throw new InvalidOperationException("Failed to deserialize settings");
                }

                // Validate settings version for forward/backward compatibility
                const int SupportedVersion = 1;
                // Treat 0 or missing version as SupportedVersion to avoid breaking older files
                var loadedVersion = settings.Version;
                if (loadedVersion != 0 && loadedVersion != SupportedVersion)
                {
                    throw new NotSupportedException(
                        $"Unsupported settings version: {loadedVersion}. Supported version: {SupportedVersion}.");
                }

                // Ensure collections are not null
                settings.Files = settings.Files ?? new System.Collections.Generic.List<string>();
                settings.FalsePositiveList1 = settings.FalsePositiveList1 ?? new System.Collections.Generic.List<string>();
                settings.FalsePositiveList2 = settings.FalsePositiveList2 ?? new System.Collections.Generic.List<string>();
                settings.BindingList1 = settings.BindingList1 ?? new System.Collections.Generic.List<SerializableListViewDataItem>();
                settings.BindingList2 = settings.BindingList2 ?? new System.Collections.Generic.List<SerializableListViewDataItem>();
                settings.ConsoleMessages = settings.ConsoleMessages ?? new System.Collections.Generic.List<string>();

                return settings;
            }
            catch (JsonException ex)
            {
                ErrorLogger.LogError("SerializationService.Deserialize - JSON Parse Error", ex);
                throw new InvalidOperationException("The session file is corrupted or invalid.", ex);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                ErrorLogger.LogError("SerializationService.Deserialize", ex);
                throw new InvalidOperationException("Failed to load session file.", ex);
            }
        }
    }
}
