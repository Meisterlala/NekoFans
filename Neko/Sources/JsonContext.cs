using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Neko.Sources.APIS;

namespace Neko.Sources;

public static class JsonContext
{
    public static readonly Dictionary<Type, JsonSerializerContext> dic = new()
    {
        { typeof(Catboys.CatboysJson), CatboysContext.Default },
        { typeof(DogCEO.DogCEOJson), DogCEOContext.Default },
        { typeof(NekosBest.NekosBestJson), NekosBestContext.Default },
        { typeof(NekosLife.NekosLifeJson), NekosLifeContext.Default },
        { typeof(ShibeOnline.ShibeOnlineJson), ShibeOnlineContext.Default },
        { typeof(TheCatAPI.TheCatAPIJson), TheCatAPIContext.Default },
        { typeof(Twitter.Search.SearchJson), TwitterSearchJsonContext.Default },
        { typeof(Twitter.CountJson), TwitterCountJsonContext.Default },
        { typeof(Twitter.UserTimeline.TweetTimelineJson), TweetTimelineJsonContext.Default },
        { typeof(Twitter.UserTimeline.UserLookupJson), UserLookupJsonContext.Default },
        { typeof(Waifuim.WaifuImJson), WaifuimContext.Default },
        { typeof(WaifuPics.WaifuPicsJson), WaifuPicsContext.Default }
    };

    public static JsonTypeInfo<T> GetTypeInfo<T>()
    {
        var typ = typeof(T);
        if (!dic.ContainsKey(typ))
            throw new Exception("Unknown API Json Context. Please use JsonSerializable to generate a JsonSerializerContext");

        var info = dic[typ].GetTypeInfo(typ);

        return info == null ? throw new Exception("Error in Code Generation") : (JsonTypeInfo<T>)info;
    }
}

[JsonSerializable(typeof(Catboys.CatboysJson))]
sealed partial class CatboysContext : JsonSerializerContext { }

[JsonSerializable(typeof(DogCEO.DogCEOJson))]
sealed partial class DogCEOContext : JsonSerializerContext { }

[JsonSerializable(typeof(NekosBest.NekosBestJson))]
sealed partial class NekosBestContext : JsonSerializerContext { }

[JsonSerializable(typeof(NekosLife.NekosLifeJson))]
sealed partial class NekosLifeContext : JsonSerializerContext { }

[JsonSerializable(typeof(ShibeOnline.ShibeOnlineJson))]
sealed partial class ShibeOnlineContext : JsonSerializerContext { }

[JsonSerializable(typeof(Twitter.Search.SearchJson))]
sealed partial class TwitterSearchJsonContext : JsonSerializerContext { }

[JsonSerializable(typeof(Twitter.CountJson))]
sealed partial class TwitterCountJsonContext : JsonSerializerContext { }

[JsonSerializable(typeof(Twitter.UserTimeline.TweetTimelineJson))]
sealed partial class TweetTimelineJsonContext : JsonSerializerContext { }

[JsonSerializable(typeof(Twitter.UserTimeline.UserLookupJson))]
sealed partial class UserLookupJsonContext : JsonSerializerContext { }

[JsonSerializable(typeof(TheCatAPI.TheCatAPIJson))]
sealed partial class TheCatAPIContext : JsonSerializerContext { }

[JsonSerializable(typeof(Waifuim.WaifuImJson))]
sealed partial class WaifuimContext : JsonSerializerContext { }

[JsonSerializable(typeof(WaifuPics.WaifuPicsJson))]
sealed partial class WaifuPicsContext : JsonSerializerContext { }
