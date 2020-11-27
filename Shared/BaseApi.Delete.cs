namespace Zebble
{
    using System;
    using System.Threading.Tasks;

    partial class BaseApi
    {
        public static async Task<bool> Delete(string relativeUrl, object jsonParams = null, OnError errorAction = OnError.Alert, bool showWaiting = true)
        {
            var result = await DoDelete<string>(relativeUrl, jsonParams, errorAction, showWaiting);
            return result.Item2.Error == null;
        }

        public static async Task<TResponse> Delete<TResponse>(
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true)
        {
            return (await DoDelete<TResponse>(relativeUrl, jsonParams, errorAction, showWaiting)).Item1;
        }

        public static Task<TResponse> Delete<TResponse, TEntity>(
            TEntity entity,
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true) where TEntity : IQueueable<Guid>
        {
            return Delete<TResponse, TEntity, Guid>(entity, relativeUrl, jsonParams, errorAction, showWaiting);
        }

        public static async Task<TResponse> Delete<TResponse, TEntity, TIdentifier>(
            TEntity entity,
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true) where TEntity : IQueueable<TIdentifier>
        {
            return (await DoDelete<TResponse, TEntity, TIdentifier>(entity, relativeUrl, jsonParams, errorAction, showWaiting)).Item1;
        }

        static async Task<Tuple<TResponse, RequestInfo>> DoDelete<TResponse, TEntity, TIdentifier>(
         TEntity entity,
         string relativeUrl,
         object jsonParams,
         OnError errorAction,
         bool showWaiting = true) where TEntity : IQueueable<TIdentifier>
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
                if (await request.Send<TEntity, TIdentifier>(entity)) result = await request.ExtractResponse<TResponse>();
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
         bool showWaiting = true)
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
                if (await request.Send()) result = await request.ExtractResponse<TResponse>();
                return Tuple.Create(result, request);
            }
            finally
            {
                if (showWaiting) await HideWaiting();
            }
        }
    }
}