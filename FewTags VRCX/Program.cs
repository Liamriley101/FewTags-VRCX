using DiscordRPC;
using System.Text;
using Funeral_PMT;
using FewTags.VRCX;
using Newtonsoft.Json;
using FewTags.VRCX.IPC;
using DiscordRPC.Logging;
using Fewtags_VRCX.Properties;
using BuildSoft.VRChat.Osc.Chatbox;
using System.Text.RegularExpressions;
using Microsoft.Toolkit.Uwp.Notifications;

namespace FewTags
{
    internal class Program
    {
        // Classes \\
        private static DiscordRpcClient Discord;
        private static readonly IPCClient IpcClient = new IPCClient();
        private static readonly IPCClientReceive IpcClientRec = new IPCClientReceive();
        // End \\

        // Console \\
        private static async Task Main()
        {
            Console.Title = $"{Config.ApplicationName} v{Config.Version} | Tags: 0";
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            HTCC.EnableVirtualTerminal();

            Config.Check();
            if (File.Exists(Config.Configuration) == true)
            {
                Configure();
            }
            else if (File.Exists(Config.Configuration) == false)
            {
                UpdateConfig();
            }
            while (true)
            {
                Console.WriteLine(@$"Press 1 To Toggle OSC
Press 2 To Toggle RPC
Press 3 To Toggle Toast Notifications
Press Enter To Confirm
                    
OSC: {(Config.OSC ? "Enabled" : "Disabled")}
RPC: {(Config.RPC ? "Enabled" : "Disabled")}
Toast Notifications: {(Config.ToastNotifications ? "Enabled" : "Disabled")}
                ");

                ConsoleKeyInfo KeyInfo = Console.ReadKey(true);
                Console.Clear();
                if (KeyInfo.Key == ConsoleKey.D1)
                {
                    Config.OSC = !Config.OSC;
                }
                else if (KeyInfo.Key == ConsoleKey.D2)
                {
                    Config.RPC = !Config.RPC;
                }
                else if (KeyInfo.Key == ConsoleKey.D3)
                {
                    Config.ToastNotifications = !Config.ToastNotifications;
                }
                else if (KeyInfo.Key == ConsoleKey.Enter)
                {
                    break;
                }
            }
            UpdateConfig();
            await UpdateTags();

            IpcClient.Connect();
            IpcClientRec.Connect();
            new Thread(async delegate ()
            {
                await OutputWorker.ScanLog();
            }).Start();
            if (Config.RPC == true)
            {
                new Thread(delegate ()
                {
                    try
                    {
                        RichPresence("FewTags VRCX", "Tags: 0");
                        while (Config.RPC == true)
                        {
                            Discord.UpdateState($"Tags: {Config.Tagged}");
                            Thread.Sleep(10000);
                        }
                    }
                    catch
                    {
                        Config.RPC = false;
                    }
                }).Start();
            }
            while (true)
            {
                Console.Title = $"{Config.ApplicationName} v{Config.Version} | Tags: {Config.Tagged}";
                Thread.Sleep(100);
            };
        }
        // End \\

        // Functions \\
        private static void Configure()
        {
            string Settings = File.ReadAllText(Config.Configuration);
            Config.Configurator Configurate = JsonConvert.DeserializeObject<Config.Configurator>(Settings);
            Config.OSC = Configurate.OSC;
            Config.RPC = Configurate.RPC;
            Config.ToastNotifications = Configurate.ToastNotifications;
        }

        public static void UpdateConfig()
        {
            Config.Configurator Configure = new Config.Configurator
            {
                OSC = Config.OSC,
                RPC = Config.RPC,
                ToastNotifications = Config.ToastNotifications
            };
            string Configurate = System.Text.Json.JsonSerializer.Serialize(Configure);
            File.WriteAllText("Config.json", Configurate);
        }

        private static void RichPresence(string Details, string State)
        {
            Discord = new DiscordRpcClient("1270729273979830354");
            Discord.Logger = new ConsoleLogger
            {
                Level = LogLevel.Warning
            };
            Discord.Initialize();
            Discord.SetPresence(new RichPresence
            {
                Details = Details,
                State = State,
                Timestamps = Timestamps.Now,
                Assets = new Assets
                {
                    LargeImageKey = "icon"
                }
            });
        }

        public static void HandleIPC(string Search)
        {
            Config.Tags[] TagsArray = Config.InternalTags.Records.Any(User => User.UserID == Search) ? Config.InternalTags.Records.Where(User => User.UserID == Search).ToArray() : Config.ExternalTags.Records.Any(User => User.UserID == Search) ? Config.ExternalTags.Records.Where(User => User.UserID == Search).ToArray() : null;
            if (TagsArray != null)
            {
                ParseTags(Config.Status.VRCX, UserID: Search);
            }
            else if (TagsArray == null)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[FewTags {Config.Status.VRCX}] {Search} Has No Tags");
                Console.ResetColor();
            }
        }

