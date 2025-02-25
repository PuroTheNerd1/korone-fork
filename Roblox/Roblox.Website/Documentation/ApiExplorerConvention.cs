using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

public class ApiExplorerGetsOnlyConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        var apiExplorerSettings = action.Attributes.OfType<ApiExplorerSettingsAttribute>().FirstOrDefault();
        if (apiExplorerSettings != null && apiExplorerSettings.IgnoreApi)
        {
            action.ApiExplorer.IsVisible = false;
        }
        else
        {
            action.ApiExplorer.IsVisible = action.Attributes.OfType<HttpGetAttribute>().Any() || action.Attributes.OfType<HttpPostAttribute>().Any();
        }
    }
}