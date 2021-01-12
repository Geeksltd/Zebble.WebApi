namespace Zebble
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Olive;

    partial class BaseApi
    {
        public static string StaleDataWarning = "The latest data cannot be received from the server right now.";
        const string CACHE_FOLDER = "-ApiCache";
        static object CacheSyncLock = new object();

        static FileInfo GetCacheFile<TResponse>(string url)
        {
            lock (CacheSyncLock)
                return Device.IO.Directory(Path.Combine(CACHE_FOLDER, GetTypeName<TResponse>())).EnsureExists().GetFile(url.ToIOSafeHash() + ".txt");
        }

        static FileInfo[] GetTypeCacheFiles<TResponse>(TResponse modified)
        {
            lock (CacheSyncLock)
                return Device.IO.Directory(Path.Combine(CACHE_FOLDER, GetTypeName(modified))).EnsureExists().GetFiles("*.txt");
        }

        static string GetTypeName<T>() => typeof(T).GetGenericArguments().SingleOrDefault()?.Name ?? typeof(T).Name.Replace("[]", "");

        static string GetTypeName<T>(T modified) => modified.GetType().Name;

        static string GetFullUrl(string baseUrl, object queryParams = null)
        {
            if (queryParams == null) return baseUrl;

            var queryString = queryParams as string;

            if (queryString == null)
                queryString = queryParams.GetType().GetPropertiesAndFields(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name + "=" + p.GetValue(queryParams).ToStringOrEmpty().UrlEncode())
                     .Trim().ToString("&");

            if (queryString.IsEmpty()) return baseUrl;

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
            if (cacheChoice == ApiResponseCache.Prefer || cacheChoice == ApiResponseCache.PreferThenUpdate || cacheChoice == ApiResponseCache.CacheOrNull)
            {
                result = GetCachedResponse<TResponse>(relativeUrl);
                if (HasValue(result))
                {
                    if (cacheChoice == ApiResponseCache.PreferThenUpdate)
                        Thread.Pool.RunOnNewThread(() => RefreshUponUpdatedResponse(relativeUrl, refresher));

                    return result;
                }

                if (cacheChoice == ApiResponseCache.CacheOrNull)
                    return result;
            }

            var request = new RequestInfo(relativeUrl) { ErrorAction = errorAction, HttpMethod = "GET" };

            if (await request.Send())
            {
                result = await request.ExtractResponse<TResponse>();

                if (request.Error == null)
                    await GetCacheFile<TResponse>(relativeUrl).WriteAllTextAsync(request.ResponseText);
            }

            if (request.Error != null && cacheChoice != ApiResponseCache.Refuse)
            {
                result = GetCachedResponse<TResponse>(relativeUrl);
                if (cacheChoice == ApiResponseCache.AcceptButWarn)
                    await Toast(StaleDataWarning);
            }

            return result;
        }

        static async Task RefreshUponUpdatedResponse<TResponse>(string url, Func<TResponse, Task> refresher)
        {
            await Task.Delay(50);

            string localCachedVersion;
            try
            {
                localCachedVersion = (await GetCacheFile<TResponse>(url).ReadAllTextAsync()).CreateSHA1Hash();
                if (localCachedVersion.IsEmpty()) throw new Exception("Local cached file's hash is empty!");
            }
            catch (Exception ex)
            {
                Log.For<BaseApi>().Error(ex, "Strangely, there is no cache any more when running RefreshUponUpdatedResponse(...).");
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
                    await GetCacheFile<TResponse>(url).WriteAllTextAsync(request.ResponseText);
                    await refresher(result);
                }
            }
            catch (Exception ex) { Log.For<BaseApi>().Error(ex); }
        }

        static async Task UpdateCacheUponOfflineModification<TResponse, TIdentifier>(TResponse modified, string httpMethod) where TResponse : IQueueable<TIdentifier>
        {
            await Task.Delay(50);

            // Get all cached files for this type
            var cachedFiles = GetTypeCacheFiles(modified);
            foreach (var file in cachedFiles)
            {
                var records = DeserializeResponse<IEnumerable<TResponse>>(file).ToList();
                var changed = false;

                // If the file contains the modified row, update it
                if (httpMethod == "DELETE")
                {
                    var deletedRecords = records.Where(x => EqualityComparer<TIdentifier>.Default.Equals(x.ID, modified.ID));
                    if (deletedRecords.Any())
                    {
                        records.RemoveAll(x => deletedRecords.Contains(x));
                        changed = true;
                    }
                }
                else if (httpMethod == "PATCH" || httpMethod == "PUT")
                {
                    foreach (var record in records.ToList())
                    {
                        if (EqualityComparer<TIdentifier>.Default.Equals(record.ID, modified.ID))
                        {
                            records.Replace(record, modified);
                            changed = true;
                        }
                    }
                }

                if (!changed) continue;
                // If cache file is edited, rewrite it
                var newResponseText = JsonConvert.SerializeObject(records);
                if (newResponseText.HasValue())
                    await file.WriteAllTextAsync(newResponseText);
            }
        }

        static TResponse GetCachedResponse<TResponse>(string url)
        {
            var file = GetCacheFile<TResponse>(url);
            return DeserializeResponse<TResponse>(file);
        }

        static TResponse DeserializeResponse<TResponse>(FileInfo file)
        {
            if (!file.Exists()) return default(TResponse);

            try
            {
                return JsonConvert.DeserializeObject<TResponse>(
                    file.ReadAllText(),
                    new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    }
                );
            }
            catch { return default; }
        }

        /// <summary>
        /// Deletes all cached Get API results.
        /// </summary>
        public static Task DisposeCache()
        {
            lock (CacheSyncLock)
            {
                if (Device.IO.Directory(CACHE_FOLDER).Exists())
                    Device.IO.Directory(CACHE_FOLDER).Delete(recursive: true);
            }

            // Designed as a task in case in the future we need it.
            return Task.CompletedTask;
        }

        /// <summary>
        /// Deletes the cached Get API result for the specified API url.
        /// </summary>
        public static Task DisposeCache<TResponse>(string getApiUrl)
        {
            lock (CacheSyncLock)
            {
                var file = GetCacheFile<TResponse>(getApiUrl);
                if (file.Exists())
                {
                    lock (file.GetSyncLock())
                        file.Delete();
                }
            }

            return Task.CompletedTask;
        }
    }
}