using System;
using System.Collections.Generic;
using Dalamud.Logging;

namespace Neko;

/// <summary>
/// This Class checks if NFSW images should be displayed.
/// They are only displayed if NekoLewd is installed and enabled.
/// This check is done with IPC.
/// </summary>
public static class NSFW
{
    private enum NSFW_MODE
    {
        Enabled, Disabled, Unknown
    }

    private static NSFW_MODE mode = NSFW_MODE.Unknown!;
    public static bool AllowNSFW
    {
        get
        {
            var check = CheckIPC();
            var newMode = check ? NSFW_MODE.Enabled : NSFW_MODE.Disabled;
            if (newMode != mode && mode != NSFW_MODE.Unknown)
            {
                mode = newMode;
                ChangeDetected(mode);
            }
            else
            {
                mode = newMode;
            }
            return mode == NSFW_MODE.Enabled;
        }
    }

    private static bool CheckIPC()
    {
        if (!Plugin.PluginInterface.PluginInternalNames.Contains("NekoLewd"))
            return false;

        bool ipcCheck;
        try
        {
            var sub = Plugin.PluginInterface.GetIpcSubscriber<bool>("NSFW Check");
            ipcCheck = sub.InvokeFunc();
        }
        catch (Exception)
        {
            // Could not use IPC to Check for NSFW mode
            return false;
        }
        return ipcCheck;
    }

    private static void ChangeDetected(NSFW_MODE mode)
    {
        if (mode == NSFW_MODE.Unknown)
            return;
        else if (mode == NSFW_MODE.Enabled)
            PluginLog.Log("Detected NSFW Plugin. NSFW Images are now avalible.");
        else
            PluginLog.Log("NSFW Plugin was disabled. NSFW Images are unavalible");

        // Reload Config
        Plugin.UpdateImageSource();
        // Refresh Image Queue
        Plugin.GuiMain?.Queue.Refresh();
    }

    public static bool Allowed(string text)
    {
        // Sort to make Lookup faster
        if (!BadWordsSorted)
        {
            BadWords.Sort();
            BadWordsSorted = true;
        }

        if (mode == NSFW_MODE.Enabled)
            return true;
        var lower = text.ToLower().Replace(" ", "_");
        foreach (var s in BadWords)
        {
            if (lower.Contains(s))
                return false;
        }
        return true;
    }

    public static string? MatchesBadWord(string text)
    {
        // Sort to make Lookup faster
        if (!BadWordsSorted)
        {
            BadWords.Sort();
            BadWordsSorted = true;
        }

        if (mode == NSFW_MODE.Enabled)
            return null;
        var lower = text.ToLower().Replace(" ", "_");
        foreach (var s in BadWords)
        {
            if (lower.Contains(s))
                return s;
        }
        return null;
    }

    public static bool NotAllowed(string text) => !Allowed(text);
    private static bool BadWordsSorted;

