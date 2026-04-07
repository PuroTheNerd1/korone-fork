using System.Globalization;
using System.Text;

namespace Roblox.Services;


public class FilterService : ServiceBase, IService
{
    private static readonly string[] filteredWords =
    {
        "anal",
        "anally",
        "anus",
        "ballsac",
        "pussy",
        "ballsack",
        "beastiality",
        "beastility",
        "bestiality",
        "blowjob",
        "blowjobs",
        "boner",
        "bitch",
        "boob",
        "boobies",
        "boobs",
        "breast",
        "breasts",
        "buttfuck",
        "buttfucker",
        "cock",
        "cockride",
        "cocks",
        "cocksuck",
        "cocksucked",
        "cocksucker",
        "cocksucking",
        "cocksucks",
        "condom",
        "condoms",
        "condo",
        "cum",
        "cummer ",
        "cumming",
        "cums",
        "cumshot",
        "cunilingus",
        "cunillingus",
        "cunnilingus",
        "dick",
        "dicks",
        "dildo",
        "dildos",
        "dildos",
        "digga",
        "ejaculate ",
        "ejaculated ",
        "ejaculates",
        "ejaculating",
        "ejaculatings",
        "ejaculation",
        "faget",
        "fagg",
        "fag",
        "fagget",
        "fagging",
        "faggit",
        "faggot",
        "fagot",
        "faggots",
        "faggs",
        "fagit",
        "fagot",
        "fagots",
        "fingerfuck",
        "fingerfucked",
        "fingerfucker",
        "fingerfuckers",
        "fingerfucking",
        "fingerfucks",
        "fistfuck",
        "fistfucked",
        "fistfucker",
        "fistfuckers",
        "fistfucking",
        "fistfuckings",
        "fistfucks",
        "gangbang",
        "gangbanged",
        "gangbangs",
        "gangbangs",
        "gaysex",
        "hardcoresex",
        "hitler",
        "horniest",
        "horny",
        "hotsex",
        "jackingoff",
        "jackoff",
        "jackxoff",
        "jerkxoff",
        "kidsinasanbox",
        "kkk",
        "masterbait",
        "masterbate",
        "masturbate",
        "molest",
        "mycock",
        "nazi",
        "nazis",
        "niger",
        "nigger",
        "niigger",
        "niggers",
        "niiggers",
        "ngga",
        "negger",
        "neckhurt",
        "nigga",
        "n0gga",
        "nhigga",
        "n8ggas",
        "niigga",
        "niga",
        "ni$$a",
        "ni$$as",
        "nude",
        "nudism",
        "nudist",
        "orgasim",
        "orgasims",
        "orgasm",
        "orgasms",
        "pern",
        "pecker",
        "pedo",
        "pedobear",
        "penis",
        "phonesex",
        "porn",
        "pron",
        "porno",
        "pornography",
        "goon",
        "pornos",
        "pren",
        "prostitute",
        "paygorn",
        "raip",
        "raiping",
        "rape",
        "raped",
        "raper",
        "raping",
        "rapist",
        "schlong",
        "sex",
        "sexx",
        "sexxx",
        "sexxy",
        "sexytiem",
        "sexytime",
        "slut",
        "sluts ",
        "sperm",
        "strip",
        "stripper",
        "stripperpole",
        "strippers",
        "swastika",
        "thong",
        "titties",
        "titty",
        "urcock",
        "vaggina",
        "vagina",
        "vegina",
        "vibrator",
        "wanker",
        "whore",
        "whorehouse",
        "yourcock",
        "femb",
        "fembx",
        "jerkingoff",
        "jerkoff",
        "jackoff",
        "jackingoff",
        "kys",
        "killyourself",
        "killurself",
        "retard"
     };
    private static readonly HashSet<string> _filteredWordsSet = new HashSet<string>(filteredWords);

    public bool IsTextFiltered(string input)
    {
        input = CleanText(input);
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        string cleanedInput = string.Join("", input.ToCharArray()
            .Where(c => !char.IsWhiteSpace(c))
            .Select(char.ToLower)
            .Select(c =>
            {
                switch (c)
                {
                    case '#': return '\0';
                    case '.': return '\0';
                    case '$': return 's';
                    case '@': return 'a';
                    case '!': return 'i';
                    case '0': return 'o';
                    case '*': return '\0';
                    case 'я': return 'r';
                    default: return c;
                }
            })
            .Where(c => c != '\0')
            .ToArray());

        return _filteredWordsSet.Any(word => cleanedInput.Contains(word));
    }

    public string FilterText(string input)
    {
        if (IsTextFiltered(input))
        {
            return new string('#', input.Length);
        }


        return CleanText(input);
    }

    public string CleanText(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string normalized = input.Normalize(NormalizationForm.FormKC);
        StringBuilder sb = new StringBuilder(normalized.Length);

        foreach (char c in normalized)
        {
            if (char.IsSurrogate(c))
            {
                sb.Append(c);
                continue;
            }

            var category = char.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.Format ||
                category == UnicodeCategory.Control ||
                category == UnicodeCategory.PrivateUse)
            {
                continue;
            }

            bool isAscii = c <= 0x007F; // English, numbers
            bool isLatinExtended = c >= 0x0080 && c <= 0x024F; // Covers Turkish French, German, etc.
            bool isCyrillic = c >= 0x0400 && c <= 0x052F; // Russian
            bool isJapanese = c >= 0x3040 && c <= 0x30FF; // Japanese
            bool isChinese = c >= 0x4E00 && c <= 0x9FFF; // Chinese 
            bool isPunctuationOrSymbol = c >= 0x2000 && c <= 0x2BFF; // Emojis, bullets etc

            if (isAscii || isLatinExtended || isCyrillic || isJapanese || isChinese || isPunctuationOrSymbol)
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public bool IsReusable()
    {
        return true;
    }

    public bool IsThreadSafe()
    {
        return true;
    }
}
