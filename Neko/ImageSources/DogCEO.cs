using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace Neko.Sources;

public class DogCEO : IImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled = false;
        public Breed breed = Breed.all;
        public int selected = 0;

        public IImageSource? LoadConfig()
        {
            if (enabled)
                return new DogCEO(breed);
            return null;
        }
    }


    private const int URL_COUNT = 10; // max 50
    private readonly MultiURLs<DogCEOJson> URLs;

    public DogCEO(Breed b)
    {
        if (b == Breed.all)
            URLs = new($"https://dog.ceo/api/breeds/image/random/{URL_COUNT}");
        else
            URLs = new($"https://dog.ceo/api/breed/{BreedPath(b)}/images/random/{URL_COUNT}");
    }
    public async Task<NekoImage> Next(CancellationToken ct = default)
    {
        var url = await URLs.GetURL();
        return await Common.DownloadImage(url, ct);
    }

    public override string ToString()
    {
        var breed = Plugin.Config.Sources.DogCEO.breed;
        return $"Dog CEO\tBreed: {BreedName(breed)}\tURLs: {URLs.URLCount}";
    }

    public static string BreedName(Breed b)
    {
        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
        var name = Enum.GetName(typeof(Breed), b)?.Trim() ?? "Unknown";
        name = name.Replace("_", " (");
        if (name.Contains("("))
            name += ")";
        return textInfo.ToTitleCase(name);
    }

    public static string BreedPath(Breed b)
    {
        var name = Enum.GetName(typeof(Breed), b)?.Trim() ?? "Unknown";
        return name.Replace("_", "/");
    }

#pragma warning disable
    public class DogCEOJson : IJsonToList
    {
        public List<string> message { get; set; }
        public string status { get; set; }

        public List<string> ToList() => message;
    }
#pragma warning restore

    public enum Breed
    {
        all,
        affenpinscher, african, airedale, akita, appenzeller,
        australian, australian_shepherd,
        basenji, beagle, bluetick, borzoi, bouvier, boxer, brabancon, briard, buhund, buhund_norwegian,
        bulldog, bulldog_boston, bulldog_english, bulldog_french,
        bullterrier, bullterrier_staffordshire,
        cattledog, cattledog_australian,
        chihuahua, chow, clumber, cockapoo,
        collie, collie_border,
        coonhound,
        corgi, corgi_cardigan,
        cotondetulear, dachshund, dalmatian,
        dane, dane_great,
        deerhound, deerhound_scottish,
        dhole, dingo, doberman,
        elkhound, elkhound_norwegian,
        entlebucher, eskimo,
        finnish, finnish_lapphund,
        frise, frise_bichon,
        germanshepherd,
        greyhound, greyhound_italian,
        groenendael, havanese,
        hound, hound_afghan, hound_basset, hound_blood, hound_english, hound_ibizan, hound_plott, hound_walker,
        husky,
        keeshond, kelpie, komondor, kuvasz,
        labradoodle, labrador, leonberg, lhasa,
        malamute, malinois, maltese,
        mastiff, mastiff_bull, mastiff_english, mastiff_tibetan,
        mexicanhairless, mix,
        mountain, mountain_bernese, mountain_swiss,
        newfoundland, otterhound,
        ovcharka, ovcharka_caucasian,
        papillon, pekinese, pembroke,
        pinscher, pinscher_miniature,
        pitbull,
        pointer, pointer_german, pointer_germanlonghair,
        pomeranian,
        poodle, poodle_medium, poodle_miniature, poodle_standard, poodle_toy,
        pug, puggle, pyrenees, redbone,
        retriever, retriever_chesapeake, retriever_curly, retriever_flatcoated, retriever_golden,
        ridgeback, ridgeback_rhodesian,
        rottweiler,
        saluki, samoyed, schipperke,
        schnauzer, schnauzer_giant, schnauzer_miniature,
        setter, setter_english, setter_gordon, setter_irish,
        sharpei,
        sheepdog, sheepdog_english, sheepdog_shetland,
        shiba, shihtzu,
        spaniel, spaniel_blenheim, spaniel_brittany, spaniel_cocker, spaniel_irish, spaniel_japanese, spaniel_sussex, spaniel_welsh,
        springer, springer_english,
        stbernard,
        terrier, terrier_american, terrier_australian, terrier_bedlington, terrier_border, terrier_cairn, terrier_dandie, terrier_fox,
        terrier_irish, terrier_kerryblue, terrier_lakeland, terrier_norfolk, terrier_norwich, terrier_patterdale, terrier_russell, terrier_scottish,
        terrier_sealyham, terrier_silky, terrier_tibetan, terrier_toy, terrier_welsh, terrier_westhighland, terrier_wheaten, terrier_yorkshire,
        tervuren,
        vizsla,
        waterdog, waterdog_spanish,
        weimaraner,
        whippet,
        wolfhound, wolfhound_irish,
    }
}