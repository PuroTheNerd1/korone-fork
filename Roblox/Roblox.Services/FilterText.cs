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
    public bool IsReusable()
    {
        throw new NotImplementedException();
    }

    public bool IsThreadSafe()
    {
        throw new NotImplementedException();
    }
    public string FilterText(string input)
    {
        string[] filteredWords =
        {
            "nigger",
            "nigga",
            "1488",
            "nazi",
            "sex",
            "cock",
            "vagina",
            "penis",
            "breasts",
            "tits",
            "dildo",
            "masturbation",
            "blowjob",
            "ejaculation",
            "fetish",
            "orgasm",
            "rape",
            "cum",
            "porn",
            "pornography",
            "whore",
            "prostitute",
            "erotic",
            "submissive",
            "dominant",
            "fleshlight",
            "fucking",
            "kink",
            "molest",
            "footjob",
            "boobjob",
            "assjob",
            "facefuck",
            "cunnilingus",
            "creampie",
            "orgy",
            "milf",
            "slut",
            "peg",
            "nipple",
            "pornstar"
        };
        //remove all spaces
        string nonWhitespaceInput;
        nonWhitespaceInput = String.Join("", input.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries)).ToLower();
        foreach (string word in filteredWords)
        {
            //check if the chat msg contains one of the filtering words
            if (nonWhitespaceInput.Contains(word))
            {
                //replace the string with # like roblox does
                input = new string('#', input.Length);
                break;
            }
        }
        return input;
    }
}
