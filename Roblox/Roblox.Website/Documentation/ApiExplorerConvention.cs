public class ApiExplorerConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        action.ApiExplorer.IsVisible = action.Controller.ControllerType.BaseType == typeof(ControllerBase);
    }
}