namespace Zebble
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    partial class BaseApi
    {
        public static async Task<bool> Delete(
            string relativeUrl,
            object jsonParams = null,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true,
            Func<string> sessionTokenProvider = null,
            Action<HttpClient> onConfigureClient = null)
        {
            var result = await DoDelete<string>(relativeUrl, jsonParams, errorAction, showWaiting, sessionTokenProvider, onConfigureClient);
            return result.Item2.Error == null;
        }

        public static async Task<TResponse> Delete<TResponse>(
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true,
            Func<string> sessionTokenProvider = null,
            Action<HttpClient> onConfigureClient = null)
        {
            return (await DoDelete<TResponse>(relativeUrl, jsonParams, errorAction, showWaiting, sessionTokenProvider, onConfigureClient)).Item1;
        }

        public static Task<TResponse> Delete<TResponse, TEntity>(
            TEntity entity,
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true,
            Func<string> sessionTokenProvider = null,
            Action<HttpClient> onConfigureClient = null) where TEntity : IQueueable<Guid>
        {
            return Delete<TResponse, TEntity, Guid>(entity, relativeUrl, jsonParams, errorAction, showWaiting, sessionTokenProvider, onConfigureClient);
        }

        public static async Task<TResponse> Delete<TResponse, TEntity, TIdentifier>(
            TEntity entity,
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true,
            Func<string> sessionTokenProvider = null,
            Action<HttpClient> onConfigureClient = null) where TEntity : IQueueable<TIdentifier>
        {
            return (await DoDelete<TResponse, TEntity, TIdentifier>(entity, relativeUrl, jsonParams, errorAction, showWaiting, sessionTokenProvider, onConfigureClient)).Item1;
        }

        static async Task<Tuple<TResponse, RequestInfo>> DoDelete<TResponse, TEntity, TIdentifier>(
         TEntity entity,
         string relativeUrl,
         object jsonParams,
         OnError errorAction,
         bool showWaiting = true,
         Func<string> sessionTokenProvider = null,
         Action<HttpClient> onConfigureClient = null) where TEntity : IQueueable<TIdentifier>
        {
            var request = new RequestInfo(relativeUrl)
            {
                ErrorAction = errorAction,
                HttpMethod = "DELETE",
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

        static async Task<Tuple<TResponse, RequestInfo>> DoDelete<TResponse>(
            string relativeUrl,
            object jsonParams,
            OnError errorAction,
            bool showWaiting,
            Func<string> sessionTokenProvider,
            Action<HttpClient> onConfigureClient)
        {
            var request = new RequestInfo(relativeUrl)
            {
                ErrorAction = errorAction,
                HttpMethod = "DELETE",
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
    }
}