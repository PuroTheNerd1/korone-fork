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
    public bool ContainsCyrillic(string input)
    {
        Regex regex = new Regex(@"[\u0400-\u04FF]");
        return regex.IsMatch(input);
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
            "pornography"
        };
        //remove all spaces
        string nonWhitespaceInput;
        nonWhitespaceInput = String.Join("", input.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        foreach (string word in filteredWords)
        {
            //check if the chat msg contains one of the filtering words
            if (nonWhitespaceInput.ToLower().Contains(word))
            {
                //replace the string with # like roblox does
                input = new string('#', input.Length);
                break;
            }
        }
        return input;
    }
}
