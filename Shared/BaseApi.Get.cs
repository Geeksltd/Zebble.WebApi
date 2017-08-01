namespace Zebble
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    partial class BaseApi
    {
        public static string StaleDataWarning = "The latest data cannot be received from the server right now.";
        const string CacheFolder = "-ApiCache";
        static object CacheSyncLock = new object();

        static FileInfo GetCacheFile(string url)
        {
            lock (CacheSyncLock)
                return Device.IO.Directory(CacheFolder).EnsureExists().GetFile(url.ToIOSafeHash() + ".txt");
        }

        static string GetFullUrl(string baseUrl, object queryParams = null)
        {
            if (queryParams == null) return baseUrl;

            var queryString = queryParams as string;

            if (queryString == null)
                queryString = queryParams.GetType().GetPropertiesAndFields(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name + "=" + p.GetValue(queryParams).ToStringOrEmpty().UrlEncode())
                     .Trim().ToString("&");

            if (queryString.LacksValue()) return baseUrl;

            if (baseUrl.Contains("?")) return (baseUrl + "&" + queryString).KeepReplacing("&&", "&");
            return baseUrl + "?" + queryString;
        }

        static bool HasValue<TType>(TType value)
        {
            if (ReferenceEquals(value, null)) return false;
            if (value.Equals(default(TType))) return false;
            return true;
        }

        public static async Task<TResponse> Get<TResponse>(string relativeUrl, object queryParams = null, OnError errorAction = OnError.Toast, ApiResponseCache cacheChoice = ApiResponseCache.Accept, Func<TResponse, Task> refresher = null)
        {
            if (refresher != null && cacheChoice != ApiResponseCache.PreferThenUpdate)
                throw new ArgumentException("refresher can only be provided when using ApiResponseCache.PreferThenUpdate.");

            if (refresher == null && cacheChoice == ApiResponseCache.PreferThenUpdate)
                throw new ArgumentException("When using ApiResponseCache.PreferThenUpdate, refresher must be specified.");

            relativeUrl = GetFullUrl(relativeUrl, queryParams);

            var result = default(TResponse);
            if (cacheChoice == ApiResponseCache.Prefer || cacheChoice == ApiResponseCache.PreferThenUpdate)
            {
                result = GetCachedResponse<TResponse>(relativeUrl);
                if (HasValue(result))
                {
                    if (cacheChoice == ApiResponseCache.PreferThenUpdate)
                        Device.ThreadPool.RunOnNewThread(() => RefreshUponUpdatedResponse(relativeUrl, refresher));

                    return result;
                }
            }

            var request = new RequestInfo(relativeUrl) { ErrorAction = errorAction, HttpMethod = "GET" };

            if (await request.Send())
            {
                result = await request.ExtractResponse<TResponse>();

                if (request.Error == null)
                    await GetCacheFile(relativeUrl).WriteAllTextAsync(request.ResponseText);
            }

            if (request.Error != null && cacheChoice != ApiResponseCache.Refuse)
            {
                result = GetCachedResponse<TResponse>(relativeUrl);
                if (HasValue(result) && cacheChoice == ApiResponseCache.AcceptButWarn)
                    await Alert.Toast(StaleDataWarning);
            }

            return result;
        }

        static async Task RefreshUponUpdatedResponse<TResponse>(string url, Func<TResponse, Task> refresher)
        {
            await Task.Delay(50);

            string localCachedVersion;
            try
            {
                localCachedVersion = (await GetCacheFile(url).ReadAllTextAsync()).CreateSHA1Hash();
                if (localCachedVersion.LacksValue()) throw new Exception("Local cached file's hash is empty!");
            }
            catch (Exception ex)
            {
                Device.Log.Error("Strangely, there is no cache any more when running RefreshUponUpdatedResponse(...).");
                Device.Log.Error(ex);
                return; // High concurrency perhaps.
            }

            var request = new RequestInfo(url)
            {
                ErrorAction = OnError.Throw,
                HttpMethod = "GET",
                LocalCachedVersion = localCachedVersion
            };

            try
            {
                if (!await request.Send()) return;

                if (localCachedVersion.HasValue() && request.ResponseCode == System.Net.HttpStatusCode.NotModified) return;

                var newResponseCache = request.ResponseText.OrEmpty().CreateSHA1Hash();
                if (newResponseCache == localCachedVersion)
                {
                    // Same response. No update needed.
                    return;
                }

                var result = await request.ExtractResponse<TResponse>();
                if (request.Error == null)
                {
                    await GetCacheFile(url).WriteAllTextAsync(request.ResponseText);
                    await refresher(result);
                }
            }
            catch (Exception ex) { Device.Log.Error(ex); }
        }

        static TResponse GetCachedResponse<TResponse>(string url)
        {
            var file = GetCacheFile(url);
            if (!file.Exists()) return default(TResponse);

            try { return JsonConvert.DeserializeObject<TResponse>(file.ReadAllText()); }
            catch { return default(TResponse); }
        }

        /// <summary>
        /// Deletes all cached Get API results.
        /// </summary>
        public static Task DisposeCache()
        {
            lock (CacheSyncLock)
            {
                if (Device.IO.Directory(CacheFolder).Exists())
                    Device.IO.Directory(CacheFolder).Delete(recursive: true);
            }

            // Desined as a task in case in the future we need it.
            return Task.CompletedTask;
        }
    }
}