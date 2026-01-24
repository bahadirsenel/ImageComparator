using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AddFiles
{
    static class Program
    {
        #region variables
        static List<String> tempDirectories = new List<String>();
        static List<String> tempFiles = new List<String>();
        static List<String> files = new List<String>();
        static bool includeSubfolders, jpegFiles, gifFiles, pngFiles, bmpFiles, tiffFiles, icoFiles, gotException = false;
        static StreamWriter streamWriter;
        static StreamReader streamReader;
        static int count;
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
                    catch (UnauthorizedAccessException ex)
                    {
                        gotException = true;
                    }
                    catch (DirectoryNotFoundException ex)
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
                    catch (UnauthorizedAccessException ex)
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
            streamReader = new StreamReader(path + @"\Directories.imc");
            count = int.Parse(streamReader.ReadLine());

            for (int i = 0; i < count; i++)
            {
                tempDirectories.Add(streamReader.ReadLine());
            }

            streamReader.Close();
            File.Delete(path + @"\Directories.imc");

            streamReader = new StreamReader(path + @"\Filters.imc");
            includeSubfolders = bool.Parse(streamReader.ReadLine());
            jpegFiles = bool.Parse(streamReader.ReadLine());
            gifFiles = bool.Parse(streamReader.ReadLine());
            pngFiles = bool.Parse(streamReader.ReadLine());
            bmpFiles = bool.Parse(streamReader.ReadLine());
            tiffFiles = bool.Parse(streamReader.ReadLine());
            icoFiles = bool.Parse(streamReader.ReadLine());
            streamReader.Close();
            File.Delete(path + @"\Filters.imc");
        }

        private static void WriteToFile()
        {
            streamWriter = new StreamWriter(path + @"\Results.imc");
            streamWriter.WriteLine(gotException);
            streamWriter.WriteLine(files.Count);

            for (int i = 0; i < files.Count; i++)
            {
                streamWriter.WriteLine(files.ElementAt(i));
            }

            streamWriter.Close();
        }
    }
}