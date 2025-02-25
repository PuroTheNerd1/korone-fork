using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

public class ApiExplorerGetsOnlyConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        var apiExplorerSettings = action.Controller.Attributes.OfType<ApiExplorerSettingsAttribute>().FirstOrDefault();
        if (apiExplorerSettings != null && (apiExplorerSettings.IgnoreApi || apiExplorerSettings.GroupName == null))
        {
            action.ApiExplorer.IsVisible = false;
        }
        else
        {
            action.ApiExplorer.IsVisible = action.Attributes.OfType<HttpGetAttribute>().Any() || action.Attributes.OfType<HttpPostAttribute>().Any();
        }
    }
}