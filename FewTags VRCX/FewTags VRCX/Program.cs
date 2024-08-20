using DiscordRPC;
using System.Text;
using FewTags.VRCX;
using Newtonsoft.Json;
using FewTags.VRCX.IPC;
using DiscordRPC.Logging;
using BuildSoft.VRChat.Osc.Chatbox;
using System.Text.RegularExpressions;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Media;

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
            Console.Title = $"{Config.ApplicationName} v{Config.Version}";
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Config.Check();
            if (File.Exists(Config.Configuration))
            {
                Configure();
            }
            else if (!File.Exists(Config.Configuration))
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
                await OutputWatcher.ScanLog();
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
                            Discord.UpdateState($"Tags: {Config.Fewdys}");
                            Thread.Sleep(10000);
                        }
                    }
                    catch
                    {
                        Config.RPC = false;
                    }
                }).Start();
            }
            while (true) {
                Thread.Sleep(10);
            };
        }
        // End \\

        // Functions \\
        public static void HandleJoin(string Search = null)
        {
            if (Config.InternalRawTags.Contains(Search))
            {
                Config.Tags[] TagsArray = Config.InternalTags.Records.Where(User => User.UserID == Search).ToArray();
                ParseTags(TagsArray, Config.Status.VRCX);
            }
            else if (!Config.InternalRawTags.Contains(Search))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[FewTags] ({Search}) Has No Tags");
            }
        }

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

        public static async Task UpdateTags()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Fetching Tags...");
                using (HttpClient Https = new HttpClient())
                {
                    Config.InternalRawTags = await Https.GetStringAsync(Config.InternalTagsEndPoint);
                    Config.ExternalRawTags = await Https.GetStringAsync(Config.ExternalTagsEndPoint);
                    if (!string.IsNullOrEmpty(Config.ExternalRawTags))
                    {
                        Config.InternalTags = JsonConvert.DeserializeObject<Config.Tags>(Config.InternalRawTags);
                        Config.ExternalTags = JsonConvert.DeserializeObject<Config.Tags>(Config.ExternalRawTags);
                    }
                    else if (string.IsNullOrEmpty(Config.ExternalRawTags))
                    {
                        Console.WriteLine("Failed To Fetch Extnernal Tags: Response Is Null Or Empty");
                        Thread.Sleep(1000);
                        Environment.Exit(1);
                    }
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("Finished Fetching Tags");
                    Console.ResetColor();
                    Console.WriteLine("Please Note: Colors May Not Be Correct Due To Limitations Of Console");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An Error Occurred While Updating Tags: {ex.Message}");
            }
        }

        public static void ParseTags(Config.Tags[] TagsArray, Config.Status Status = Config.Status.Myself)
        {
            Console.WriteLine();
            Config.Tags Tag = TagsArray.LastOrDefault();
            Config.Tags InternalTag = Config.InternalTags.Records.Where(User => User.UserID == Tag.UserID).LastOrDefault();
            Config.Tags ExternalTag = Config.ExternalTags.Records.Where(User => User.UserID == Tag.UserID).LastOrDefault();

            if (Tag.Active == true)
            {
                string[] Tags = Tag.Tag;
                string UserID = Tag.UserID;
                string ID = Tag.ID.ToString();
                string PlateBigText = Tag.PlateBigText;
                string Malicious = Tag.Malicious.ToString();
                string DisplayName = ExternalTag.DisplayName;
                string User = (!string.IsNullOrEmpty(DisplayName)) ? DisplayName : (!string.IsNullOrEmpty(UserID)) ? UserID : "Unknown";

                Config.Fewdys++;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] [FewTags] ({User}) {Status} With Tags");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[FewTags] UserID: {UserID}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FewTags] Malicious: {Malicious}");
                Console.ResetColor();

                var Message = new StringBuilder();
                Message.AppendLine($"[FewTags {Status}]").AppendLine($"Name: {User}").AppendLine($"Malicious: {Malicious}").AppendLine("Tags:");
                if (Tag.BigTextActive && !string.IsNullOrEmpty(PlateBigText))
                {
                    string ProcessedPlateText = Regex.Replace(PlateBigText, @"<\/?b>|<\/?i>|<\/?color>|<color=[^>]*>", "");
                    ColorConsole.Print(ProcessedPlateText);
                    Message.AppendLine(ProcessedPlateText);
                }
                if (Tags != null && Tags.Length > 0)
                {
                    foreach (var RegexTag in Tags)
                    {
                        string ProcessedTag = Regex.Replace(RegexTag, @"<\/?b>|<\/?i>|<\/?color>|<color=[^>]*>", "");
                        ColorConsole.Print(ProcessedTag);
                        Message.AppendLine(ProcessedTag);
                    }
                }
                else if (Tags == null || Tags.Length < 1)
                {
                    Message.AppendLine("None");
                    Console.WriteLine("[FewTags] No Tags");
                }

                if (Config.OSC)
                {
                    OscChatbox.SendMessage(Message.ToString() + Config.Blank, direct: true, complete: false);
                }
                if (Config.ToastNotifications)
                {
                    new ToastContentBuilder().AddText(Message.ToString()).SetToastDuration((ToastDuration)1).AddAppLogoOverride(new Uri(Config.NotificationIcon), ToastGenericAppLogoCrop.Default).Show();
                }
            }
            Console.ResetColor();
            Console.WriteLine();
        }
        // End \\
    }
}