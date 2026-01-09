using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roblox.Dto.Users;
using Roblox.Services.Exceptions;
using Roblox.Website.Filters;

namespace Roblox.Website.Pages.Internal;

public class CollectibleInventory : RobloxPageModel
{
    [BindProperty(SupportsGet = true)]
    public long userId { get; set; }
    public List<CollectibleItemEntry> inventory { get; set; }
    public string username { get; set; }
    public string? errorMessage { get; set; }
    public long totalRap { get; set; }

    public async Task OnGet()
    {
        UserInfo info;
        try
        {
            info = await services.users.GetUserById(userId);
            username = info.username;
        }
        catch (RecordNotFoundException)
        {
            errorMessage = "User ID is invalid or does not exist.";
            return;
        }
        // If the user has their inventory privacy setitngs to private or if they are banned, other users can not view their inventory only staff can
        if (!await services.inventory.CanViewInventory(userId, userSession?.userId ?? 0) || 
            info.IsDeleted() && !await StaffFilter.IsStaff(userSession?.userId ?? 0))
        {
            errorMessage = "You don't have permissions to view the specified user's inventory";
            return;
        }

        inventory = new ();
        var offset = 0;
        while (true)
        {
            var results = (await services.inventory.GetCollectibleInventory(userId, null, "asc", 100, offset)).ToArray();
            if (results.Length == 0) break;
            offset += 100;
            inventory.AddRange(results);
        }

        foreach (var item in inventory)
        {
            totalRap += item.recentAveragePrice;
        }

        inventory.Sort((a, b) =>
        {
            return a.recentAveragePrice > b.recentAveragePrice ? -1 :
                a.recentAveragePrice == b.recentAveragePrice ? 0 : 1;
        });
    }
}