        public static async Task UpdateTags()
        {
            Config.InternalTags = new Config.Tags();
            Config.ExternalTags = new Config.Tags();
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("[FewTags] Fetching Tags");
                Console.ForegroundColor = ConsoleColor.Yellow;
                foreach (FileInfo Tag in Config.Internal.GetFiles())
                {
                    string Tags = File.ReadAllText(Tag.FullName);
                    Config.InternalTags += JsonConvert.DeserializeObject<Config.Tags>(Tags);
                    Console.WriteLine($"Added Custom Internal Tags: {Tag.Name}");
                }
                foreach (FileInfo Tag in Config.External.GetFiles())
                {
                    string Tags = File.ReadAllText(Tag.FullName);
                    Config.ExternalTags += JsonConvert.DeserializeObject<Config.Tags>(Tags);
                    Console.WriteLine($"Added Custom External Tags: {Tag.Name}");
                }

                using (HttpClient Https = new HttpClient())
                {
                    string InternalRawTags = await Https.GetStringAsync(Config.InternalTagsEndPoint);
                    string ExternalRawTags = await Https.GetStringAsync(Config.ExternalTagsEndPoint);
                    if (string.IsNullOrEmpty(ExternalRawTags) == false && string.IsNullOrEmpty(InternalRawTags) == false)
                    {
                        Config.InternalTags += JsonConvert.DeserializeObject<Config.Tags>(InternalRawTags);
                        Config.ExternalTags += JsonConvert.DeserializeObject<Config.Tags>(ExternalRawTags);
                    }
                    else if (string.IsNullOrEmpty(ExternalRawTags) == true || string.IsNullOrEmpty(InternalRawTags) == true)
                    {
                        Console.WriteLine("Failed To Fetch Extnernal Tags: Response Is Null Or Empty");
                        Console.ReadLine();
                        Environment.Exit(1);
                    }
                }

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("[FewTags] Fetched Tags");
                Console.WriteLine($"Internal Tags: {Config.InternalTags.Records.Count}");
                Console.WriteLine($"External Tags: {Config.ExternalTags.Records.Count}");
                Console.ResetColor();
                Console.WriteLine("Please Note: Colors May Not Be Correct Due To Limitations Of Console");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An Error Occurred While Updating Tags: {ex.Message}");
            }
        }

        public static void ParseTags(Config.Status Status = Config.Status.Myself, string DisplayName = null, string UserID = null)
        {
            Console.WriteLine();

            Config.Tags InternalTag = Config.InternalTags.Records.Where(User => User.UserID == UserID).LastOrDefault();
            Config.Tags ExternalTag = Config.ExternalTags.Records.Where(User => User.DisplayName == DisplayName).LastOrDefault();
            if (InternalTag != null || ExternalTag != null)
            {
                if (InternalTag?.Active == true || ExternalTag?.Active == true)
                {
                    string[] Tags = InternalTag != null ? InternalTag.Tag : ExternalTag != null ? ExternalTag.Tag : null;
                    if (Tags != null && Tags.Length > 0)
                    {
                        Config.Tagged++;
                        UserID = string.IsNullOrEmpty(UserID) == false ? UserID : InternalTag != null ? InternalTag.UserID : ExternalTag != null ? ExternalTag.UserID : "Unknown";
                        DisplayName = string.IsNullOrEmpty(DisplayName) == false ? DisplayName : ExternalTag != null ? ExternalTag.DisplayName : "Unknown";
                        string User = string.IsNullOrEmpty(DisplayName) == false ? $"Name: {DisplayName}" : $"UserID: {UserID}";
                        string PlateBigText = InternalTag != null ? InternalTag.PlateBigText : ExternalTag != null ? ExternalTag.PlateBigText : null;
                        bool BigTextActive = InternalTag != null ? InternalTag.BigTextActive : ExternalTag != null ? ExternalTag.BigTextActive : false;
                        string Malicious = InternalTag != null ? InternalTag.Malicious.ToString() : ExternalTag != null ? ExternalTag.Malicious.ToString() : null;

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] [FewTags {Status}] ({User}) Has Tags");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"[FewTags] UserID: {UserID}");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[FewTags] Malicious: {Malicious}");
                        Console.ResetColor();
                        Console.WriteLine($"[FewTags] Tags");

                        var Message = new StringBuilder();
                        Message.AppendLine($"[FewTags {Status}]").AppendLine(User).AppendLine($"Malicious: {Malicious}").AppendLine("Tags:");
                        if (BigTextActive && string.IsNullOrEmpty(PlateBigText) == false)
                        {
                            string ProcessedTag = Regex.Replace(PlateBigText, @"<\/?b>|<\/?i>|</color>", "");
                            ColorConsole.Print(ProcessedTag);
                            ProcessedTag = Regex.Replace(ProcessedTag, @"<\/?b>|<\/?i>|<\/?color>|<color=[^>]*>", "");
                            Message.AppendLine(ProcessedTag);
                        }
                        if (Tags != null && Tags.Length > 0)
                        {
                            foreach (var RegexTag in Tags)
                            {
                                string ProcessedTag = Regex.Replace(RegexTag, @"<\/?b>|<\/?i>|</color>", "");
                                ColorConsole.Print(ProcessedTag);
                                ProcessedTag = Regex.Replace(ProcessedTag, @"<\/?b>|<\/?i>|<\/?color>|<color=[^>]*>", "");
                                Message.AppendLine(ProcessedTag);
                            }
                        }
                        else if (Tags == null || Tags.Length < 1)
                        {
                            Message.AppendLine("None");
                            Console.WriteLine("[FewTags] No Tags");
                        }

                        if (Config.OSC == true)
                        {
                            OscChatbox.SendMessage(Message.ToString() + Config.Blank, direct: true, complete: false);
                        }
                        if (Config.ToastNotifications == true)
                        {
                            new ToastContentBuilder().AddText(Message.ToString()).SetToastDuration((ToastDuration)1).AddAppLogoOverride(new Uri(Config.NotificationIcon), ToastGenericAppLogoCrop.Default).Show();
                        }
                    }
                }
                else if (InternalTag?.Tag == null && ExternalTag?.Tag == null)
                {
                    Console.WriteLine("[FewTags] No Tags");
                }
            }
            else if (InternalTag == null && ExternalTag == null)
            {
                Console.WriteLine("[FewTags] No Tags");
            }
            Console.ResetColor();
            Console.WriteLine();
        }
        // End \\
    }
}