    private static readonly List<string> BadWords = new() {
        "2g1c", "2_girls_1_cup", "acrotomophilia", "alabama_hot_pocket", "alaskan_pipeline", "anal", "anilingus", "anus", "apeshit", "arsehole", "ass", "asshole", "assmunch", "auto_erotic", "autoerotic", "babeland", "baby_batter", "baby_juice", "ball_gag", "ball_gravy", "ball_kicking", "ball_licking", "ball_sack", "ball_sucking", "bangbros", "bangbus", "bareback", "barely_legal", "barenaked", "bastard", "bastardo", "bastinado", "bbw", "bdsm", "beaner", "beaners", "beaver_cleaver", "beaver_lips", "beastiality", "bestiality", "big_black", "big_breasts", "big_knockers", "big_tits", "bimbos", "birdlock", "bitch", "bitches", "black_cock", "blonde_action", "blonde_on_blonde_action", "blowjob", "blow_job", "blow_your_load", "blue_waffle", "blumpkin", "bollocks", "bondage", "boner", "boob", "boobs", "booty_call", "brown_showers", "brunette_action", "bukkake", "bulldyke", "bullet_vibe", "bullshit", "bung_hole", "bunghole", "busty", "butt", "buttcheeks", "butthole", "camel_toe", "camgirl", "camslut", "camwhore", "carpet_muncher", "carpetmuncher", "chocolate_rosebuds", "cialis", "circlejerk", "cleveland_steamer", "clit", "clitoris", "clover_clamps", "clusterfuck", "cock", "cocks", "coprolagnia", "coprophilia", "cornhole", "coon", "coons", "creampie", "cum", "cumming", "cumshot", "cumshots", "cunnilingus", "cunt", "darkie", "date_rape", "daterape", "deep_throat", "deepthroat", "dendrophilia", "dick", "dildo", "dingleberry", "dingleberries", "dirty_pillows", "dirty_sanchez", "doggie_style", "doggiestyle", "doggy_style", "doggystyle", "dog_style", "dolcett", "domination", "dominatrix", "dommes", "donkey_punch", "double_dong", "double_penetration", "dp_action", "dry_hump", "dvda", "eat_my_ass", "ecchi", "ejaculation", "erotic", "erotism", "escort", "eunuch", "fag", "faggot", "fecal", "felch", "fellatio", "feltch", "female_squirting", "femdom", "figging", "fingerbang", "fingering", "fisting", "foot_fetish", "footjob", "frotting", "fuck", "fuck_buttons", "fuckin", "fucking", "fucktards", "fudge_packer", "fudgepacker", "futanari", "gangbang", "gang_bang", "gay_sex", "genitals", "giant_cock", "girl_on", "girl_on_top", "girls_gone_wild", "goatcx", "goatse", "god_damn", "gokkun", "golden_shower", "goodpoop", "goo_girl", "goregasm", "grope", "group_sex", "g-spot", "guro", "hand_job", "handjob", "hard_core", "hardcore", "hentai", "homoerotic", "honkey", "hooker", "horny", "hot_carl", "hot_chick", "how_to_kill", "how_to_murder", "huge_fat", "humping", "incest", "intercourse", "jack_off", "jail_bait", "jailbait", "jelly_donut", "jerk_off", "jigaboo", "jiggaboo", "jiggerboo", "jizz", "juggs", "kike", "kinbaku", "kinkster", "kinky", "knobbing", "leather_restraint", "leather_straight_jacket", "lemon_party", "livesex", "lolita", "lovemaking", "make_me_come", "male_squirting", "masturbate", "masturbating", "masturbation", "menage_a_trois", "milf", "missionary_position", "mong", "motherfucker", "mound_of_venus", "mr_hands", "muff_diver", "muffdiving", "nambla", "nawashi", "negro", "neonazi", "nigga", "nigger", "nig_nog", "nimphomania", "nipple", "nipples", "nsfw", "nsfw_images", "nude", "nudity", "nutten", "nympho", "nymphomania", "octopussy", "omorashi", "one_cup_two_girls", "one_guy_one_jar", "orgasm", "orgy", "paedophile", "paki", "panties", "panty", "pedobear", "pedophile", "pegging", "penis", "phone_sex", "piece_of_shit", "pikey", "pissing", "piss_pig", "pisspig", "playboy", "pleasure_chest", "pole_smoker", "ponyplay", "poof", "poon", "poontang", "punany", "poop_chute", "poopchute", "porn", "porno", "pornography", "prince_albert_piercing", "pthc", "pubes", "pussy", "queaf", "queef", "quim", "raghead", "raging_boner", "rape", "raping", "rapist", "rectum", "reverse_cowgirl", "rimjob", "rimming", "rosy_palm", "rosy_palm_and_her_5_sisters", "rusty_trombone", "sadism", "santorum", "scat", "schlong", "scissoring", "semen", "sex", "sexcam", "sexo", "sexy", "sexual", "sexually", "sexuality", "shaved_beaver", "shaved_pussy", "shemale", "shibari", "shit", "shitblimp", "shitty", "shota", "shrimping", "skeet", "slanteye", "slut", "s&m", "smut", "snatch", "snowballing", "sodomize", "sodomy", "spastic", "spic", "splooge", "splooge_moose", "spooge", "spread_legs", "spunk", "strap_on", "strapon", "strappado", "strip_club", "style_doggy", "suck", "sucks", "suicide_girls", "sultry_women", "swastika", "swinger", "tainted_love", "taste_my", "tea_bagging", "threesome", "throating", "thumbzilla", "tied_up", "tight_white", "tit", "tits", "titties", "titty", "tongue_in_a", "topless", "tosser", "towelhead", "tranny", "tribadism", "tub_girl", "tubgirl", "tushy", "twat", "twink", "twinkie", "two_girls_one_cup", "undressing", "upskirt", "urethra_play", "urophilia", "vagina", "venus_mound", "viagra", "vibrator", "violet_wand", "vorarephilia", "voyeur", "voyeurweb", "voyuer", "vulva", "wank", "wetback", "wet_dream", "white_power", "whore", "worldsex", "wrapping_men", "wrinkled_starfish", "xx", "xxx", "yaoi", "yellow_showers", "yiffy", "zoophilia", "🖕",
        "baiser", "bander", "bigornette", "bite", "bitte", "bloblos", "bordel", "bourré", "bourrée", "brackmard", "branlage", "branler", "branlette", "branleur", "branleuse", "brouter_le_cresson", "caca", "chatte", "chiasse", "chier", "chiottes", "clito", "clitoris", "con", "connard", "connasse", "conne", "couilles", "cramouille", "cul", "déconne", "déconner", "emmerdant", "emmerder", "emmerdeur", "emmerdeuse", "enculé", "enculée", "enculeur", "enculeurs", "enfoiré", "enfoirée", "étron", "fille_de_pute", "fils_de_pute", "folle", "foutre", "gerbe", "gerber", "gouine", "grande_folle", "grogniasse", "gueule", "jouir", "la_putain_de_ta_mère", "MALPT", "ménage_à_trois", "merde", "merdeuse", "merdeux", "meuf", "nègre", "negro", "nique_ta_mère", "nique_ta_race", "palucher", "pédale", "pédé", "péter", "pipi", "pisser", "pouffiasse", "pousse-crotte", "putain", "pute", "ramoner", "sac_à_foutre", "sac_à_merde", "salaud", "salope", "suce", "tapette", "tanche", "teuch", "tringler", "trique", "troncher", "trou_du_cul", "turlute", "zigounette", "zizi",
        "analritter", "arsch", "arschficker", "arschlecker", "arschloch", "bimbo", "bratze", "bumsen", "bonze", "dödel", "fick", "ficken", "flittchen", "fotze", "fratze", "hackfresse", "hure", "hurensohn", "ische", "kackbratze", "kacke", "kacken", "kackwurst", "kampflesbe", "kanake", "kimme", "lümmel", "MILF", "möpse", "morgenlatte", "möse", "mufti", "muschi", "nackt", "neger", "nigger", "nippel", "nutte", "onanieren", "orgasmus", "penis", "pimmel", "pimpern", "pinkeln", "pissen", "pisser", "popel", "poppen", "porno", "reudig", "rosette", "schabracke", "schlampe", "scheiße", "scheisser", "schiesser", "schnackeln", "schwanzlutscher", "schwuchtel", "tittchen", "titten", "vögeln", "vollpfosten", "wichse", "wichsen", "wichser",
        "age_progression", "age_regression", "dilf", "infantilism", "lolicon", "milf", "old_lady", "old_man", "shotacon", "toddlercon", "adventitious_penis", "adventitious_vagina", "amputee", "big_muscles", "body_modification", "conjoined", "doll_joints", "gijinka", "invisible", "multiple_arms", "multiple_breasts", "multiple_nipples", "multiple_penises", "multiple_vaginas", "muscle", "muscle_growth", "pregnant", "shapening", "stretching", "tailjob", "wingjob", "absorption", "age_progression", "age_regression", "ass_expansion", "balls_expansion", "body_swap", "breast_expansion", "breast_reduction", "clit_growth", "corruption", "dick_growth", "feminization", "gender_change", "gender_morph", "growth", "moral_degeneration", "muscle_growth", "nipple_expansion", "personality_excretion", "petrification", "shrinking", "transformation", "weight_gain", "furry", "bestiality", "giant", "giantess", "growth", "midget", "minigirl", "miniguy", "shrinking", "albino", "body_painting", "body_writing", "crotch_tattoo", "dark_skin", "freckles", "gyaru", "gyaruoh", "large_tattoo", "oil", "scar", "skinsuit", "sweating", "tanlines", "anorexic", "bbm", "bbw", "ssbbm", "ssbbw", "weight_gain", "beauty_mark", "brain_fuck", "cockslapping", "crown", "ear_fuck", "facesitting", "hairjob", "body_swap", "chloroform", "corruption", "drugs", "drunk", "emotionless_sex", "mind_break", "mind_control", "moral_degeneration", "parasite", "personality_excretion", "possession", "shared_senses", "sleeping", "blind", "blindfold", "closed_eyes", "crying", "cum_in_eye", "dark_sclera", "eye_penetration", "eyemask", "eyepatch", "glasses", "heterochromia", "monoeye", "sunglasses", "unusual_pupils", "nose_fuck", "nose_hook", "smell", "adventitious_mouth", "autofellatio", "ball_sucking", "big_lips", "blowjob", "blowjob_face", "braces", "burping", "coprophagia", "cunnilingus", "deepthroat", "double_blowjob", "foot_licking", "gag", "gokkun", "kissing", "long_tongue", "multimouth_blowjob", "piss_drinking", "rimjob", "saliva", "smoking", "tooth_brushing", "unusual_teeth", "vampire", "vomit", "vore", "asphyxiation", "collar", "hanging", "leash", "armpit_licking", "armpit_sex", "hairy_armpits", "fingering", "fisting", "gloves", "handjob", "multiple_handjob", "autopaizuri", "big_areolae", "big_breasts", "breast_expansion", "breast_feeding", "breast_reduction", "clothed_paizuri", "gigantic_breasts", "huge_breasts", "lactation", "milking", "multiple_breasts", "multiple_paizuri", "oppai_loli", "paizuri", "small_breasts", "big_nipples", "dark_nipples", "dicknipples", "inverted_nipples", "multiple_nipples", "nipple_birth", "nipple_expansion", "nipple_fuck", "nipple_stimulation", "cumflation", "inflation", "navel_fuck", "pregnant", "stomach_deformation", "bloomers", "chastity_belt", "crotch_tattoo", "diaper", "fundoshi", "gymshorts", "hairy", "hotpants", "mesuiki", "multiple_orgasms", "pantyjob", "pubic_stubble", "shimapan", "urethra_insertion", "adventitious_penis", "balls_expansion", "ball_sucking", "balljob", "big_balls", "big_penis", "cbt", "cloaca_insertion", "cockphagia", "cock_ring", "cockslapping", "cuntboy", "dick_growth", "dickgirl_on_male", "dickgirls_only", "frottage", "futanari", "horse_cock", "huge_penis", "multiple_penises", "penis_birth", "phimosis", "prostate_massage", "retractable_penis", "scrotal_lingerie", "shemale", "small_penis", "smegma", "adventitious_vagina", "big_clit", "big_vagina", "birth", "cervix_penetration", "cervix_prolapse", "clit_growth", "clit_insertion", "clit_stimulation", "cunnilingus", "cuntbusting", "defloration", "double_vaginal", "multiple_vaginas", "squirting", "strapon", "tribadism", "triple_vaginal", "unbirth", "vaginal_sticker", "anal", "anal_birth", "anal_intercourse", "analphagia", "anal_prolapse", "ass_expansion", "assjob", "big_ass", "double_anal", "enema", "farting", "multiple_assjob", "pegging", "rimjob", "scat", "spanking", "tail", "tail_plug", "tailphagia", "triple_anal", "eggs", "gaping", "large_insertions", "nakadashi", "prolapse", "sex_toys", "speculum", "unusual_insertions", "garter_belt", "kneepit_sex", "leg_lock", "legjob", "pantyhose", "stirrup_legwear", "stockings", "sumata", "denki_anma", "foot_insertion", "foot_licking", "footjob", "multiple_footjob", "sockjob", "stirrup_legwear", "thigh_high_boots", "animegao", "apparel_bukkake", "apron", "bandages", "bike_shorts", "bikini", "blindfold", "bloomers", "bodystocking", "bodysuit", "cock_ring", "collar", "condom", "corset", "cowgirl", "cowman", "crossdressing", "detached_sleeves", "diaper", "dougi", "exposed_clothing", "eyemask", "eyepatch", "fishnets", "fundoshi", "gag", "garter_belt", "gasmask", "glasses", "gothic_lolita", "gymshorts", "haigure", "headphones", "hijab", "hotpants", "hyena_boy", "hyena_girl", "kemonomimi", "kigurumi_pajama", "kimono", "kindergarten_uniform", "kunoichi", "lab_coat", "latex", "leash", "leotard", "lingerie", "living_clothes", "metal_armor", "miko", "military", "mouth_mask", "nazi", "nose_hook", "nudism", "pantyhose", "ponygirl", "race_queen", "randoseru", "sarashi", "schoolboy_uniform", "schoolgirl_uniform", "school_swimsuit", "scrotal_lingerie", "shimapan", "stirrup_legwear", "stockings", "straitjacket", "sundress", "sunglasses", "swimsuit", "thigh_high_boots", "tiara", "tights", "transparent_clothing", "vaginal_sticker", "wet_clothes", "bisexual", "double_anal", "double_blowjob", "double_vaginal", "fff_threesome", "ffm_threesome", "fft_threesome", "harem", "layer_cake", "mmf_threesome", "mmm_threesome", "mmt_threesome", "mtf_threesome", "multimouth_blowjob", "multiple_assjob", "multiple_footjob", "multiple_handjob", "multiple_paizuri", "multiple_straddling", "oyakodon", "shimaidon", "triple_anal", "triple_vaginal", "ttf_threesome", "ttm_threesome", "ttt_threesome", "all_the_way_through", "double_penetration", "triple_penetration", "blindfold", "clamp", "dakimakura", "gag", "glory_hole", "machine", "onahole", "pillory", "pole_dancing", "real_doll", "sex_toys", "speculum", "strapon", "syringe", "table_masturbation", "tail_plug", "tube", "unusual_insertions", "vacbed", "whip", "wooden_horse", "wormhole", "oil", "slime", "slime_boy", "slime_girl", "underwater", "blood", "crying", "lactation", "milking", "saliva", "squirting", "apparel_bukkake", "bukkake", "cum_bath", "cum_in_eye", "cum_swap", "cumflation", "giant_sperm", "gokkun", "nakadashi", "coprophagia", "internal_urination", "menstruation", "omorashi", "piss_drinking", "public_use", "scat", "scat_insertion", "sweating", "urination", "vomit", "chikan", "rape", "sleeping", "time_stop", "bdsm", "bodysuit", "blindfold", "clamp", "collar", "domination_loss", "femdom", "food_on_body", "forniphilia", "human_cattle", "josou_seme", "latex", "orgasm_denial", "petplay", "slave", "smalldom", "tickling", "bondage", "gag", "harness", "pillory", "ponygirl", "shibari", "straitjacket", "stuck_in_wall", "vacbed", "abortion", "blood", "cannibalism", "catfight", "cbt", "cuntbusting", "dismantling", "electric_shocks", "guro", "ryona", "snuff", "torture", "trampling", "whip", "exhibitionism", "filming", "forced_exposure", "hidden_sex", "humiliation", "voyeurism", "autofellatio", "autopaizuri", "clone", "masturbation", "phone_sex", "selfcest", "solo_action", "table_masturbation", "amputee", "blind", "handicapped", "mute", "absorption", "analphagia", "breast_feeding", "cannibalism", "cockphagia", "coprophagia", "gokkun", "piss_drinking", "tailphagia", "vore", "weight_gain", "cuntboy", "feminization", "futanari", "gender_change", "gender_morph", "otokofutanari", "shemale", "dickgirl_on_dickgirl", "dickgirl_on_male", "fft_threesome", "male_on_dickgirl", "mmt_threesome", "mtf_threesome", "ttf_threesome", "ttm_threesome", "blackmail", "defloration", "impregnation", "oyakodon", "prostitution", "shimaidon", "tomboy", "tomgirl", "virginity", "yaoi", "yuri", "dickgirls_only", "females_only", "males_only", "no_penetration", "nudity_only", "pussyboys_only", "sole_dickgirl", "sole_female", "sole_male", "sole_pussyboy", "cheating", "netorare", "swinging", "incest", "inseki", "low_bestiality", "low_guro", "low_lolicon", "low_scat", "low_shotacon", "low_smegma"
    };
}
