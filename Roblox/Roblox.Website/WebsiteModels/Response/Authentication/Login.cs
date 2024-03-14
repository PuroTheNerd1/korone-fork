namespace Roblox.Website.WebsiteModels.Authentication;
public class LoginRequestMobile
{
    public string username { get; set; } = "";
    public string password { get; set; } = "";
}
public class LoginRequest
{
    public string ctype { get; set; }
    public string cvalue { get; set; }
    public string password { get; set; }
}

