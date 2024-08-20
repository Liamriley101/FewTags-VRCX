using System.Reflection;

namespace FewTags
{
    internal class Config
    {
        // Longs \\
        public static long LastReadOffset;
        // End \\

        // Bools \\
        public static bool OSC = true;
        public static bool RPC = true;
        public static bool ToastNotifications = true;
        // End \\

        // Strings \\
        public static int Fewdys = 0;
        public static bool VRCCheck = true;
        public static string Version = "1.0.1";
        public static string Blank = "\u0003\u0003";
        public static string CurrentDirectory = Directory.GetCurrentDirectory();
        public static string Configuration = CurrentDirectory + @"\Config.json";
        public static string ApplicationName = Assembly.GetEntryAssembly().GetName().Name;
        public static string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        public static string InternalTagsEndPoint = "https://raw.githubusercontent.com/Fewdys/FewTags/main/FewTags.json";
        public static string ExternalTagsEndPoint = "https://raw.githubusercontent.com/Fewdys/FewTags/main/ExternalTags.json";
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
            Myself
        }
        // End \\

        // Classes \\
        public static Tags InternalTags { get; set; }
        public static Tags ExternalTags { get; set; }
        public static string InternalRawTags { get; set; }
        public static string ExternalRawTags { get; set; }
        public class Configurator
        {
            public bool OSC { get; set; }
            public bool RPC { get; set; }
            public bool ToastNotifications { get; set; }
        }
        public class Tags
        {
            public int ID { get; set; }
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
        }
        // End \\
    }
}