using System.Text.RegularExpressions;
using Dapper;
using Roblox.Dto.Economy;
using Roblox.Dto.Users;
using Roblox.Libraries.Exceptions;
using Roblox.Models.Assets;
using Roblox.Models.Economy;
using Roblox.Services.Exceptions;

namespace Roblox.Services;


public class FilterService : ServiceBase, IService
{
    public string FilterText(string input)
    {
        string[] filteredWords =
        {
            "anal",
            "anally",
            "anus",
            "ballsac",
            "ballsack",
            "beastiality",
            "beastility",
            "bestiality",
            "blowjob",
            "blowjobs",
            "boner",
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
            "dildo ",
            "dildos",
            "dildos",
            "ejaculate ",
            "ejaculated ",
            "ejaculates",
            "ejaculating",
            "ejaculatings",
            "ejaculation",
            "faget",
            "fagg",
            "fagget",
            "fagging",
            "faggit",
            "faggot",
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
            "niger ",
            "nigger",
            "niggers",
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
            "porno",
            "pornography",
            "pornos",
            "pren",
            "prostitute",
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
            "yourcock"
        };
        //remove all spaces
        string cleanedInput = String.Join("", input.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries))
        .ToLower()
        // Will prevent bypassing chat filter with words like n!gga
        .Replace("#", "")
        .Replace("$", "")
        .Replace("!", "i")
        .Replace("*", "");
        foreach (string word in filteredWords)
        {
            //check if the chat msg contains one of the filtering words
            if (cleanedInput.Contains(word))
            {
                //replace the string with # like roblox does
                input = new string('#', input.Length);
                break;
            }
        }
        return input;
    }
    public bool IsReusable()
    {
        throw new NotImplementedException();
    }

    public bool IsThreadSafe()
    {
        throw new NotImplementedException();
    }
}
