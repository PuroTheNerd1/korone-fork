using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Roblox.Website.Controllers.Internal
{
    public class SignatureController : ControllerBase
    {
        private static RSACryptoServiceProvider? _rsaCsp;
        private static SHA1? _shaCsp;
        private static RSACryptoServiceProvider rsa = new();
        private static RSACryptoServiceProvider rsa2048 = new();
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
                rsa2048.ImportFromPem(System.IO.File.ReadAllText("PEM\\PrivateKey.pem"));

            }
            catch (Exception ex)
            {
                throw new Exception("Error setting up SignatureController: " + ex.Message);
            }
        }

        public static string SignJsonResponseForClientFromPrivateKey(dynamic JSONToSign)
        {
            string format = "--rbxsig%{0}%{1}";

            string json = JsonConvert.SerializeObject(JSONToSign);
            string script = Environment.NewLine + json;
            byte[] signature = _rsaCsp!.SignData(Encoding.Default.GetBytes(script), _shaCsp!);

            return String.Format(format, Convert.ToBase64String(signature), script);
        }
        public static string SignStringResponseForClientFromPrivateKey(string stringToSign, bool bUseRbxSig = false)
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
        public static string SignJson2048(dynamic JSONToSign)
        {
            string script = newLine + JsonConvert.SerializeObject(JSONToSign);
            byte[] signature = rsa2048.SignData(Encoding.Default.GetBytes(script), SHA1.Create());

            return string.Format(format2048, Convert.ToBase64String(signature), script);
        }
        public static string SignString2048(string stringToSign, bool bUseRbxSig = false)
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
        public static string GenerateClientTicketV1(long userId, string username, string jobId, string characterAppearanceUrl)
        {
            DateTime currentUtcDateTime = DateTime.UtcNow;
            string formattedDateTime = currentUtcDateTime.ToString("M/d/yyyy h:mm:ss tt");
            string cticket = $"{userId}\n{jobId}\n{formattedDateTime}";
            string ticketSignature = SignStringResponseForClientFromPrivateKey(cticket);
                
            string ticket2 = $"{userId}\n{username}\n{characterAppearanceUrl}\n{jobId}\n{formattedDateTime}";
            string ticketSignature2 = SignStringResponseForClientFromPrivateKey(ticket2);

            string finalTicket = $"{formattedDateTime};{ticketSignature2};{ticketSignature}";
            return finalTicket;
        }
        public static string GenerateClientTicketV3(long userId, string username, string jobId, string dateTime)
        {
            // the second userid is meant to be characterAppearanceId
            string ticket2 = $"{userId}\n{username}\n{userId}\n{jobId}\n{dateTime}";
            string ticket2Signature = SignString2048(ticket2);
            string ticket = $"{userId}\n{jobId}\n{dateTime}";
            string ticketSignature = SignString2048(ticket);
            // Final ticket
            string finalTicket = $"{dateTime};{ticket2Signature};{ticketSignature};3";
            return finalTicket;
        }
        public static string GenerateClientTicketV4(long userId, string username, string jobId, string dateTime, long accountAgeDays, long placeId)
        {
            string characterAppearanceUrl = $"{Configuration.BaseUrl}/v1/avatar-fetch?userId={userId}&placeId={placeId}";
            string membershipType = "Premium";
            string countryCode = "US";
            string ticket2 = $"{userId}\n{username}\n{characterAppearanceUrl}\n{jobId}\n{dateTime}";
            string ticket2Signature = SignString2048(ticket2);
            string ticket = $"{dateTime}\n{jobId}\n{userId}\n{userId}\n0\n{accountAgeDays}\nf\n{username.Length}\n{username}\n{membershipType.Length}\n{membershipType}\n{countryCode.Length}\n{countryCode}\n0\n\n{username.Length}\n{username}";
            string ticketSignature = SignString2048(ticket);

            string finalTicket = $"{dateTime};{ticket2Signature};{ticketSignature};4";
            return finalTicket;
        }
    }
}