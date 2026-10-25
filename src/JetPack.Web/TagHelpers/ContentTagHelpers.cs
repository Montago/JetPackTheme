using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.TagHelpers;

/// <summary>Severity of an <see cref="InfoBarTagHelper"/>, as InfoBarSeverity in JetPack.Wpf.</summary>
public enum InfoBarSeverity
{
    Info,
    Success,
    Warning,
    Error,
}

/// <summary>
/// <c>&lt;jp-info-bar severity="Warning" title="4 timesheets" message="are waiting."&gt;actions&lt;/jp-info-bar&gt;</c>:
/// light strip with a coloured severity icon; the content goes to the right as actions.
/// </summary>
[HtmlTargetElement("jp-info-bar")]
public class InfoBarTagHelper : TagHelper
{
    public InfoBarSeverity Severity { get; set; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    /// <summary>Shows the close button (hides the bar in the browser).</summary>
    public bool IsClosable { get; set; } = true;

    /// <summary>False renders the bar hidden, like IsOpen="False".</summary>
    public bool IsOpen { get; set; } = true;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var actions = (await output.GetChildContentAsync()).GetContent().Trim();
        var severity = Severity.ToString().ToLowerInvariant();
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("jp-info-bar", Html.Encoder);
        output.AddClass("jp-info-bar--" + severity, Html.Encoder);
        output.Attributes.SetAttribute("role", Severity == InfoBarSeverity.Error ? "alert" : "status");
        if (!IsOpen)
        {
            output.Attributes.SetAttribute("hidden", "hidden");
        }

        var icon = Severity switch
        {
            InfoBarSeverity.Success => "✓",
            InfoBarSeverity.Warning or InfoBarSeverity.Error => "!",
            _ => "i",
        };
        output.Content.SetHtmlContent($"<span class=\"jp-info-bar__icon\" aria-hidden=\"true\">{icon}</span>");
        output.Content.AppendHtml("<span>");
        if (!string.IsNullOrEmpty(Title))
        {
            output.Content.AppendHtml("<span class=\"jp-info-bar__title\">").Append(Title).AppendHtml("</span> ");
        }
        output.Content.Append(Message ?? "");
        output.Content.AppendHtml("</span>");
        if (actions.Length > 0)
        {
            output.Content.AppendHtml("<span class=\"jp-info-bar__actions\">").AppendHtml(actions).AppendHtml("</span>");
        }
        if (IsClosable)
        {
            output.Content.AppendHtml("<button type=\"button\" class=\"jp-info-bar__close\" data-jp-dismiss aria-label=\"Close\">×</button>");
        }
    }
}

/// <summary>Colour of a <see cref="TileTagHelper"/>: the JetPack.Styles.Tile.* styles.</summary>
public enum TileStyle
{
    Default,
    Accent,
    Green,
    Orange,
    Blue,
}

/// <summary>
/// <c>&lt;jp-tile title="Active employees" count="8" tile-style="Green" href="/employees" /&gt;</c>:
/// a large button with a title and a number. Without href it renders a &lt;button&gt;.
/// </summary>
[HtmlTargetElement("jp-tile", TagStructure = TagStructure.NormalOrSelfClosing)]
public class TileTagHelper : TagHelper
{
    public string? Title { get; set; }

    public string? Count { get; set; }

    public TileStyle TileStyle { get; set; }

    public string? Href { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var extra = (await output.GetChildContentAsync()).GetContent();
        output.TagMode = TagMode.StartTagAndEndTag;
        if (Href is null)
        {
            output.TagName = "button";
            output.Attributes.SetAttribute("type", "button");
        }
        else
        {
            output.TagName = "a";
            output.Attributes.SetAttribute("href", Href);
        }
        output.AddClass("jp-tile", Html.Encoder);
        if (TileStyle != TileStyle.Default)
        {
            output.AddClass("jp-tile--" + TileStyle.ToString().ToLowerInvariant(), Html.Encoder);
        }
        output.Content.SetHtmlContent("<span class=\"jp-tile__title\">");
        output.Content.Append(Title ?? "").AppendHtml("</span>").AppendHtml(extra);
        output.Content.AppendHtml("<b class=\"jp-tile__count\">").Append(Count ?? "").AppendHtml("</b>");
    }
}

/// <summary><c>&lt;jp-group-box header="Details"&gt;content&lt;/jp-group-box&gt;</c>: dark panel with a header strip.</summary>
[HtmlTargetElement("jp-group-box")]
public class GroupBoxTagHelper : TagHelper
{
    public string? Header { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = (await output.GetChildContentAsync()).GetContent();
        output.TagName = "section";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("jp-group-box", Html.Encoder);
        output.Content.SetHtmlContent("<h2 class=\"jp-group-box__header\">");
        output.Content.Append(Header ?? "").AppendHtml("</h2><div class=\"jp-group-box__content\">");
        output.Content.AppendHtml(content).AppendHtml("</div>");
    }
}

/// <summary>Shared encoder for <see cref="TagHelperOutputExtensions.AddClass"/>.</summary>
internal static class Html
{
    public static readonly System.Text.Encodings.Web.HtmlEncoder Encoder = System.Text.Encodings.Web.HtmlEncoder.Default;
}
