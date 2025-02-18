using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Roblox.Website.Pages.Internal;

public class Promocodes : RobloxPageModel
{    

    public string? errorMessage { get; set; }
    public string? successMessage { get; set; }
    [BindProperty]
    public string? promocode { get; set; }
    public void OnGet()
    {

    }
    public async Task OnPost()
    {
        long assetId = 0;
        if (string.IsNullOrWhiteSpace(promocode))
        {
            errorMessage = "Promocode is empty";
            return;
        }
        try
        {
            await services.promocodes.ClaimPromocode(promocode, userSession.userId);
        }
        catch (Exception e)
        {
            errorMessage = e.Message;
            return;
        }
        var assetInfo = await services.assets.GetAssetCatalogInfo(assetId);
        successMessage = $"You have successfully claimed the item ({assetInfo.name}! Check your inventory to see it.";
        
    }
}