using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AddFiles.Models;

namespace AddFiles
{
    static class Program
    {
        #region variables
        static readonly List<String> files = new List<String>();
        static readonly List<String> tempDirectories = new List<String>();
        static List<String> tempFiles = new List<String>();
        static bool includeSubfolders, jpegFiles, gifFiles, pngFiles, bmpFiles, tiffFiles, icoFiles, gotException = false;
        static String ext, path;
        #endregion variables

        [STAThread]
        static void Main()
        {
            try
            {
                path = Environment.GetCommandLineArgs().ElementAt(0).Substring(0, Environment.GetCommandLineArgs().ElementAt(0).LastIndexOf("\\"));
                ReadFromFile();
                AddFiles();
            }
            catch (Exception ex)
            {
                // Mark that an exception occurred
                gotException = true;
                
                // Try to write error details to a log file for debugging
                try
                {
                    string errorLog = path + @"\AddFiles_Error.log";
                    File.WriteAllText(errorLog, $"[{DateTime.Now}] ERROR in AddFiles.exe\r\n" +
                                                 $"Exception Type: {ex.GetType().FullName}\r\n" +
                                                 $"Message: {ex.Message}\r\n" +
                                                 $"Stack Trace:\r\n{ex.StackTrace}\r\n");
                }
                catch
                {
                    // If we can't write the log, just continue
                }
            }
            finally
            {
                // Always write Results.json, even if there was an error
                WriteToFile();
            }
        }

        private static void AddFiles()
        {
            int i;

            if (includeSubfolders)
            {
                while (tempDirectories.Count != 0)
                {
                    try
                    {
                        tempFiles = new List<String>();
                        Directory.GetFiles(tempDirectories.ElementAt(0)).ToList().ForEach(s => tempFiles.Add(s));

                        for (i = 0; i < tempFiles.Count; i++)
                        {
                            ext = Path.GetExtension(tempFiles.ElementAt(i)).ToLower();

                            if (tempFiles.ElementAt(i).Substring(0, tempFiles.ElementAt(i).LastIndexOf("\\")).Length >= 248 || tempFiles.ElementAt(i).Length >= 260 || !IsSearchedFor())
                            {
                                tempFiles.RemoveAt(i);
                                i--;
                            }
                        }

                        files.AddRange(tempFiles);
                        Directory.GetDirectories(tempDirectories.ElementAt(0)).ToList().ForEach(s => tempDirectories.Add(s));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        gotException = true;
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }
                    tempDirectories.RemoveAt(0);
                }
            }
            else
            {
                for (i = 0; i < tempDirectories.Count; i++)
                {
                    try
                    {
                        Directory.GetFiles(tempDirectories.ElementAt(0)).ToList().ForEach(s => files.Add(s));

                        for (i = 0; i < files.Count; i++)
                        {
                            ext = Path.GetExtension(files.ElementAt(i)).ToLower();

                            if (files.ElementAt(i).Substring(0, files.ElementAt(i).LastIndexOf("\\")).Length >= 248 || files.ElementAt(i).Length >= 260 || !IsSearchedFor())
                            {
                                files.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        gotException = true;
                    }
                }
            }
        }

        private static bool IsSearchedFor()
        {
            if ((jpegFiles && (ext.Equals(".jpg") || ext.Equals(".jpeg"))) || (bmpFiles && ext.Equals(".bmp"))
                || (gifFiles && ext.Equals(".gif")) || (pngFiles && ext.Equals(".png"))
                || (tiffFiles && ext.Equals(".tif")) || (icoFiles && ext.Equals(".ico")))
            {
                return true;
            }

            return false;
        }

        private static void ReadFromFile()
        {
            string directoriesPath = path + @"\Directories.json";
            string filtersPath = path + @"\Filters.json";
            
            try
            {
                // Read Directories.json
                string directoriesJson = File.ReadAllText(directoriesPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var directoriesData = JsonSerializer.Deserialize<DirectoriesData>(directoriesJson, options);
                
                if (directoriesData != null && directoriesData.Directories != null)
                {
                    tempDirectories.AddRange(directoriesData.Directories);
                }

                // Read Filters.json
                string filtersJson = File.ReadAllText(filtersPath);
                var filtersData = JsonSerializer.Deserialize<FiltersData>(filtersJson, options);
                
                if (filtersData != null)
                {
                    includeSubfolders = filtersData.IncludeSubfolders;
                    jpegFiles = filtersData.JpegFiles;
                    gifFiles = filtersData.GifFiles;
                    pngFiles = filtersData.PngFiles;
                    bmpFiles = filtersData.BmpFiles;
                    tiffFiles = filtersData.TiffFiles;
                    icoFiles = filtersData.IcoFiles;
                }
            }
            catch (Exception)
            {
                gotException = true;
            }
            finally
            {
                // Always delete the files, even if there was an error
                try
                {
                    if (File.Exists(directoriesPath))
                    {
                        File.Delete(directoriesPath);
                    }
                }
                catch { }
                
                try
                {
                    if (File.Exists(filtersPath))
                    {
                        File.Delete(filtersPath);
                    }
                }
                catch { }
            }
        }

        private static void WriteToFile()
        {
            // Defensive: If path is null or empty, try to determine it
            if (string.IsNullOrEmpty(path))
            {
                try
                {
                    path = Environment.GetCommandLineArgs().ElementAt(0).Substring(0, Environment.GetCommandLineArgs().ElementAt(0).LastIndexOf("\\"));
                }
                catch
                {
                    path = Directory.GetCurrentDirectory();
                }
            }
            
            try
            {
                var resultsData = new ResultsData
                {
                    GotException = gotException,
                    Files = files
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(resultsData, options);
                File.WriteAllText(path + @"\Results.json", jsonString);
            }
            catch (Exception ex)
            {
                // If serialization fails, try to write a simple error file
                try
                {
                    var resultsData = new ResultsData
                    {
                        GotException = true,
                        Files = new List<string>()
                    };
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(resultsData, options);
                    File.WriteAllText(path + @"\Results.json", jsonString);
                }
                catch
                {
                    // Last resort: write a minimal file
                    try
                    {
                        File.WriteAllText(path + @"\Results.json", "{\"GotException\":true,\"Files\":[]}");
                    }
                    catch
                    {
                        // Nothing more we can do
                    }
                }
            }
        }
    }
}