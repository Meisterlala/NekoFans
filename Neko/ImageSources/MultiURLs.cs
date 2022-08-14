using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace Neko.Sources
{
    public class MultiURLs<T>
    {
        private const int URLThreshold = 25;
        private Task<T> getNewURLs;
        private readonly string url;
        private readonly ConcurrentQueue<string> URLs = new();
        private readonly Func<T, List<string>> func;

        public int URLCount => URLs.Count;

        public MultiURLs(string url, Func<T, List<string>> function)
        {
            this.url = url;
            func = function;
            getNewURLs = StartTask();
        }

        public async Task<string> GetURL()
        {
            // Load more
            if (URLs.Count <= URLThreshold && getNewURLs.IsCompletedSuccessfully)
                getNewURLs = StartTask();

            string? res;
            do
            {
                await getNewURLs;
            } while (!URLs.TryDequeue(out res));

            if (res == null || getNewURLs.IsFaulted)
                throw new Exception("Could not get URLs to images");

            return res;
        }

        private async Task<T> StartTask()
        {
            var tsk = Common.ParseJson<T>(url);
            _ = tsk.ContinueWith((task) =>
            {
                foreach (var ex in task.Exception?.Flatten().InnerExceptions ?? new(Array.Empty<Exception>()))
                {
                    _ = ex;
                }

                if (task.IsCompletedSuccessfully)
                {
                    var list = func(task.Result);
                    foreach (var item in list)
                    {
                        URLs.Enqueue(item);
                    }
                }
            });
            return await tsk;
        }
    }
}