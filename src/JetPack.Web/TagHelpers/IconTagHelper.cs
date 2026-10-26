using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.TagHelpers;

/// <summary>
/// <c>&lt;jp-icon kind="Bell" /&gt;</c>: one of the JetPack icons from the sprite in
/// _content/JetPack.Web/img/icons.svg, drawn in the current text colour, 16×16 by default.
/// Without a label the icon is hidden from screen readers.
/// </summary>
[HtmlTargetElement("jp-icon", TagStructure = TagStructure.WithoutEndTag)]
public class IconTagHelper : TagHelper
{
    public JetPackIconKind Kind { get; set; }

    /// <summary>Text for screen readers when the icon stands alone.</summary>
    public string? Label { get; set; }

    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("jp-icon", Html.Encoder);
        if (Label is null)
        {
            output.Attributes.SetAttribute("aria-hidden", "true");
        }
        else
        {
            output.Attributes.SetAttribute("role", "img");
            output.Attributes.SetAttribute("aria-label", Label);
        }
        output.Attributes.SetAttribute("focusable", "false");

        var use = new TagBuilder("use");
        use.MergeAttribute("href", Html.IconHref(ViewContext, Kind));
        output.Content.SetHtmlContent(use);
    }
}
