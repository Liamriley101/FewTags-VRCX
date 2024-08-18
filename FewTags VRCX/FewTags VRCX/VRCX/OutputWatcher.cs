using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FewTags.VRCX
{
    public class OutputWatcher
    {
        // Functions \\
        public static async Task ScanLog()
        {
            try
            {
                while (true)
                {
                    Process[] Processes = Process.GetProcessesByName("VRChat");
                    if (Processes != null && Processes.Length != 0)
                    {
                        Config.VRCCheck = true;
                        Process VRChat = Processes[0];
                        DirectoryInfo DirectoryInfo = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat");
                        if (DirectoryInfo != null && DirectoryInfo.Exists)
                        {
                            FileInfo FileInfo = null;
                            foreach (FileInfo File in DirectoryInfo.GetFiles("output_log_*.txt", SearchOption.TopDirectoryOnly))
                            {
                                if (FileInfo == null || File.LastWriteTime.CompareTo(FileInfo.LastWriteTime) >= 0)
                                {
                                    try
                                    {
                                        File.Delete();
                                    }
                                    catch
                                    {
                                        FileInfo = File;
                                    }
                                }
                            }
                            if (FileInfo != null)
                            {
                                ReadNewLines(FileInfo.FullName);
                                while (VRChat.HasExited == false)
                                {
                                    await ReadLog(FileInfo.FullName);
                                    Thread.Sleep(1000);
                                }
                            }
                        }
                    }
                    else if (Processes == null || Processes.Length == 0)
                    {
                        if (Config.VRCCheck == true)
                        {
                            Config.VRCCheck = false;
                            Console.WriteLine("VRChat Isn't Running");
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An Error Occurred While Scanning Log: {ex.Message}");
            }
        }

        private static List<string> ReadNewLines(string FilePath)
        {
            List<string> Lines = new();
            try
            {
                if (FilePath == null)
                {
                    Console.WriteLine("File Path Is Null");
                }
                using (FileStream FileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader StreamReader = new StreamReader(FileStream))
                    {
                        string Line = null;
                        StreamReader.BaseStream.Seek(Config.LastReadOffset, SeekOrigin.Begin);
                        while ((Line = StreamReader.ReadLine()) != null)
                        {
                            if (Line.Contains("User Authenticated:"))
                            {
                                string Pattern = "Authenticated:\\s+(.*?)\\s+\\(";
                                Match Match = Regex.Match(Line, Pattern);
                                if (Match.Success == true)
                                {
                                    string DisplayName = Match.Groups[1].Value;
                                    if (Config.ExternalRawTags.Contains(DisplayName))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"[FewTags] Welcome {DisplayName}");
                                        Console.ForegroundColor = ConsoleColor.Magenta;
                                        Config.Tags[] TagsArray = Config.SetExternalTags.Records.Where(User => User.DisplayName == DisplayName).ToArray();
                                        Program.ParseTags(TagsArray);
                                    }
                                    else if (!Config.ExternalRawTags.Contains(DisplayName))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"[FewTags] Welcome {DisplayName}");
                                        Console.ForegroundColor = ConsoleColor.Magenta;
                                        Console.WriteLine($"[FewTags] {DisplayName} Was Not Found In The Database (No Tags)");
                                    }
                                }
                            }
                            Lines.Add(Line);
                        }
                        Config.LastReadOffset = StreamReader.BaseStream.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An Error Occurred While Reading New Lines: {ex.Message}");
            }
            return Lines;
        }

        private static async Task ReadLog(string Path)
        {
            var Lines = ReadNewLines(Path);
            try
            {
                foreach (var Line in Lines)
                {
                    if (Line.Contains("OnPlayerJoined "))
                    {
                        string[] Parts = Line.Split(new[] { "OnPlayerJoined " }, StringSplitOptions.None);
                        string DisplayName = Parts[1].Trim();
                        if (Config.ExternalRawTags.Contains(DisplayName))
                        {
                            Config.Tags[] TagsArray = Config.SetExternalTags.Records.Where(User => User.DisplayName == DisplayName).ToArray();
                            Program.ParseTags(TagsArray, Config.Status.Joined);
                        }
                        else if (!Config.ExternalRawTags.Contains(DisplayName))
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine($"[FewTags] {DisplayName} Joined With No Tags");
                        }
                    }
                    if (Line.Contains("OnPlayerLeft "))
                    {
                        string[] Parts = Line.Split(new[] { "OnPlayerLeft " }, StringSplitOptions.None);
                        string DisplayName = Parts[1].Trim();
                        if (Config.ExternalRawTags.Contains(DisplayName))
                        {
                            Config.Tags[] TagsArray = Config.SetExternalTags.Records.Where(User => User.DisplayName == DisplayName).ToArray();
                            Program.ParseTags(TagsArray, Config.Status.Left);
                        }
                        else if (!Config.ExternalRawTags.Contains(DisplayName))
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine($"[FewTags] {DisplayName} Left With No Tags");
                        }
                    }
                    if (Line.Contains("OnLeftRoom"))
                    {
                        Console.Clear();
                        await Program.UpdateTags();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An Error Occurred While Reading Log: {ex.Message}");
            }
        }
        // End \\
    }
}