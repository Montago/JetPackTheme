using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.TagHelpers;

/// <summary>
/// Base of the &lt;jp-*&gt; tag helpers: the properties every WPF control has. Attribute names are the
/// WPF property names in kebab case (IsEnabled → is-enabled).
/// </summary>
public abstract class JetPackTagHelper : TagHelper
{
    /// <summary>IsEnabled="False": the control is shown disabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>ToolTip: the green call-out jetpack.js shows under the pointer.</summary>
    public string? ToolTip { get; set; }

    /// <summary>
    /// Marks the control invalid with this message, as a validation error in WPF: the red frame and
    /// corner, and the message in a red call-out when the control has the focus or the mouse.
    /// </summary>
    public string? ValidationError { get; set; }

    /// <summary>Sets data-jp-tool-tip and the invalid look on the element that represents the control.</summary>
    protected void ApplyCommon(TagHelperOutput output)
    {
        if (ToolTip is not null)
        {
            output.Attributes.SetAttribute("data-jp-tool-tip", ToolTip);
        }
        if (ValidationError is not null)
        {
            output.Attributes.SetAttribute("aria-invalid", "true");
            output.Attributes.SetAttribute("data-jp-error", ValidationError);
        }
    }

    /// <summary>The same attributes as markup, for an inner element (a text box inside a wrapper).</summary>
    protected string CommonAttributes(bool toolTip = true, bool error = true)
    {
        var s = "";
        if (toolTip && ToolTip is not null)
        {
            s += $" data-jp-tool-tip=\"{Html.Attr(ToolTip)}\"";
        }
        if (error && ValidationError is not null)
        {
            s += $" aria-invalid=\"true\" data-jp-error=\"{Html.Attr(ValidationError)}\"";
        }
        return s;
    }

    /// <summary>Keeps class, style, id and other plain attributes the page wrote on the tag helper.</summary>
    protected static void AddClasses(TagHelperOutput output, params string?[] classes)
    {
        foreach (var c in classes)
        {
            if (!string.IsNullOrEmpty(c))
            {
                output.AddClass(c, Html.Encoder);
            }
        }
    }
}

/// <summary>Collects the children of a container tag helper (TabControl → TabItems and so on).</summary>
internal sealed class ChildList<T>
{
    public List<T> Items { get; } = [];

    public static ChildList<T> Start(TagHelperContext context)
    {
        var list = new ChildList<T>();
        context.Items[typeof(ChildList<T>)] = list;
        return list;
    }

    public static ChildList<T>? Of(TagHelperContext context) =>
        context.Items.TryGetValue(typeof(ChildList<T>), out var v) ? (ChildList<T>)v : null;
}

internal static class Html
{
    public static readonly HtmlEncoder Encoder = HtmlEncoder.Default;

    public static string Text(string? s) => Encoder.Encode(s ?? "");

    public static string Attr(string? s) => Encoder.Encode(s ?? "");

    public static string Num(double v) => v.ToString(CultureInfo.InvariantCulture);

    public static string Kebab(string pascal) =>
        System.Text.RegularExpressions.Regex.Replace(pascal, "(?<=[a-z0-9])(?=[A-Z])", "-").ToLowerInvariant();

    /// <summary>A unique id for the parts of a control that need one (tab panels, popups).</summary>
    public static string NewId(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// The address of an icon in the sprite, below the site's path base (app.UsePathBase), so the
    /// icons also load when the site runs under a sub-path.
    /// </summary>
    public static string IconHref(ViewContext view, JetPackIconKind kind) =>
        view.HttpContext.Request.PathBase + "/_content/JetPack.Web/img/icons.svg#" + JetPackIconIds.Get(kind);

    /// <summary>A decorative icon (hidden from screen readers) as markup; empty for None.</summary>
    public static string Icon(ViewContext view, JetPackIconKind kind) => kind == JetPackIconKind.None
        ? ""
        : $"<svg class=\"jp-icon\" aria-hidden=\"true\"><use href=\"{Attr(IconHref(view, kind))}\"></use></svg>";
}

/// <summary>
/// <c>jp-tool-tip="Saves the changes"</c> on any element: the JetPack tool tip under the pointer.
/// </summary>
[HtmlTargetElement("*", Attributes = "jp-tool-tip")]
public class ToolTipTagHelper : TagHelper
{
    [HtmlAttributeName("jp-tool-tip")]
    public string? Text { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Text is not null)
        {
            output.Attributes.SetAttribute("data-jp-tool-tip", Text);
        }
    }
}
