using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.Demo.TagHelpers;

/// <summary>
/// <c>&lt;demo-column header="Button" tag="JetPack.Styles.Button"&gt;…&lt;/demo-column&gt;</c>: the Demo.Column style of
/// the WPF demo, a ruled column with a heading and the key it shows.
/// </summary>
[HtmlTargetElement("demo-column")]
public class DemoColumnTagHelper : TagHelper
{
    public string Header { get; set; } = "";

    public string? Tag { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = (await output.GetChildContentAsync()).GetContent();
        output.TagName = "section";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("demo-column", System.Text.Encodings.Web.HtmlEncoder.Default);
        var e = System.Text.Encodings.Web.HtmlEncoder.Default;
        output.Content.SetHtmlContent(
            $"<h5 class=\"demo-column__header\">{e.Encode(Header)}</h5>" +
            $"<span class=\"demo-column__tag\">{e.Encode(Tag ?? "")}</span>" +
            $"<div class=\"demo-column__content\">{content}</div>");
    }
}
