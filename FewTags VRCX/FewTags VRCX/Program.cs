﻿using DiscordRPC;
using System.Text;
using FewTags.VRCX;
using Newtonsoft.Json;
using FewTags.VRCX.IPC;
using DiscordRPC.Logging;
using BuildSoft.VRChat.Osc.Chatbox;
using System.Text.RegularExpressions;

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
                Config.Tags[] TagsArray = Config.SetInternalTags.Records.Where(User => User.UserID == Search).ToArray();
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
                        Config.SetInternalTags = JsonConvert.DeserializeObject<Config.Tags>(Config.InternalRawTags);
                        Config.SetExternalTags = JsonConvert.DeserializeObject<Config.Tags>(Config.ExternalRawTags);
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
            foreach (var ExternalTagArray in TagsArray)
            {
                Config.Tags[] InternalTagsArray = Config.SetInternalTags.Records.Where(User => User.UserID == ExternalTagArray.UserID).ToArray();
                foreach (var TagArray in InternalTagsArray)
                {
                    string[] Tag = TagArray.Tag;
                    string UserID = TagArray.UserID;
                    string ID = TagArray.ID.ToString();
                    string PlateBigText = TagArray.PlateBigText;
                    string Malicious = TagArray.Malicious.ToString();
                    string DisplayName = ExternalTagArray.DisplayName;
                    string User = (Status == Config.Status.VRCX) ? UserID : DisplayName;
                    if (TagArray.Active == true)
                    {
                        string Message = null;
                        Config.Fewdys++;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] [FewTags] ({User}) {Status.ToString()} With Tags");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"[FewTags] UserID: {UserID}");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[FewTags] Malicious: {Malicious}");
                        Console.ResetColor();
                        Message += $"[FewTags {Status.ToString()}]\n";
                        Message += $"Name: {User}\n";
                        Message += $"Malicious: {Malicious}\n";
                        Message += $"Tags:\n";
                        if (TagArray.BigTextActive == true && PlateBigText != null)
                        {
                            // Replace <b>, <i>, </b>, </i> with empty strings
                            string ProcessedTag = Regex.Replace(PlateBigText, @"<\/?b>|<\/?i>|</color>", "");
                            ColorConsole.Print(ProcessedTag);
                            ProcessedTag = Regex.Replace(ProcessedTag, @"<\/?b>|<\/?i>|<\/?color>|<color=[^>]*>", "");
                            Message += $"{ProcessedTag}\n";
                        }
                        if (Tag != null && Tag.Length > 0)
                        {
                            foreach (var RegexTag in Tag)
                            {
                                // Replace <b>, <i>, </b>, </i> with empty strings
                                string ProcessedTag = Regex.Replace(RegexTag, @"<\/?b>|<\/?i>|</color>", "");
                                ColorConsole.Print(ProcessedTag);
                                ProcessedTag = Regex.Replace(RegexTag, @"<\/?b>|<\/?i>|<\/?color>|<color=[^>]*>", "");
                                Message += $"{ProcessedTag}\n";
                            }
                        }
                        else if (Tag == null || Tag.Length < 1)
                        {
                            Message += $"None\n";
                            Console.WriteLine("[FewTags] No Tags");
                        }
                        if (Config.OSC == true)
                        {
                            OscChatbox.SendMessage(Message + Config.Blank, direct: true, complete: false);
                        }
                    }
                    Console.ResetColor();
                }
            }
            Console.WriteLine();
        }
        // End \\
    }
}