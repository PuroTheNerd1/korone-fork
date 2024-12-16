using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using System.Dynamic;

namespace Roblox.Rendering
{
    public class RenderingHandler
    {
        private static string BaseUrl = "";
        public static string LuaScriptPath = "";
        public static string RccServicePath = "C:\\ProjectX\\services\\RCCService\\";
        public static string RccServicePathGames = "C:\\ProjectX\\services\\RCCService\\";
        private static Random RandomComponent = new Random();
        private static HttpClient client = new HttpClient();
        // TODO: REWRITE RENDERING HANDLER
        private enum RenderType
        {
            Avatar = 0,
            Headshot,
            Head,
            Package,
            Image,
            Clothing,
            Face,
            Mesh,
            Hat,
            Place,

        }
        private class RenderResponse
        {
            public bool success { get; set; }
            public string message { get; set; }
            public string data { get; set; }
        }
        public static void Configure(string baseUrl, string rccPath, string luaScriptPath, string rccPathGames)
        {
            BaseUrl = baseUrl;
            RccServicePath = rccPath;
            LuaScriptPath = luaScriptPath;
        }

        private static async Task<dynamic> SendRenderRequest(long id, RenderType type, int? x = 0, int? y = 0, bool? isFace = false)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string url = "";
            // Hacky asf
            dynamic renderRequest = new ExpandoObject();

            switch (type)
            {
                case RenderType.Avatar:
                    renderRequest.userId = id;
                    url = "player/thumbnail";
                    break;
                case RenderType.Headshot:
                    renderRequest.userId = id;
                    url = "player/headshot";
                    break;
                case RenderType.Package:
                    url = "catalog/package";
                    break;
                case RenderType.Head:
                    renderRequest.assetId = id;
                    url = "catalog/head";
                    break;
                case RenderType.Image:
                    renderRequest.assetId = id;
                    renderRequest.isFace = isFace;
                    url = "image/image";
                    break;
                case RenderType.Clothing:
                    renderRequest.assetId = id;
                    url = "image/clothing";
                    break;
                case RenderType.Mesh:
                    renderRequest.assetId = id;
                    url = "catalog/mesh";
                    break;
                case RenderType.Hat:
                    renderRequest.assetId = id;
                    url = "catalog/hat";
                    break;
                case RenderType.Place:
                    renderRequest.placeId = id;
                    renderRequest.x = x;
                    renderRequest.y = y;
                    url = "game/thumbnail";
                    break;
            }
            // i will add error handling to this later
            var content = new StringContent(JsonSerializer.Serialize(renderRequest), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync("http://localhost:3043/" + url, content);
            sw.Stop();
            var request = await response.Content.ReadFromJsonAsync<RenderResponse>();
            Console.WriteLine($"[RenderingHandler] Request took {sw.ElapsedMilliseconds}ms");
            return request?.data ?? "FAILURE";
        }


        private static async Task<string> SendRequestToRcc(string URL, string XML, string SOAPAction)
        {
            using (HttpClient RccHttpClient = new HttpClient())
            {
                RccHttpClient.DefaultRequestHeaders.Add("SOAPAction", $"http://pekora.zip/{SOAPAction}");
                HttpContent XMLContent = new StringContent(XML, Encoding.UTF8, "text/xml");
                try
                {
                    HttpResponseMessage RccHttpClientPost = await RccHttpClient.PostAsync(URL, XMLContent);
                    string RccHttpClientResponse = await RccHttpClientPost.Content.ReadAsStringAsync();
                    if (!RccHttpClientPost.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[RCCSendRequest] Recieved not OK status request: {RccHttpClientPost.StatusCode}, full response: {RccHttpClientResponse}");
                    }

                    XDocument Doc = XDocument.Parse(RccHttpClientResponse);
                    XNamespace ns1 = "http://pekora.zip/";
                    XElement Element = Doc.Descendants(ns1 + "value").FirstOrDefault()!;
                    string LuaValue = Element.Value ?? "";
                    return LuaValue;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[RCCSendRequest] Failed to send request to RCC: {e}");
                }
            }
            return "FAILURE";
        }

