using System;
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
        if (mode == NSFW_MODE.Enabled)
            return true;
        var lower = text.ToLower();
        for (var i = 0; i < BadWords.Length; i++)
        {
            if (lower.Contains(BadWords[i]))
                return false;
        }
        return true;
    }

    public static bool NotAllowed(string text) => !Allowed(text);

    private static readonly string[] BadWords = {
        "2g1c", "2_girls_1_cup", "acrotomophilia", "alabama_hot_pocket", "alaskan_pipeline", "anal", "anilingus", "anus", "apeshit", "arsehole", "ass", "asshole", "assmunch", "auto_erotic", "autoerotic", "babeland", "baby_batter", "baby_juice", "ball_gag", "ball_gravy", "ball_kicking", "ball_licking", "ball_sack", "ball_sucking", "bangbros", "bangbus", "bareback", "barely_legal", "barenaked", "bastard", "bastardo", "bastinado", "bbw", "bdsm", "beaner", "beaners", "beaver_cleaver", "beaver_lips", "beastiality", "bestiality", "big_black", "big_breasts", "big_knockers", "big_tits", "bimbos", "birdlock", "bitch", "bitches", "black_cock", "blonde_action", "blonde_on_blonde_action", "blowjob", "blow_job", "blow_your_load", "blue_waffle", "blumpkin", "bollocks", "bondage", "boner", "boob", "boobs", "booty_call", "brown_showers", "brunette_action", "bukkake", "bulldyke", "bullet_vibe", "bullshit", "bung_hole", "bunghole", "busty", "butt", "buttcheeks", "butthole", "camel_toe", "camgirl", "camslut", "camwhore", "carpet_muncher", "carpetmuncher", "chocolate_rosebuds", "cialis", "circlejerk", "cleveland_steamer", "clit", "clitoris", "clover_clamps", "clusterfuck", "cock", "cocks", "coprolagnia", "coprophilia", "cornhole", "coon", "coons", "creampie", "cum", "cumming", "cumshot", "cumshots", "cunnilingus", "cunt", "darkie", "date_rape", "daterape", "deep_throat", "deepthroat", "dendrophilia", "dick", "dildo", "dingleberry", "dingleberries", "dirty_pillows", "dirty_sanchez", "doggie_style", "doggiestyle", "doggy_style", "doggystyle", "dog_style", "dolcett", "domination", "dominatrix", "dommes", "donkey_punch", "double_dong", "double_penetration", "dp_action", "dry_hump", "dvda", "eat_my_ass", "ecchi", "ejaculation", "erotic", "erotism", "escort", "eunuch", "fag", "faggot", "fecal", "felch", "fellatio", "feltch", "female_squirting", "femdom", "figging", "fingerbang", "fingering", "fisting", "foot_fetish", "footjob", "frotting", "fuck", "fuck_buttons", "fuckin", "fucking", "fucktards", "fudge_packer", "fudgepacker", "futanari", "gangbang", "gang_bang", "gay_sex", "genitals", "giant_cock", "girl_on", "girl_on_top", "girls_gone_wild", "goatcx", "goatse", "god_damn", "gokkun", "golden_shower", "goodpoop", "goo_girl", "goregasm", "grope", "group_sex", "g-spot", "guro", "hand_job", "handjob", "hard_core", "hardcore", "hentai", "homoerotic", "honkey", "hooker", "horny", "hot_carl", "hot_chick", "how_to_kill", "how_to_murder", "huge_fat", "humping", "incest", "intercourse", "jack_off", "jail_bait", "jailbait", "jelly_donut", "jerk_off", "jigaboo", "jiggaboo", "jiggerboo", "jizz", "juggs", "kike", "kinbaku", "kinkster", "kinky", "knobbing", "leather_restraint", "leather_straight_jacket", "lemon_party", "livesex", "lolita", "lovemaking", "make_me_come", "male_squirting", "masturbate", "masturbating", "masturbation", "menage_a_trois", "milf", "missionary_position", "mong", "motherfucker", "mound_of_venus", "mr_hands", "muff_diver", "muffdiving", "nambla", "nawashi", "negro", "neonazi", "nigga", "nigger", "nig_nog", "nimphomania", "nipple", "nipples", "nsfw", "nsfw_images", "nude", "nudity", "nutten", "nympho", "nymphomania", "octopussy", "omorashi", "one_cup_two_girls", "one_guy_one_jar", "orgasm", "orgy", "paedophile", "paki", "panties", "panty", "pedobear", "pedophile", "pegging", "penis", "phone_sex", "piece_of_shit", "pikey", "pissing", "piss_pig", "pisspig", "playboy", "pleasure_chest", "pole_smoker", "ponyplay", "poof", "poon", "poontang", "punany", "poop_chute", "poopchute", "porn", "porno", "pornography", "prince_albert_piercing", "pthc", "pubes", "pussy", "queaf", "queef", "quim", "raghead", "raging_boner", "rape", "raping", "rapist", "rectum", "reverse_cowgirl", "rimjob", "rimming", "rosy_palm", "rosy_palm_and_her_5_sisters", "rusty_trombone", "sadism", "santorum", "scat", "schlong", "scissoring", "semen", "sex", "sexcam", "sexo", "sexy", "sexual", "sexually", "sexuality", "shaved_beaver", "shaved_pussy", "shemale", "shibari", "shit", "shitblimp", "shitty", "shota", "shrimping", "skeet", "slanteye", "slut", "s&m", "smut", "snatch", "snowballing", "sodomize", "sodomy", "spastic", "spic", "splooge", "splooge_moose", "spooge", "spread_legs", "spunk", "strap_on", "strapon", "strappado", "strip_club", "style_doggy", "suck", "sucks", "suicide_girls", "sultry_women", "swastika", "swinger", "tainted_love", "taste_my", "tea_bagging", "threesome", "throating", "thumbzilla", "tied_up", "tight_white", "tit", "tits", "titties", "titty", "tongue_in_a", "topless", "tosser", "towelhead", "tranny", "tribadism", "tub_girl", "tubgirl", "tushy", "twat", "twink", "twinkie", "two_girls_one_cup", "undressing", "upskirt", "urethra_play", "urophilia", "vagina", "venus_mound", "viagra", "vibrator", "violet_wand", "vorarephilia", "voyeur", "voyeurweb", "voyuer", "vulva", "wank", "wetback", "wet_dream", "white_power", "whore", "worldsex", "wrapping_men", "wrinkled_starfish", "xx", "xxx", "yaoi", "yellow_showers", "yiffy", "zoophilia", "🖕",
        "baiser", "bander", "bigornette", "bite", "bitte", "bloblos", "bordel", "bourré", "bourrée", "brackmard", "branlage", "branler", "branlette", "branleur", "branleuse", "brouter_le_cresson", "caca", "chatte", "chiasse", "chier", "chiottes", "clito", "clitoris", "con", "connard", "connasse", "conne", "couilles", "cramouille", "cul", "déconne", "déconner", "emmerdant", "emmerder", "emmerdeur", "emmerdeuse", "enculé", "enculée", "enculeur", "enculeurs", "enfoiré", "enfoirée", "étron", "fille_de_pute", "fils_de_pute", "folle", "foutre", "gerbe", "gerber", "gouine", "grande_folle", "grogniasse", "gueule", "jouir", "la_putain_de_ta_mère", "MALPT", "ménage_à_trois", "merde", "merdeuse", "merdeux", "meuf", "nègre", "negro", "nique_ta_mère", "nique_ta_race", "palucher", "pédale", "pédé", "péter", "pipi", "pisser", "pouffiasse", "pousse-crotte", "putain", "pute", "ramoner", "sac_à_foutre", "sac_à_merde", "salaud", "salope", "suce", "tapette", "tanche", "teuch", "tringler", "trique", "troncher", "trou_du_cul", "turlute", "zigounette", "zizi",
        "analritter", "arsch", "arschficker", "arschlecker", "arschloch", "bimbo", "bratze", "bumsen", "bonze", "dödel", "fick", "ficken", "flittchen", "fotze", "fratze", "hackfresse", "hure", "hurensohn", "ische", "kackbratze", "kacke", "kacken", "kackwurst", "kampflesbe", "kanake", "kimme", "lümmel", "MILF", "möpse", "morgenlatte", "möse", "mufti", "muschi", "nackt", "neger", "nigger", "nippel", "nutte", "onanieren", "orgasmus", "penis", "pimmel", "pimpern", "pinkeln", "pissen", "pisser", "popel", "poppen", "porno", "reudig", "rosette", "schabracke", "schlampe", "scheiße", "scheisser", "schiesser", "schnackeln", "schwanzlutscher", "schwuchtel", "tittchen", "titten", "vögeln", "vollpfosten", "wichse", "wichsen", "wichser",
    };
}
