using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Roblox.Services;

public class FilterService : ServiceBase, IService
{
    private static readonly string[] baseFilteredWords =
    {
        "anal", "anally", "anus", "ballsac", "pussy", "ballsack", "beastiality",
        "beastility", "bestiality", "blowjob", "blowjobs", "boner", "bitch",
        "boob", "boobies", "boobs", "breast", "breasts", "buttfuck",
        "buttfucker", "cock", "cockride", "cocks", "cocksuck", "cocksucked",
        "cocksucker", "cocksucking", "cocksucks", "condom", "condoms", "condo",
        "cum", "cummer", "cumming", "cums", "cumshot", "cunilingus", "cunillingus",
        "cunnilingus", "dick", "dicks", "dildo", "dildos", "digga", "ejaculate",
        "ejaculated", "ejaculates", "ejaculating", "ejaculatings", "ejaculation",
        "faget", "fagg", "fag", "fagget", "fagging", "faggit", "faggot", "fagot",
        "faggots", "faggs", "fagit", "fagots", "fingerfuck", "fingerfucked",
        "fingerfucker", "fingerfuckers", "fingerfucking", "fingerfucks", "fistfuck",
        "fistfucked", "fistfucker", "fistfuckers", "fistfucking", "fistfuckings",
        "fistfucks", "gangbang", "gangbanged", "gangbangs", "gaysex", "hardcoresex",
        "hitler", "horniest", "horny", "hotsex", "jackingoff", "jackoff", "jackxoff",
        "jerkxoff", "kidsinasanbox", "kkk", "masterbait", "masterbate", "masturbate",
        "molest", "mycock", "nazi", "nazis", "niger", "nigger", "niigger", "niggers",
        "niiggers", "ngga", "negger", "neckhurt", "nigga", "n0gga", "nhigga", "n8ggas",
        "niigba", "niga", "nude", "nudism", "nudist", "orgasim", "orgasims", "orgasm",
        "orgasms", "pern", "pecker", "pedo", "pedobear", "penis", "phonesex", "porn",
        "pron", "porno", "pornography", "goon", "pornos", "pren", "prostitute",
        "paygorn", "raip", "raiping", "rape", "raped", "raper", "raping", "rapist",
        "schlong", "sex", "sexx", "sexxx", "sexxy", "sexytiem", "sexytime", "slut",
        "sluts", "sperm", "strip", "stripper", "stripperpole", "strippers", "swastika",
        "thong", "titties", "titty", "urcock", "vaggina", "vagina", "vegina",
        "vibrator", "wanker", "whore", "whorehouse", "yourcock", "femb", "fembx",
        "jerkingoff", "jerkoff", "kys", "killyourself", "killurself", "retard", "nigg"
    };

    private static readonly string[] canonicalFilteredWords;

    static FilterService()
    {
        var canonicalSet = new HashSet<string>();
        foreach (var word in baseFilteredWords)
        {
            string canonicalWord = GetCanonicalText(word);
            if (!string.IsNullOrEmpty(canonicalWord))
            {
                canonicalSet.Add(canonicalWord);
            }
        }
        canonicalFilteredWords = canonicalSet.ToArray();
        System.Array.Sort(canonicalFilteredWords);
    }

    public bool IsTextFiltered(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        char[] buffer = input.ToCharArray();
        for (int i = 0; i < buffer.Length; i++)
        {
            if (char.IsWhiteSpace(buffer[i]) || char.IsPunctuation(buffer[i]) ||
                char.IsSeparator(buffer[i]) || char.IsSymbol(buffer[i]))
            {
                buffer[i] = ' ';
            }
        }

        var words = new string(buffer).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            string canonicalWord = GetCanonicalText(word);

            if (string.IsNullOrEmpty(canonicalWord)) continue;

            if (System.Array.BinarySearch(canonicalFilteredWords, canonicalWord) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    public string FilterText(string input)
    {
        if (IsTextFiltered(input))
        {
            return new string('#', input.Length);
        }

        return CleanText(input);
    }

    private static string GetCanonicalText(string input)
    {
        string normalized = CleanText(input).ToLowerInvariant();
        StringBuilder sb = new StringBuilder(normalized.Length);
        char lastChar = '\0';

        foreach (char c in normalized)
        {
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c) || char.IsSeparator(c))
            {
                continue;
            }

            char mappedChar = c;

            switch (c)
            {
                case '$': case '5': case 'z': mappedChar = 's'; break;
                case '@': case '4': mappedChar = 'a'; break;
                case '!': case '1': case 'l': case '|': mappedChar = 'i'; break;
                case '0': mappedChar = 'o'; break;
                case '3': mappedChar = 'e'; break;
                case '7': mappedChar = 't'; break;
                case '8': mappedChar = 'b'; break;
                case 'я': mappedChar = 'r'; break;
                case 'v': mappedChar = 'u'; break;
                case 'k': mappedChar = 'c'; break;
            }

            if (mappedChar != lastChar)
            {
                sb.Append(mappedChar);
                lastChar = mappedChar;
            }
        }

        return sb.ToString();
    }

    public static string CleanText(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

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

            bool isAscii = c <= 0x007F;
            bool isLatinExtended = c >= 0x0080 && c <= 0x024F;
            bool isCyrillic = c >= 0x0400 && c <= 0x052F;
            bool isJapanese = c >= 0x3040 && c <= 0x30FF;
            bool isChinese = c >= 0x4E00 && c <= 0x9FFF;
            bool isPunctuationOrSymbol = c >= 0x2000 && c <= 0x2BFF;

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