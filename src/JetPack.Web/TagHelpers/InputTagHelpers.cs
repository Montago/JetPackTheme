using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.TagHelpers;

/// <summary>
/// <c>&lt;jp-numeric-up-down asp-for="HoursPerWeek" minimum="0" maximum="40" interval="0.5" /&gt;</c>:
/// a number box with the JetPack spin buttons. Name, id and validation attributes come from the
/// model like an ordinary input tag helper; the value is written with a period, as type=number needs.
/// </summary>
[HtmlTargetElement("jp-numeric-up-down", TagStructure = TagStructure.WithoutEndTag)]
public class NumericUpDownTagHelper(IHtmlGenerator generator) : TagHelper
{
    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    public double? Minimum { get; set; }

    public double? Maximum { get; set; }

    public double Interval { get; set; } = 1;

    /// <summary>JetPack.Styles.NumericUpDown.Dark</summary>
    public bool Dark { get; set; }

    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var attributes = new Dictionary<string, object?>
        {
            ["type"] = "number",
            ["class"] = Dark ? "jp-text-box jp-text-box--dark" : "jp-text-box",
            ["step"] = Interval.ToString(CultureInfo.InvariantCulture),
        };
        if (Minimum is { } min)
        {
            attributes["min"] = min.ToString(CultureInfo.InvariantCulture);
        }
        if (Maximum is { } max)
        {
            attributes["max"] = max.ToString(CultureInfo.InvariantCulture);
        }
        var value = Convert.ToString(For.Model, CultureInfo.InvariantCulture);
        // Adds the validation attributes, and input-validation-error after a failed post
        var input = generator.GenerateTextBox(ViewContext, For.ModelExplorer, For.Name, value, null, attributes);

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("jp-numeric-up-down", Html.Encoder);
        output.Content.SetHtmlContent(input);
        output.Content.AppendHtml(
            "<span class=\"jp-numeric-up-down__spin\">" +
            "<button type=\"button\" data-jp-step=\"1\" tabindex=\"-1\" aria-label=\"Increase\"></button>" +
            "<button type=\"button\" data-jp-step=\"-1\" tabindex=\"-1\" aria-label=\"Decrease\"></button>" +
            "</span>");
    }
}

/// <summary>
/// <c>&lt;jp-toggle-switch asp-for="IsActive"&gt;Active&lt;/jp-toggle-switch&gt;</c>: an on/off switch for
/// a bool property, posted like a check box. Without asp-for, give it a name (and checked).
/// </summary>
[HtmlTargetElement("jp-toggle-switch")]
public class ToggleSwitchTagHelper(IHtmlGenerator generator) : TagHelper
{
    [HtmlAttributeName("asp-for")]
    public ModelExpression? For { get; set; }

    public string? Name { get; set; }

    public bool Checked { get; set; }

    public bool Disabled { get; set; }

    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var text = (await output.GetChildContentAsync()).GetContent();
        var attributes = new Dictionary<string, object?> { ["role"] = "switch" };
        if (Disabled)
        {
            attributes["disabled"] = "disabled";
        }

        TagBuilder input;
        if (For is not null)
        {
            input = generator.GenerateCheckBox(ViewContext, For.ModelExplorer, For.Name, null, attributes);
        }
        else
        {
            input = new TagBuilder("input") { TagRenderMode = TagRenderMode.SelfClosing };
            input.MergeAttributes(attributes);
            input.MergeAttribute("type", "checkbox");
            if (Name is not null)
            {
                input.MergeAttribute("name", Name);
                input.MergeAttribute("value", "true");
            }
            if (Checked)
            {
                input.MergeAttribute("checked", "checked");
            }
        }

        output.TagName = "label";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("jp-toggle-switch", Html.Encoder);
        output.Content.SetHtmlContent(input);
        output.Content.AppendHtml(text);
    }
}
