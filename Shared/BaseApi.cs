namespace Zebble
{
    using System;
    using System.Linq;
    using Olive;

    public partial class BaseApi
    {
        public static string BaseUrl = Config.Get("Api.Base.Url").OrEmpty().TrimEnd("/");

        public static Func<string> GetSessionToken = () =>
        {
            var file = Device.IO.File("SessionToken.txt");

            if (file.Exists()) return file.ReadAllText();

            return null;
        };

        /// <summary>
        /// Returns a full absolute URL for a specified relativeUrl.
        /// </summary>
        public static string Url(params string[] relativeUrlParts)
        {
            var result = relativeUrlParts.Trim().Select(x => x.TrimStart("/").TrimEnd("/")).Trim().ToString("/");

            if (result.StartsWithAny("http://", "https://")) return result;

            if (BaseUrl.IsEmpty()) throw new Exception("Could not find the config value for 'Api.Base.Url'.");

            return BaseUrl.EnsureEndsWith("/") + result;
        }
    }

    internal class ServerError
    {
        public string Message { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace { get; set; }
    }
}