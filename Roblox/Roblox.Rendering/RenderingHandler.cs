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
            Model,
            Emote,
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

        private static async Task<dynamic> SendRenderRequest(long id, RenderType type, int? x = 0, int? y = 0, bool? isFace = false,  string? assetUrls = null)
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
                    renderRequest.assetUrls = assetUrls;
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
                case RenderType.Model:
                    renderRequest.assetId = id;
                    url = "catalog/model";
                    break;
                case RenderType.Emote:
                    renderRequest.assetId = id;
                    url = "catalog/animationsilhouette";
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

        public static async Task<string> RequestHatThumbnail(long assetId, int JobExpiration)
        {
            return await SendRenderRequest(assetId, RenderType.Hat);
        }

        public static async Task<string> RequestMeshThumbnail(long assetId, int JobExpiration)
        {
            return await SendRenderRequest(assetId, RenderType.Mesh);
        }

        public static async Task<string> RequestModelThumbnail(long assetId, int JobExpiration)
        {
            return await SendRenderRequest(assetId, RenderType.Model);
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

        public static async Task<string> RequestAnimationRender(long assetId, int JobExpiration)
        {
            return await SendRenderRequest(assetId, RenderType.Emote);
        }
        public static async Task<string> RequestPackageRender(string assetUrls, int JobExpiration)
        {
            return await SendRenderRequest(0, RenderType.Package, assetUrls: assetUrls);
        }
        public static async Task<string> RequestPlayerThumbnail(long userId, int JobExpiration)
        {
            return await SendRenderRequest(userId, RenderType.Avatar);
        }
        public static async Task<string> RequestHeadshotThumbnail(long userId, int JobExpiration)
        {
            return await SendRenderRequest(userId, RenderType.Headshot);
        }
    }
}