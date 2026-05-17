using Microsoft.AspNetCore.Razor.TagHelpers;

namespace diplomaProject.TagHelpers;

[HtmlTargetElement("navigator")]
public class NavigatorTagHelper : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "nav";

        var classes = output.Attributes["class"]?.Value?.ToString() ?? "";
        output.Attributes.SetAttribute("class", $"nav {classes}".Trim());
    }
}