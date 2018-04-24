namespace Zebble
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    partial class BaseApi
    {
        const string QUEUE_FOLDER = "-ApiQueue";
        static object QueueSyncLock = new object();

        static FileInfo GetQueueFile()
        {
            lock (QueueSyncLock)
            {
                var file = Device.IO.Directory(QUEUE_FOLDER).EnsureExists().GetFile("Queue.txt");
                // TODO: not sure why needed for the first time. Should be removed in future.
                if (!file?.Exists ?? true) file.WriteAllText("");
                return file;
            }
        }

        static bool UpdateQueueFile<TEntitiy, TIdentifier>(IEnumerable<TEntitiy> items) where TEntitiy : IQueueable<TIdentifier>
        {
            var text = JsonConvert.SerializeObject(items);
            if (text.HasValue())
            {
                lock (QueueSyncLock)
                    GetQueueFile().WriteAllText(text);
                return true;
            }

            return false;
        }

        public static async Task<IEnumerable<TEntity>> GetQueueItems<TEntity>()
        {
            var file = GetQueueFile();
            var text = await file.ReadAllTextAsync();
            return JsonConvert.DeserializeObject<IEnumerable<TEntity>>(
                    text,
                    new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    }
                );
        }

        static async Task<bool> UpdateQueueItem<TEntitiy, TIdentifier>(TEntitiy item) where TEntitiy : IQueueable<TIdentifier>
        {
            var queueItems = await GetQueueItems<TEntitiy>();
            var edited = false;

            queueItems?.Do(queueItem =>
            {
                if (EqualityComparer<TIdentifier>.Default.Equals(queueItem.ID, item.ID))
                {
                    queueItem = item;
                    edited = true;
                }
            });

            if (edited)
                return UpdateQueueFile<TEntitiy, TIdentifier>(queueItems);

            return false;
        }

        static async Task<bool> AddQueueItem<TEntitiy, TIdentifier>(TEntitiy item) where TEntitiy : IQueueable<TIdentifier>
        {
            var queueItems = (await GetQueueItems<TEntitiy>() ?? new List<TEntitiy>()).ToList();
            queueItems.Add(item);
            return UpdateQueueFile<TEntitiy, TIdentifier>(queueItems);
        }

        public static async Task<bool> ApplyQueueItems<TEntitiy, TIdentifier>(bool applyRejectedItems = false) where TEntitiy : IQueueable<TIdentifier>
        {
            var queueItems = await GetQueueItems<TEntitiy>();

            if (queueItems == null) return false;

            foreach (var queueItem in queueItems.OrderBy(x => x.TimeAdded))
            {
                if (queueItem.Status == QueueStatus.Added
                || (queueItem.Status == QueueStatus.Rejected && applyRejectedItems))
                {
                    if (await queueItem.RequestInfo.Send()) queueItem.Status = QueueStatus.Applied;
                    else queueItem.Status = QueueStatus.Rejected;

                    queueItem.TimeUpdated = DateTime.Now;
                }
            }

            return UpdateQueueFile<TEntitiy, TIdentifier>(queueItems);
        }
    }
}