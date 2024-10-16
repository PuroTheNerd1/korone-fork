using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Roblox.Dto.Persistence;
using Roblox.Services;
using Roblox.Services.Exceptions;
using ServiceProvider = Roblox.Services.ServiceProvider;
namespace Roblox.Website.Controllers
{
    [Route("/")]
    public class Datastores : ControllerBase
    {
        private bool IsRcc()
        {
            var rccAccessKey = Request.Headers.ContainsKey("accesskey") ? Request.Headers["accesskey"].ToString() : null;
            var isRcc = rccAccessKey == Configuration.RccAuthorization;
            return isRcc;
        }

        [HttpPostBypass("persistence/increment")]
        public async Task<dynamic> IncrementPersistenceAsync(long placeId, string key, string type, string scope, string target, int? value = null)
        {
            // increment?placeId=%i&key=%s&type=%s&scope=%s&target=&value=%i
            var ds = ServiceProvider.GetOrCreate<DataStoreService>();
            if (!IsRcc())
                throw new RobloxException(400, 0, "BadRequest");

            if (value == null) 
                value = int.Parse(Request.Form["value"][0]);
            string? result = await ds.Get(placeId, type, scope, key, target);

            if (result != null)
            {
                if (int.TryParse(result, out var parsedValue))
                {
                    value = parsedValue + value.Value;
                }
                else
                {
                    throw new RobloxException(400, 0, "InvalidValue");
                }
            }
            else 
            {
                throw new RobloxException(400, 0, "InvalidValue");
            }

            await ds.Set(placeId, key, type, scope, target, 31, value.ToString());
            return new
            {
                data = value,
            };
        }

        [HttpPostBypass("persistence/set")]
        public async Task<dynamic> Set(long placeId, string key, string type, string scope, string target, int valueLength)
        {
            if (!IsRcc())
                throw new RobloxException(400, 0, "BadRequest");
            var value = Request.Form["value"][0];
            await ServiceProvider.GetOrCreate<DataStoreService>()
                .Set(placeId, key, type, scope, target, valueLength, value);
            return new 
            {
                data = new 
                {
                    Value = value,
                    Scope = scope,
                    Key = key,
                    Target = target    
                }
            };
        }
        [HttpPostBypass("persistence/getSortedValues")]
        public async Task<dynamic> GetSortedPersistenceValues(long placeId, string type, string scope, string key, bool ascending, int pageSize = 50, int inclusiveMinValue = 0, int inclusiveMaxValue = 0, int? exclusiveStartKey = null)
        {
            // persistence/getSortedValues?placeId=0&type=sorted&scope=global&key=Level%5FHighscores20&pageSize=10&ascending=False"
            // persistence/set?placeId=124921244&key=BF2%5Fds%5Ftest&&type=standard&scope=global&target=BF2%5Fds%5Fkey%5Ftmp&valueLength=31
            using var ds = ServiceProvider.GetOrCreate<DataStoreService>();
            if (!IsRcc())
                throw new RobloxException(403, 0, "BadRequest");
            if (pageSize > 100)
                throw new RobloxException(400, 0, "PageSizeTooLarge");
            if (type != "sorted")
                throw new RobloxException(400, 0, "TypeNotSorted");
            if (exclusiveStartKey == null)
                exclusiveStartKey = 1;	
            else if (exclusiveStartKey < 1)
                throw new RobloxException(400, 0, "InValidExclusiveStartKey");

            var res = await ds.GetOrderedEntry(placeId, key, scope);
            var result = new List<GetKeyEntry>();
            
            result.AddRange(res.Select(entry => new GetKeyEntry()
            {
                Target = entry.name,
                Value = entry.value ?? "",
            })
            .Where(entry => int.TryParse(entry.Value, out _))
            .OrderBy(entry => 
            {
                int value = int.Parse(entry.Value);
                return Math.Clamp(value, inclusiveMinValue, inclusiveMaxValue);
            }
            ));

            if (!ascending)
            {
                result.Reverse();
            }

            var startIndex = exclusiveStartKey.HasValue ? result.FindIndex(e => int.Parse(e.Value) > exclusiveStartKey.Value) : 0;

            if (startIndex == -1)
            {
                startIndex = result.Count; 
            }

            var endIndex = startIndex + pageSize;

            List<GetKeyEntry> paginatedEntries;
            if (startIndex >= result.Count)
            {
                paginatedEntries = new List<GetKeyEntry>();
            }
            else
            {
                paginatedEntries = result.GetRange(startIndex, Math.Min(pageSize, result.Count - startIndex));
            }

            return new
            {
                data = new
                {
                    Entries = paginatedEntries.Select(e => int.Parse(e.Value)).ToArray(),
                    ExclusiveStartKey = paginatedEntries.Count > 0 ? paginatedEntries.Last().Value : null,
                },
            };
        }

        [HttpPostBypass("persistence/getv2")]
        public async Task<dynamic> GetPersistenceV2(long placeId, string type, string scope)
        {
            if (!IsRcc())
                throw new RobloxException(403, 0, "Unauthorized");
            int countRequest = 0;
            using var ds = ServiceProvider.GetOrCreate<DataStoreService>();
            var result = new List<GetKeyEntry>();
            string qKeyscope;
            string qKeyTarget;
            string qKeyKey;
            while (true) 
            {
                qKeyscope = Request.Form[$"qkeys[{countRequest}].scope"]!;
                qKeyTarget = Request.Form[$"qkeys[{countRequest}].target"]!;
                qKeyKey = Request.Form[$"qkeys[{countRequest}].key"]!;

                if (qKeyscope == null || qKeyTarget == null || qKeyKey == null)
                    break;

                string value = await ds.Get(placeId, type, qKeyscope, qKeyKey, qKeyTarget);
                if (value == null)
                {
                    continue;
                }
                result.Add(new GetKeyEntry()
                {
                    Key = qKeyKey,
                    Scope = qKeyscope ?? scope,
                    Target = qKeyTarget,
                    Value = value ?? ""
                });
                countRequest++;
            }

            var finalData = new { data = result };
            string jsonString = JsonConvert.SerializeObject(finalData);
            return Content(jsonString, "application/json");
        }
    }
}
