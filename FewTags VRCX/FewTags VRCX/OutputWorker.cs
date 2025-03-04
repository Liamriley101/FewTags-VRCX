using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FewTags.VRCX
{
    public class OutputWorker
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
                                    Config.Tags[] TagsArray = Config.ExternalTags.Records.Where(User => User.DisplayName == DisplayName).ToArray();
                                    if (TagsArray != null)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"[FewTags] Welcome {DisplayName}");
                                        Console.ForegroundColor = ConsoleColor.Magenta;
                                        Program.ParseTags(TagsArray);
                                    }
                                    else if (TagsArray == null)
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
            foreach (var Line in Lines)
            {
                string State = Line.Contains("OnPlayerJoined ") ? "OnPlayerJoined " : Line.Contains("OnPlayerLeft ") ? "OnPlayerLeft" : null;
                if (string.IsNullOrEmpty(State) == false)
                {
                    Config.Status Status = State.Contains("Joined") ? Config.Status.Joined : State.Contains("Left") ? Config.Status.Left : Config.Status.Unknown;
                    string Parts = Line.Split(new[] { State }, StringSplitOptions.None)[1].Trim();
                    string UserID = Parts.Split(new char[] { '(', ')' }, StringSplitOptions.None)[1];
                    string DisplayName = Parts.Split(" ", StringSplitOptions.None)[0];
                    Config.Tags[] TagsArray = Config.ExternalTags.Records.Where(User => User.UserID == UserID).ToArray();
                    if (TagsArray.LastOrDefault() != null)
                    {
                        Program.ParseTags(TagsArray, Status);
                    }
                    if (TagsArray.LastOrDefault() == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"[FewTags] {DisplayName} {Status} With No Tags");
                        Console.ResetColor();
                    }
                }
                if (Line.Contains("OnConnected"))
                {
                    Console.ResetColor();
                    Console.Clear();
                    await Program.UpdateTags();
                }
            }
        }
        // End \\
    }
}