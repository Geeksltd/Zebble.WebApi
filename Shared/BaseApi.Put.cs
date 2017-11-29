namespace Zebble
{
    using System;
    using System.Threading.Tasks;

    partial class BaseApi
    {
        public static async Task<bool> Put(
          string relativeUrl,
          OnError errorAction = OnError.Alert,
          bool showWaiting = true)
        {
            var result = await DoPut<string>(relativeUrl, null, null, errorAction, showWaiting);
            return result.Item2.Error == null;
        }

        public static async Task<bool> Put(
            string relativeUrl,
            string requestData,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true)
        {
            var result = await DoPut<string>(relativeUrl, requestData, null, errorAction, showWaiting);
            return result.Item2.Error == null;
        }

        public static async Task<bool> Put(
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true)
        {
            var result = await DoPut<string>(relativeUrl, null, jsonParams, errorAction, showWaiting);
            return result.Item2.Error == null;
        }

        public static async Task<TResponse> Put<TResponse>(
            string relativeUrl,
            string requestData,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true)
        {
            return (await DoPut<TResponse>(relativeUrl, requestData, null, errorAction, showWaiting)).Item1;
        }

        public static async Task<TResponse> Put<TResponse, TEntity>(
           TEntity entity,
           string relativeUrl,
           string requestData,
           OnError errorAction = OnError.Alert,
           bool showWaiting = true) where TEntity : IQueueable<Guid>
        {
            return await Put<TResponse, TEntity, Guid>(entity, relativeUrl, requestData, errorAction, showWaiting);
        }

        public static async Task<TResponse> Put<TResponse, TEntity, TIdentifier>(
           TEntity entity,
           string relativeUrl,
           string requestData,
           OnError errorAction = OnError.Alert,
           bool showWaiting = true) where TEntity : IQueueable<TIdentifier>
        {
            return (await DoPut<TResponse, TEntity, TIdentifier>(entity, relativeUrl, requestData, null, errorAction, showWaiting)).Item1;
        }

        public static async Task<TResponse> Put<TResponse>(
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true)
        {
            return (await DoPut<TResponse>(relativeUrl, null, jsonParams, errorAction, showWaiting)).Item1;
        }

        public static async Task<TResponse> Put<TResponse, TEntity>(
            TEntity entity,
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true) where TEntity : IQueueable<Guid>
        {
            return await Put<TResponse, TEntity, Guid>(entity, relativeUrl, jsonParams, errorAction, showWaiting);
        }

        public static async Task<TResponse> Put<TResponse, TEntity, TIdentifier>(
            TEntity entity,
            string relativeUrl,
            object jsonParams,
            OnError errorAction = OnError.Alert,
            bool showWaiting = true) where TEntity : IQueueable<TIdentifier>
        {
            return (await DoPut<TResponse, TEntity, TIdentifier>(entity, relativeUrl, null, jsonParams, errorAction, showWaiting)).Item1;
        }

        static async Task<Tuple<TResponse, RequestInfo>> DoPut<TResponse>(
         string relativeUrl,
         string requestData,
         object jsonParams,
         OnError errorAction,
         bool showWaiting)
        {
            var request = new RequestInfo(relativeUrl)
            {
                ErrorAction = errorAction,
                HttpMethod = "PUT",
                RequestData = requestData,
                JsonData = jsonParams
            };

            try
            {
                if (showWaiting) await Waiting.Show();

                var result = default(TResponse);
                if (await request.Send()) result = await request.ExtractResponse<TResponse>();
                return Tuple.Create(result, request);
            }
            finally
            {
                if (showWaiting) await Waiting.Hide();
            }
        }

        static async Task<Tuple<TResponse, RequestInfo>> DoPut<TResponse, TEntity, TIdentifier>(
         TEntity entity,
         string relativeUrl,
         string requestData,
         object jsonParams,
         OnError errorAction,
         bool showWaiting) where TEntity : IQueueable<TIdentifier>
        {
            var request = new RequestInfo(relativeUrl)
            {
                ErrorAction = errorAction,
                HttpMethod = "PUT",
                RequestData = requestData,
                JsonData = jsonParams
            };

            try
            {
                if (showWaiting) await Waiting.Show();

                var result = default(TResponse);
                if (await request.Send<TEntity, TIdentifier>(entity)) result = await request.ExtractResponse<TResponse>();
                return Tuple.Create(result, request);
            }
            finally
            {
                if (showWaiting) await Waiting.Hide();
            }
        }
    }
}