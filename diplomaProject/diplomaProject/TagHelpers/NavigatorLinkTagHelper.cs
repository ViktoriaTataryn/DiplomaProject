using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace diplomaProject.TagHelpers;

[HtmlTargetElement("nav-link")]
public class NavigatorLinkTagHelper(IHtmlGenerator generator) : AnchorTagHelper(generator)
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        base.Process(context, output);
        output.TagName = "a";

        var existingClasses = output.Attributes["class"]?.Value?.ToString() ?? "";
        if (!existingClasses.Contains("nav-link")) existingClasses = $"nav-link {existingClasses}".Trim();

        var currentArea = ViewContext.RouteData.Values["area"]?.ToString();
        var currentController = ViewContext.RouteData.Values["controller"]?.ToString();
        var currentAction = ViewContext.RouteData.Values["action"]?.ToString();

        var areaMatches = string.Equals(Area ?? "", currentArea ?? "", StringComparison.OrdinalIgnoreCase);
        var controllerMatches = string.Equals(Controller, currentController, StringComparison.OrdinalIgnoreCase);
        var actionMatches = string.Equals(Action, currentAction, StringComparison.OrdinalIgnoreCase);

        if (areaMatches && controllerMatches && actionMatches) existingClasses = $"{existingClasses} active".Trim();

        output.Attributes.SetAttribute("class", existingClasses);
    }
}