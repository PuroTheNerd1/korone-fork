using Newtonsoft.Json;
using Roblox;
using Roblox.Dto.Games;
using Roblox.Models.GameServer;
using Roblox.Services;
using System.Security.Cryptography;
using System.Text;
namespace Roblox.Services.Signer;
public class SignService : ServiceBase
{
    private static RSACryptoServiceProvider? _rsaCsp;
    private static SHA1? _shaCsp;
    private static RSACryptoServiceProvider rsa = new();
    private static RSACryptoServiceProvider rsa2048 = new();
    private static RSACryptoServiceProvider rsa2048New = new();
    private static readonly string newLine = "\r\n";
    private static readonly string format = "--rbxsig%{0}%{1}";
    private static readonly string format2048 = "--rbxsig2%{0}%{1}";

    public static void Setup()
    {
        try
        {
            byte[] privateKeyBlob = Convert.FromBase64String(System.IO.File.ReadAllText("PrivateKeyBlob.txt"));

            _shaCsp = SHA1.Create();
            _rsaCsp = new RSACryptoServiceProvider();

            _rsaCsp.ImportCspBlob(privateKeyBlob);
            rsa2048.ImportFromPem(System.IO.File.ReadAllText(@"PEM\PrivateKey2048.pem"));
            rsa2048New.ImportFromPem(System.IO.File.ReadAllText(@"PEM\2020\privatekey.pem"));
        }
        catch (Exception ex)
        {
            throw new Exception("Error setting up SignatureController: " + ex.Message);
        }
    }

    public string SignJsonResponseForClientFromPrivateKey(dynamic JSONToSign)
    {
        string format = "--rbxsig%{0}%{1}";

        string json = JsonConvert.SerializeObject(JSONToSign);
        string script = Environment.NewLine + json;
        byte[] signature = _rsaCsp!.SignData(Encoding.Default.GetBytes(script), _shaCsp!);

        return String.Format(format, Convert.ToBase64String(signature), script);
    }
    public string SignStringResponseForClientFromPrivateKey(string stringToSign, bool bUseRbxSig = false)
    {
        if (bUseRbxSig)
        {
            string format = "--rbxsig%{0}%{1}";

            byte[] signature = _rsaCsp!.SignData(Encoding.Default.GetBytes(stringToSign), _shaCsp!);
            string script = Environment.NewLine + stringToSign;

            return String.Format(format, Convert.ToBase64String(signature), script);
        }
        else
        {
            byte[] signature = _rsaCsp!.SignData(Encoding.Default.GetBytes(stringToSign), _shaCsp!);
            return Convert.ToBase64String(signature);
        }
    }
    public string SignJson2048(dynamic JSONToSign)
    {
        string script = newLine + JsonConvert.SerializeObject(JSONToSign);
        byte[] signature = rsa2048.SignData(Encoding.Default.GetBytes(script), SHA1.Create());

        return string.Format(format2048, Convert.ToBase64String(signature), script);
    }
    public string SignString2048(string stringToSign, bool bUseRbxSig = false)
    {
        if (bUseRbxSig)
        {
            string script = newLine + stringToSign;
            byte[] signature = rsa.SignData(Encoding.Default.GetBytes(script), SHA1.Create());

            return string.Format(format, Convert.ToBase64String(signature), script);
        }
        else
        {
            byte[] signature = rsa2048.SignData(Encoding.Default.GetBytes(stringToSign), SHA1.Create());
            return Convert.ToBase64String(signature);
        }
    }
    public string SignJson2048New(dynamic JSONToSign)
    {
        string script = newLine + JsonConvert.SerializeObject(JSONToSign);
        byte[] signature = rsa2048New.SignData(Encoding.Default.GetBytes(script), SHA1.Create());

        return string.Format(format2048, Convert.ToBase64String(signature), script);
    }

    public string SignString2048New(string stringToSign, bool bUseRbxSig = false)
    {
        if (bUseRbxSig)
        {
            string script = newLine + stringToSign;
            byte[] signature = rsa.SignData(Encoding.Default.GetBytes(script), SHA1.Create());

            return string.Format(format, Convert.ToBase64String(signature), script);
        }
        else
        {
            byte[] signature = rsa2048New.SignData(Encoding.Default.GetBytes(stringToSign), SHA1.Create());
            return Convert.ToBase64String(signature);
        }
    }

    public string GenerateClientTicket(long year, long userId, string username, string jobId, string? membership, long? accountAgeDays, long placeId)
    {
        DateTime currentUtcDateTime = DateTime.UtcNow;
        string formattedDateTime = currentUtcDateTime.ToString("M/d/yyyy h:mm:ss tt");
        string characterAppearanceUrl = $"{Configuration.BaseUrl}/Asset/CharacterFetch.ashx?userId={userId}";

        switch (year)
        {
            case 2016:
            case 2017:
                if (year == 2017){
                    characterAppearanceUrl = $"{Configuration.BaseUrl}/v1.1/avatar-fetch?userId={userId}";
                }
                return GenerateV1Ticket(userId, username, jobId, formattedDateTime, characterAppearanceUrl);
            case 2018:
                return GenerateV2Ticket(userId, username, jobId, formattedDateTime);
            case 2020:
            case 2021:
                return GenerateV4Ticket(userId, username, jobId, membership, accountAgeDays, formattedDateTime);

            default:
                throw new NotImplementedException("Year does not exist");
        }
    }

    private string GenerateV1Ticket(long userId, string username, string jobId, string formattedDateTime, string characterAppearanceUrl)
    {
        string cticket = $"{userId}\n{jobId}\n{formattedDateTime}";
        string ticketSignature = SignStringResponseForClientFromPrivateKey(cticket);

        string ticket2 = $"{userId}\n{username}\n{characterAppearanceUrl}\n{jobId}\n{formattedDateTime}";
        string ticketSignature2 = SignStringResponseForClientFromPrivateKey(ticket2);

        return $"{formattedDateTime};{ticketSignature2};{ticketSignature};v1";
    }

    private string GenerateV2Ticket(long userId, string username, string jobId, string formattedDateTime)
    {
        string cticket = $"{userId}\n{jobId}\n{formattedDateTime}";
        string ticketSignature = SignString2048(cticket);

        string ticket2 = $"{userId}\n{username}\n{userId}\n{jobId}\n{formattedDateTime}";
        string ticketSignature2 = SignString2048(ticket2);

        return $"{formattedDateTime};{ticketSignature2};{ticketSignature};v2";
    }

    private string GenerateV4Ticket(long userId, string username, string jobId, string? membership, long? accountAgeDays, string formattedDateTime)
    {
        string countryCode = "US";
        string ticket2 = $"{userId}\n{username}\n{$"{Configuration.BaseUrl}/v1.1/avatar-fetch?userId={userId}"};{userId}\n{jobId}\n{formattedDateTime}";
        string ticketSignature2 = SignString2048New(ticket2);

        string cticket = $"{formattedDateTime}\n{jobId}\n{userId}\n{userId}\n0\n{accountAgeDays}\nf\n{username.Length}\n{username}\n{membership?.Length ?? 0}\n{membership ?? string.Empty}\n{countryCode.Length}\n{countryCode}\n0\n\n{username.Length}\n{username}";
        string ticketSignature = SignString2048New(cticket);

        return $"{formattedDateTime};{ticketSignature2};{ticketSignature};v4";
    }
}