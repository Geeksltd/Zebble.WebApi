namespace Zebble
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    partial class BaseApi
    {
        public static async Task<bool> Patch(
            string relativeUrl,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true,
            Func<string> sessionTokenProvider = null,
            Action<HttpClient> onConfigureClient = null)
        {
            var result = await DoPatch<string>(relativeUrl, null, null, errorAction, showWaiting, sessionTokenProvider, onConfigureClient);
            return result.Item2.Error == null;
        }

        public static async Task<bool> Patch(
            string relativeUrl,
            string requestData,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true,
            Func<string> sessionTokenProvider = null,
            Action<HttpClient> onConfigureClient = null)
        {
            var result = await DoPatch<string>(relativeUrl, requestData, null, errorAction, showWaiting, sessionTokenProvider, onConfigureClient);
            return result.Item2.Error == null;
        }

        public static async Task<bool> Patch(
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true,
            Func<string> sessionTokenProvider = null,
            Action<HttpClient> onConfigureClient = null)
        {
            var result = await DoPatch<string>(relativeUrl, null, jsonParams, errorAction, showWaiting, sessionTokenProvider, onConfigureClient);
            return result.Item2.Error == null;
        }

        public static async Task<TResponse> Patch<TResponse>(
            string relativeUrl,
            string requestData,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true,
            Func<string> sessionTokenProvider = null,
            Action<HttpClient> onConfigureClient = null)
        {
            return (await DoPatch<TResponse>(relativeUrl, requestData, null, errorAction, showWaiting, sessionTokenProvider, onConfigureClient)).Item1;
        }

        public static Task<TResponse> Patch<TResponse, TEntity>(
            TEntity entity,
            string relativeUrl,
            string requestData,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true) where TEntity : IQueueable<Guid>
        {
            return Patch<TResponse, TEntity, Guid>(entity, relativeUrl, requestData, errorAction, showWaiting);
        }

        public static async Task<TResponse> Patch<TResponse, TEntity, TIdentifier>(
            TEntity entity,
            string relativeUrl,
            string requestData,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true,
            Func<string> sessionTokenProvider = null,
            Action<HttpClient> onConfigureClient = null) where TEntity : IQueueable<TIdentifier>
        {
            return (await DoPatch<TResponse, TEntity, TIdentifier>(entity, relativeUrl, requestData, null, errorAction, showWaiting, sessionTokenProvider, onConfigureClient)).Item1;
        }

        public static async Task<TResponse> Patch<TResponse>(
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true,
            Func<string> sessionTokenProvider = null,
            Action<HttpClient> onConfigureClient = null)
        {
            return (await DoPatch<TResponse>(relativeUrl, null, jsonParams, errorAction, showWaiting, sessionTokenProvider, onConfigureClient)).Item1;
        }

        public static Task<TResponse> Patch<TResponse, TEntity>(
            TEntity entity,
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true) where TEntity : IQueueable<Guid>
        {
            return Patch<TResponse, TEntity, Guid>(entity, relativeUrl, jsonParams, errorAction, showWaiting);
        }

        public static async Task<TResponse> Patch<TResponse, TEntity, TIdentifier>(
            TEntity entity,
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true,
            Func<string> sessionTokenProvider = null,
            Action<HttpClient> onConfigureClient = null) where TEntity : IQueueable<TIdentifier>
        {
            return (await DoPatch<TResponse, TEntity, TIdentifier>(entity, relativeUrl, null, jsonParams, errorAction, showWaiting, sessionTokenProvider, onConfigureClient)).Item1;
        }

        static async Task<Tuple<TResponse, RequestInfo>> DoPatch<TResponse>(
            string relativeUrl,
            string requestData,
            object jsonParams,
            OnError errorAction,
            bool showWaiting,
            Func<string> sessionTokenProvider,
            Action<HttpClient> onConfigureClient)
        {
            var request = new RequestInfo(relativeUrl)
            {
                ErrorAction = errorAction,
                HttpMethod = "PATCH",
                RequestData = requestData,
                JsonData = jsonParams
            };

            try
            {
                if (showWaiting) await ShowWaiting();

                var result = default(TResponse);
                if (await request.Send(sessionTokenProvider, onConfigureClient)) result = await request.ExtractResponse<TResponse>();
                return Tuple.Create(result, request);
            }
            finally
            {
                if (showWaiting) await HideWaiting();
            }
        }

        static async Task<Tuple<TResponse, RequestInfo>> DoPatch<TResponse, TEntity, TIdentifier>(
            TEntity entity,
            string relativeUrl,
            string requestData,
            object jsonParams,
            OnError errorAction,
            bool showWaiting,
            Func<string> sessionTokenProvider,
            Action<HttpClient> onConfigureClient) where TEntity : IQueueable<TIdentifier>
        {
            var request = new RequestInfo(relativeUrl)
            {
                ErrorAction = errorAction,
                HttpMethod = "PATCH",
                RequestData = requestData,
                JsonData = jsonParams
            };

            try
            {
                if (showWaiting) await ShowWaiting();

                var result = default(TResponse);
                if (await request.Send<TEntity, TIdentifier>(entity, sessionTokenProvider, onConfigureClient)) result = await request.ExtractResponse<TResponse>();
                return Tuple.Create(result, request);
            }
            finally
            {
                if (showWaiting) await HideWaiting();
            }
        }
    }
}