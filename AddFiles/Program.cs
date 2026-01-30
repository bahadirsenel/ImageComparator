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
            path = Environment.GetCommandLineArgs().ElementAt(0).Substring(0, Environment.GetCommandLineArgs().ElementAt(0).LastIndexOf("\\"));
            ReadFromFile();
            AddFiles();
            WriteToFile();
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
            try
            {
                // Read Directories.json
                string directoriesJson = File.ReadAllText(path + @"\Directories.json");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var directoriesData = JsonSerializer.Deserialize<DirectoriesData>(directoriesJson, options);
                
                if (directoriesData?.Directories != null)
                {
                    tempDirectories.AddRange(directoriesData.Directories);
                }
                
                File.Delete(path + @"\Directories.json");

                // Read Filters.json
                string filtersJson = File.ReadAllText(path + @"\Filters.json");
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
                
                File.Delete(path + @"\Filters.json");
            }
            catch (Exception)
            {
                gotException = true;
            }
        }

        private static void WriteToFile()
        {
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
            catch (Exception)
            {
                // If serialization fails, mark as exception
                gotException = true;
                var resultsData = new ResultsData
                {
                    GotException = true,
                    Files = new List<string>()
                };
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(resultsData, options);
                File.WriteAllText(path + @"\Results.json", jsonString);
            }
        }
    }
}