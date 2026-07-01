using System.Reflection;

namespace Fewtags_VRCX.Properties
{
    internal class Config
    {
        // Longs \\
        public static long LastReadOffset;
        // End \\

        // Bools \\
        public static bool OSC = false;
        public static bool RPC = true;
        public static bool ToastNotifications = false;
        // End \\

        // Strings \\
        public static int Tagged = 0;
        public static bool VRCCheck = true;
        public static string Version = "1.0.1";
        public static string Blank = "\u0003\u0003";
        public static string CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static string Configuration = CurrentDirectory + @"\Config.json";
        public static DirectoryInfo CustomTags = Directory.CreateDirectory("Custom Tags");
        public static DirectoryInfo External = Directory.CreateDirectory("Custom Tags/External");
        public static DirectoryInfo Internal = Directory.CreateDirectory("Custom Tags/Internal");
        public static string ApplicationName = Assembly.GetEntryAssembly().GetName().Name;
        public static string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        public static string InternalTagsEndPoint = "https://raw.githubusercontent.com/Fewdys/FewTags/refs/heads/main/FewTags.json";
        public static string ExternalTagsEndPoint = "https://raw.githubusercontent.com/Fewdys/FewTags/refs/heads/main/ExternalTags.json";
        public static string Logs = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat";
        public static string NotificationIcon = File.Exists(Path.Combine(CurrentDirectory, "Resources", "Icon.png")) ? Path.Combine(CurrentDirectory, "Resources", "Icon.png") : null;
        public static void Check()
        {
            if (string.IsNullOrEmpty(NotificationIcon))
            {
                Console.WriteLine("Notification Icon Missing");
                Thread.Sleep(2000);
                Environment.Exit(1);
            }
        }
        // End \\

        // Enums \\
        public enum Status
        {
            VRCX,
            Joined,
            Left,
            Myself,
            Unknown
        }
        // End \\

        // Classes \\
        public static Tags InternalTags { get; set; }
        public static Tags ExternalTags { get; set; }
        public class Configurator
        {
            public bool OSC { get; set; }
            public bool RPC { get; set; }
            public bool ToastNotifications { get; set; }
        }
        public class Tags
        {
            public long ID { get; set; }
            public bool Active { get; set; }
            public string Size { get; set; }
            public string[] Tag { get; set; }
            public string UserID { get; set; }
            public bool Malicious { get; set; }
            public bool TextActive { get; set; }
            public List<Tags> Records { get; set; }
            public bool BigTextActive { get; set; }
            public string DisplayName { get; set; }
            public string PlateBigText { get; set; }
            public string[] PreviousDisplayNames { get; set; }
            public string DateAdded { get; set; }
            public string DateUpdated { get; set; }

            public static Tags operator +(Tags I, Tags V)
            {
                var Result = new Tags();
                Result.Records = new List<Tags>();
                if (I?.Records != null)
                {
                    Result.Records.AddRange(I.Records);
                }
                if (V?.Records != null)
                {
                    Result.Records.AddRange(V.Records);
                }

                var TagList = new List<string>();
                if (I?.Tag != null)
                {
                    TagList.AddRange(I.Tag);
                }
                if (V?.Tag != null)
                {
                    TagList.AddRange(V.Tag);
                }
                Result.Tag = TagList.ToArray();

                Result.ID = V?.ID ?? I?.ID ?? 0;
                Result.Active = I?.Active == true || V?.Active == true;
                Result.TextActive = I?.TextActive == true || V?.TextActive == true;
                Result.BigTextActive = I?.BigTextActive == true || V?.BigTextActive == true;
                Result.Malicious = I?.Malicious == true || V?.Malicious == true;

                Result.Size = V?.Size ?? I?.Size;
                Result.UserID = V?.UserID ?? I?.UserID;
                Result.DisplayName = V?.DisplayName ?? I?.DisplayName;
                Result.PlateBigText = V?.PlateBigText ?? I?.PlateBigText;
                Result.PreviousDisplayNames = V?.PreviousDisplayNames ?? I?.PreviousDisplayNames;
                Result.DateAdded = V?.DateAdded ?? I?.DateAdded;
                Result.DateUpdated = V?.DateUpdated ?? I?.DateUpdated;

                return Result;
            }
        }
        // End \\
    }
}