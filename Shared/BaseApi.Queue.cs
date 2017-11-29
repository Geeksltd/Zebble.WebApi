﻿namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    partial class BaseApi
    {
        const string QueueFolder = "-ApiQueue";
        static object QueueSyncLock = new object();

        static FileInfo GetQueueFile()
        {
            lock (QueueSyncLock)
                return Device.IO.Directory(QueueFolder).EnsureExists().GetFile("Queue.txt");
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
                ); ;
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

        public static async Task<bool> ApplyQueueItems<TEntitiy, TIdentifier>() where TEntitiy : IQueueable<TIdentifier>
        {
            var queueItems = await GetQueueItems<TEntitiy>();
            await queueItems?.WhenAll(async queueItem =>
            {
                if (queueItem.Status == QueueStatus.Added)
                {
                    if (await queueItem.RequestInfo.Send()) queueItem.Status = QueueStatus.Applied;
                    else queueItem.Status = QueueStatus.Rejected;

                    queueItem.TimeUpdated = DateTime.Now;
                }
            });

            return UpdateQueueFile<TEntitiy, TIdentifier>(queueItems);
        }
    }
}
