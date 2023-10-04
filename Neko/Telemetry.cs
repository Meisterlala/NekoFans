using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Neko.Sources.APIS;

namespace Neko;

public static class Telemetry
{
    private const int MaxQueueSize = 25;

    private static int errorCount;
    private static bool tmpDisabled;

    private static int saving;
    private static int lastSaved = Plugin.Config.LocalDownloadCount;
    private static DateTime lastSave = DateTime.MinValue;
    private static readonly TimeSpan saveInterval = TimeSpan.FromMinutes(5);
    private static DateTime lastUploaded = DateTime.Now;
    private static readonly TimeSpan uploadInterval = TimeSpan.FromMinutes(5);

    private static readonly Dictionary<Type, int> countBuffer = new();
    private static readonly Dictionary<Type, string> apiNames = new()
    {
        {typeof(Catboys), "catboys"},
        {typeof(DogCEO), "dog_ceo"},
        {typeof(NekosBest), "nekos.best"},
        {typeof(NekosLife), "nekos.life"},
        {typeof(PicRe), "nekos.life"},
        {typeof(ShibeOnline), "nekos.life"},
        {typeof(TheCatAPI), "the_cat_api"},
        {typeof(Twitter.UserTimeline), "twitter_user_timeline"},
        {typeof(Twitter.Search), "twitter_search"},
        {typeof(Waifuim), "waifu.im"},
        {typeof(WaifuPics), "waifu.pics"},
    };

    public static void RegisterDownload(Type api)
    {
        // Increment local counter
        Interlocked.Increment(ref Plugin.Config.LocalDownloadCount);

        // Save every X downloads or every Y minutes
        if ((DateTime.Now - lastSave > saveInterval
        || Math.Abs(Plugin.Config.LocalDownloadCount - lastSaved) > 50)
        && Interlocked.CompareExchange(ref saving, 1, 0) == 0)
        {
            lastSaved = Plugin.Config.LocalDownloadCount;
            lastSave = DateTime.Now;
            Plugin.Log.Verbose("Saving updated image download count");
            Plugin.Config.Save();
            Interlocked.Exchange(ref saving, 0);
        }

        if (!Plugin.Config.EnableTelemetry || tmpDisabled)
            return;

        // Increment buffer counter to send to API
        lock (countBuffer)
        {
            if (!countBuffer.ContainsKey(api))
                countBuffer.Add(api, 0);
            countBuffer[api]++;

            if (countBuffer[api] >= MaxQueueSize)
            {
                Send(api, countBuffer[api]);
                lastUploaded = DateTime.Now;
                countBuffer[api] = 0;
            }

            // Upload count if there was no changes in X minutes
            if (DateTime.Now - lastUploaded > uploadInterval)
            {
                foreach (var (key, key_count) in countBuffer)
                {
                    if (key_count > 0)
                    {
                        Send(key, key_count);
                        countBuffer[key] = 0;
                    }
                }
                lastUploaded = DateTime.Now;
            }
        }
    }

    private static Task Send(Type API, int count)
    {
        return !Plugin.Config.EnableTelemetry || tmpDisabled
            ? Task.CompletedTask
            : Task.Run(async () =>
        {
            if (!apiNames.ContainsKey(API))
            {
                Plugin.Log.Warning($"Could not find API name for {API}");
                return;
            }

            Plugin.Log.Verbose($"Sending {count} {API} downloads to telemetry");

            var url = $"{Plugin.ControlServer}/add/{apiNames[API]}/{count}";
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            HttpResponseMessage response;
            try
            {
                response = await Plugin.HttpClient.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Plugin.Log.Debug(ex, $"Failed to send telemetry: {ex.StatusCode}");
                if (Interlocked.Increment(ref errorCount) >= 10)
                {
                    Plugin.Log.Warning("Too many errors, disabling telemetry");
                    tmpDisabled = true;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug(ex, "Failed to send telemetry");
                if (Interlocked.Increment(ref errorCount) >= 10)
                {
                    Plugin.Log.Warning("Too many errors, disabling telemetry");
                    tmpDisabled = true;
                }
            }
        });
    }
}
