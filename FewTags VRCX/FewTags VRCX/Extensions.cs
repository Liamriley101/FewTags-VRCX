using System.Net;

namespace FewTags
{
    internal class Extensions
    {
        internal class Console
        {
            internal static void Beep()
            {
                System.Console.Beep();
            }
        }

        internal class HttpClientExtensions
        {
            internal static async Task<HttpResponseMessage> GetAsync(string url)
            {
                try
                {
                    using (HttpClientHandler Handler = new HttpClientHandler())
                    {
                        Handler.AutomaticDecompression = DecompressionMethods.All;
                        using (HttpClient Https = new HttpClient(Handler))
                        {
                            return await Https.GetAsync(url);
                        }
                    }
                }
                catch
                {
                    System.Console.WriteLine("There Was An Error While Getting A Response From The Server");
                }
                return null;
            }
        }
    }
}