        public static async Task<string> RequestHatThumbnail(long assetId, int JobExpiration)
        {
            return await SendRenderRequest(assetId, RenderType.Hat);
        }

        public static async Task<string> RequestMeshThumbnail(long assetId, int JobExpiration)
        {
            return await SendRenderRequest(assetId, RenderType.Mesh);
        }

        public static async Task<string> RequestImageThumbnail(long assetId, int JobExpiration, bool isFace = false)
        {
            return await SendRenderRequest(assetId, RenderType.Image, isFace: isFace);
        }

        public static async Task<string> RequestPlaceRender(long assetId, int JobExpiration, int x, int y)
        {
            return await SendRenderRequest(assetId, RenderType.Place, x, y);
        }

        public static async Task<string> RequestClothingRender(long assetId, int JobExpiration)
        {
            return await SendRenderRequest(assetId, RenderType.Clothing);
        }

        public static async Task<string> RequestHeadRender(long assetId, int JobExpiration)
        {
            return await SendRenderRequest(assetId, RenderType.Head);
        }

        public static async Task<string> RequestPackageRender(string assetUrls, int JobExpiration)
        {
            int RCCPort = RandomComponent.Next(10003, 25000);
            Process renderRcc = new Process();
            renderRcc.StartInfo.UseShellExecute = false;
            renderRcc.StartInfo.CreateNoWindow = true;
            renderRcc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            renderRcc.StartInfo.FileName = $"{RccServicePath}\\RCCService2020\\RCCService.exe";
            renderRcc.StartInfo.Arguments = string.Format($@"-console -verbose -port {RCCPort}");
            renderRcc.StartInfo.RedirectStandardError = false;
            renderRcc.StartInfo.RedirectStandardOutput = false;
            renderRcc.StartInfo.UseShellExecute = false;
            renderRcc.StartInfo.CreateNoWindow = true;
            renderRcc.Start();
            string originalScript = File.ReadAllText($"{LuaScriptPath}\\NewRenderJSON\\Package.txt");
            string finalScript = originalScript.Replace
                ("%assetUrls%", $@"""{assetUrls}""").Replace
                ("%fileExtension%", $@"""png""").Replace
                ("%x%", $@"""{1680}""").Replace
                ("%y%", $@"""{1680}""").Replace
                ("%baseUrl%", $@"""{BaseUrl}/""").Replace
                ("%RigURL%", $@"""{BaseUrl}/asset/?id=1785197""");


            string XML = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                    <BatchJobEx xmlns=""http://pekora.zip/"">
                        <job>
                            <id>{Guid.NewGuid().ToString()}</id>
                            <category>1</category>
                            <cores>1</cores>
                            <expirationInSeconds>{JobExpiration}</expirationInSeconds>
                        </job>
                        <script>
                            <name>{Guid.NewGuid().ToString()}</name>
                            <script>
                                <![CDATA[
                                {finalScript}
                                ]]>
                            </script>
                        </script>
                    </BatchJobEx>
                </soap:Body>
            </soap:Envelope>";
            await WaitForPort(RCCPort);
            string result = await SendRequestToRcc($"http://127.0.0.1:{RCCPort}", XML, "BatchJobEx");
            renderRcc.Kill();
            return result;
        }
        public static async Task<string> RequestPlayerThumbnail(long userId, int JobExpiration)
        {
            return await SendRenderRequest(userId, RenderType.Avatar);
        }
        public static async Task<string> RequestHeadshotThumbnail(long userId, int JobExpiration)
        {
            return await SendRenderRequest(userId, RenderType.Headshot);
        }
        static Task WaitForPort(int RCCPort)
        {
            while (true)
            {
                try
                {
                    using (TcpClient client = new TcpClient())
                    {
                        client.Connect(IPAddress.Parse("127.0.0.1"), RCCPort);
                        Console.WriteLine("did not find port");
                        break;
                    }
                }
                catch (SocketException)
                {
                    Thread.Sleep(0);
                }
            }
            Console.WriteLine($"found port: {RCCPort}");
            return Task.CompletedTask;
        }
    }
}