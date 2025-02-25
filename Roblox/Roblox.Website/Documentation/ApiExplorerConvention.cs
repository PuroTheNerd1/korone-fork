using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

public class ApiExplorerConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        action.ApiExplorer.IsVisible = typeof(ControllerBase)
            .IsAssignableFrom(action.Controller.ControllerType);
    }
}