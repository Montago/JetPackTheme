using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.TagHelpers;

/// <summary>The JetPack.Styles.TextBlock.* styles.</summary>
public enum TextBlockStyle
{
    Default,
    Heading1,
    Heading2,
    Heading3,
    Heading4,
    Heading5,
    Heading6,
    Content,
    Caption,
}

/// <summary><c>&lt;jp-text-block text-block-style="Heading1" text="Companies" /&gt;</c>: headings become h1–h6.</summary>
[HtmlTargetElement("jp-text-block")]
public class TextBlockTagHelper : JetPackTagHelper
{
    public TextBlockStyle TextBlockStyle { get; set; }

    public string? Text { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var inner = (await output.GetChildContentAsync()).GetContent();
        var name = TextBlockStyle.ToString();
        output.TagName = name.StartsWith("Heading") ? "h" + name[^1] : "p";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, TextBlockStyle == TextBlockStyle.Default ? null : "jp-text-block--" + name.ToLowerInvariant());
        if (output.TagName == "p")
        {
            output.Attributes.SetAttribute("style", "margin:0;" + (output.Attributes["style"]?.Value ?? ""));
        }
        ApplyCommon(output);
        output.Content.SetHtmlContent(Text is null ? inner : Html.Text(Text));
    }
}

/// <summary><c>&lt;jp-label content="Name" target="NameBox" /&gt;</c>: Label with Target (the for attribute).</summary>
[HtmlTargetElement("jp-label")]
public class LabelTagHelper : JetPackTagHelper
{
    public string? Content { get; set; }

    public string? Target { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var inner = (await output.GetChildContentAsync()).GetContent();
        output.TagName = "label";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-label");
        if (Target is not null)
        {
            output.Attributes.SetAttribute("for", Target);
        }
        output.Content.SetHtmlContent(Content is null ? inner : Html.Text(Content));
    }
}

/// <summary><c>&lt;jp-separator /&gt;</c>: the etched line; inside a menu the menu separator.</summary>
[HtmlTargetElement("jp-separator", TagStructure = TagStructure.WithoutEndTag)]
public class SeparatorTagHelper : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "hr";
        output.TagMode = TagMode.SelfClosing;
        output.AddClass(context.Items.ContainsKey(typeof(MenuScope)) ? "jp-menu-separator" : "jp-separator", Html.Encoder);
    }
}

/// <summary>The JetPack.Styles.TextBox.* styles.</summary>
public enum TextBoxStyle
{
    Default,
    Dark,
    MultiLine,
    MultiLineDark,
}

/// <summary>
/// <c>&lt;jp-text-box asp-for="Name" watermark="Search…" clear-text-button="true" text-box-style="Dark" /&gt;</c>:
/// TextBox with TextBoxHelper.Watermark and ClearTextButton. Without asp-for use text and name.
/// </summary>
[HtmlTargetElement("jp-text-box", TagStructure = TagStructure.WithoutEndTag)]
public class TextBoxTagHelper(IHtmlGenerator generator) : JetPackTagHelper
{
    [HtmlAttributeName("asp-for")]
    public ModelExpression? For { get; set; }

    public string? Text { get; set; }

    public string? Name { get; set; }

    public string? Id { get; set; }

    public string? Watermark { get; set; }

    public bool ClearTextButton { get; set; }

    public bool IsReadOnly { get; set; }

    public TextBoxStyle TextBoxStyle { get; set; }

    /// <summary>The input type: text, email, search, tel …</summary>
    public string Type { get; set; } = "text";

    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    protected virtual string InputType => Type;

    protected virtual string? Value => Text;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var multiLine = TextBoxStyle is TextBoxStyle.MultiLine or TextBoxStyle.MultiLineDark;
        var dark = TextBoxStyle is TextBoxStyle.Dark or TextBoxStyle.MultiLineDark;
        var attributes = new Dictionary<string, object?>
        {
            ["class"] = "jp-text-box" + (dark ? " jp-text-box--dark" : ""),
        };
        if (Watermark is not null) attributes["placeholder"] = Watermark;
        if (IsReadOnly) attributes["readonly"] = "readonly";
        if (!IsEnabled) attributes["disabled"] = "disabled";
        if (Id is not null) attributes["id"] = Id;
        if (ToolTip is not null) attributes["data-jp-tool-tip"] = ToolTip;
        if (ValidationError is not null)
        {
            attributes["aria-invalid"] = "true";
            attributes["data-jp-error"] = ValidationError;
        }
        // The page's own class and style go on the box
        foreach (var a in output.Attributes.ToList())
        {
            if (a.Name == "class") attributes["class"] += " " + a.Value;
            else attributes[a.Name] = a.Value?.ToString();
        }
        output.Attributes.Clear();

        TagBuilder box;
        if (For is not null)
        {
            box = multiLine
                ? generator.GenerateTextArea(ViewContext, For.ModelExplorer, For.Name, 0, 0, attributes)
                : generator.GenerateTextBox(ViewContext, For.ModelExplorer, For.Name, For.Model, null, Merge(attributes, "type", InputType));
        }
        else if (multiLine)
        {
            box = new TagBuilder("textarea");
            box.MergeAttributes(attributes);
            if (Name is not null) box.MergeAttribute("name", Name);
            box.InnerHtml.Append(Value ?? "");
        }
        else
        {
            box = new TagBuilder("input") { TagRenderMode = TagRenderMode.SelfClosing };
            box.MergeAttributes(attributes);
            box.MergeAttribute("type", InputType);
            if (Name is not null) box.MergeAttribute("name", Name);
            if (Value is not null) box.MergeAttribute("value", Value);
        }

        if (ClearTextButton)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("class", "jp-text-box-host");
            output.Content.SetHtmlContent(box);
            output.Content.AppendHtml("<button type=\"button\" class=\"jp-text-box-host__clear\" data-jp-clear aria-label=\"Clear\" tabindex=\"-1\">×</button>");
        }
        else
        {
            output.TagName = null;
            output.Content.SetHtmlContent(box);
        }
    }

    private static Dictionary<string, object?> Merge(Dictionary<string, object?> a, string key, string value) => new(a) { [key] = value };
}

/// <summary><c>&lt;jp-password-box password="…" watermark="Password" clear-text-button="true" /&gt;</c></summary>
[HtmlTargetElement("jp-password-box", TagStructure = TagStructure.WithoutEndTag)]
public class PasswordBoxTagHelper(IHtmlGenerator generator) : TextBoxTagHelper(generator)
{
    public string? Password { get; set; }

    protected override string InputType => "password";

    protected override string? Value => Password;
}

/// <summary>An item of a ComboBox: <c>&lt;jp-combo-box-item content="Seattle" value="sea" /&gt;</c>.</summary>
[HtmlTargetElement("jp-combo-box-item", ParentTag = "jp-combo-box", TagStructure = TagStructure.WithoutEndTag)]
public class ComboBoxItemTagHelper : TagHelper
{
    public string Content { get; set; } = "";

    public string? Value { get; set; }

    public bool IsEnabled { get; set; } = true;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ChildList<ComboBoxItemTagHelper>.Of(context)?.Items.Add(this);
        output.SuppressOutput();
    }
}

/// <summary>
/// <c>&lt;jp-combo-box selected-index="1"&gt;&lt;jp-combo-box-item content="Seattle" /&gt;…&lt;/jp-combo-box&gt;</c>, or with
/// asp-for and asp-items. IsEditable makes it a text box with the list as suggestions; Watermark shows
/// while nothing is chosen.
/// </summary>
[HtmlTargetElement("jp-combo-box")]
public class ComboBoxTagHelper(IHtmlGenerator generator) : JetPackTagHelper
{
    [HtmlAttributeName("asp-for")]
    public ModelExpression? For { get; set; }

    [HtmlAttributeName("asp-items")]
    public IEnumerable<SelectListItem>? Items { get; set; }

    public int SelectedIndex { get; set; } = -1;

    public bool IsEditable { get; set; }

    /// <summary>The text of an editable combo box.</summary>
    public string? Text { get; set; }

    public string? Watermark { get; set; }

    public string? Name { get; set; }

    public string? Id { get; set; }

    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var children = ChildList<ComboBoxItemTagHelper>.Start(context);
        await output.GetChildContentAsync();
        var items = children.Items;
        var disabled = IsEnabled ? "" : " disabled";

        if (IsEditable)
        {
            var listId = Html.NewId("jp-combo");
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            AddClasses(output, "jp-combo-box", "jp-combo-box--editable");
            var name = Name is null ? "" : $" name=\"{Html.Attr(Name)}\"";
            var id = Id is null ? "" : $" id=\"{Html.Attr(Id)}\"";
            var watermark = Watermark is null ? "" : $" placeholder=\"{Html.Attr(Watermark)}\"";
            output.Content.SetHtmlContent(
                $"<input class=\"jp-text-box\" list=\"{listId}\" value=\"{Html.Attr(Text)}\"{name}{id}{watermark}{disabled}{CommonAttributes()}>" +
                $"<button type=\"button\" class=\"jp-text-box-button jp-text-box-button--drop-down\" tabindex=\"-1\" aria-label=\"Open\"{disabled}></button>" +
                $"<datalist id=\"{listId}\">" + string.Concat(items.Select(i => $"<option value=\"{Html.Attr(i.Content)}\"></option>")) + "</datalist>");
            return;
        }

        var attributes = new Dictionary<string, object?> { ["class"] = "jp-combo-box" };
        if (!IsEnabled) attributes["disabled"] = "disabled";
        if (Id is not null) attributes["id"] = Id;
        if (Watermark is not null) attributes["data-jp-watermark"] = Watermark;
        if (ToolTip is not null) attributes["data-jp-tool-tip"] = ToolTip;
        if (ValidationError is not null)
        {
            attributes["aria-invalid"] = "true";
            attributes["data-jp-error"] = ValidationError;
        }
        foreach (var a in output.Attributes.ToList())
        {
            if (a.Name == "class") attributes["class"] += " " + a.Value;
            else attributes[a.Name] = a.Value?.ToString();
        }
        output.Attributes.Clear();

        TagBuilder select;
        if (For is not null)
        {
            select = generator.GenerateSelect(ViewContext, For.ModelExplorer, Watermark, For.Name, Items ?? [], false, attributes);
        }
        else
        {
            select = new TagBuilder("select");
            select.MergeAttributes(attributes);
            if (Name is not null) select.MergeAttribute("name", Name);
            if (Watermark is not null)
            {
                select.InnerHtml.AppendHtml($"<option value=\"\" disabled hidden{(SelectedIndex < 0 ? " selected" : "")}>{Html.Text(Watermark)}</option>");
            }
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var value = item.Value is null ? "" : $" value=\"{Html.Attr(item.Value)}\"";
                var selected = i == SelectedIndex ? " selected" : "";
                var off = item.IsEnabled ? "" : " disabled";
                select.InnerHtml.AppendHtml($"<option{value}{selected}{off}>{Html.Text(item.Content)}</option>");
            }
        }
        output.TagName = null;
        output.Content.SetHtmlContent(select);
    }
}

/// <summary>
/// <c>&lt;jp-date-picker selected-date="@DateTime.Today" watermark="Select a date" /&gt;</c>: the text box, the
/// calendar button and a Calendar drop-down. With name, a hidden input posts the date as yyyy-MM-dd.
/// </summary>
[HtmlTargetElement("jp-date-picker", TagStructure = TagStructure.WithoutEndTag)]
public class DatePickerTagHelper : JetPackTagHelper
{
    public DateTime? SelectedDate { get; set; }

    public string Watermark { get; set; } = "Select a date";

    public string? Name { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-date-picker");
        var date = SelectedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        output.Attributes.SetAttribute("data-selected-date", date);
        var text = SelectedDate?.ToString("M/d/yyyy", CultureInfo.InvariantCulture) ?? "";
        var disabled = IsEnabled ? "" : " disabled";
        output.Content.SetHtmlContent(
            $"<input class=\"jp-text-box\" value=\"{text}\" placeholder=\"{Html.Attr(Watermark)}\"{disabled}{CommonAttributes()}>" +
            $"<button type=\"button\" class=\"jp-text-box-button jp-text-box-button--calendar\" tabindex=\"-1\" aria-label=\"Show calendar\"{disabled}></button>" +
            (Name is null ? "" : $"<input type=\"hidden\" name=\"{Html.Attr(Name)}\" value=\"{date}\">"));
    }
}

/// <summary><c>&lt;jp-time-picker selected-time="08:30" minute-step="30" watermark="Meeting time" /&gt;</c></summary>
[HtmlTargetElement("jp-time-picker", TagStructure = TagStructure.WithoutEndTag)]
public class TimePickerTagHelper : JetPackTagHelper
{
    public TimeSpan? SelectedTime { get; set; }

    public int MinuteStep { get; set; } = 15;

    public string? Watermark { get; set; }

    public string? Name { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-time-picker");
        output.Attributes.SetAttribute("data-jp-minute-step", MinuteStep.ToString(CultureInfo.InvariantCulture));
        var text = SelectedTime?.ToString(@"hh\:mm", CultureInfo.InvariantCulture) ?? "";
        var watermark = Watermark is null ? "" : $" placeholder=\"{Html.Attr(Watermark)}\"";
        var name = Name is null ? "" : $" name=\"{Html.Attr(Name)}\"";
        var disabled = IsEnabled ? "" : " disabled";
        output.Content.SetHtmlContent(
            $"<input class=\"jp-text-box\" value=\"{text}\"{watermark}{name}{disabled}{CommonAttributes()}>" +
            $"<button type=\"button\" class=\"jp-text-box-button jp-text-box-button--clock\" tabindex=\"-1\" aria-label=\"Choose a time\"{disabled}></button>");
    }
}

/// <summary>
/// <c>&lt;jp-auto-suggest-box items="@names" watermark="Search companies…" /&gt;</c>: a text box that suggests
/// the items containing what you type.
/// </summary>
[HtmlTargetElement("jp-auto-suggest-box", TagStructure = TagStructure.WithoutEndTag)]
public class AutoSuggestBoxTagHelper : JetPackTagHelper
{
    public IEnumerable<string> Items { get; set; } = [];

    public string? Text { get; set; }

    public string? Watermark { get; set; }

    public string? Name { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-auto-suggest-box");
        var watermark = Watermark is null ? "" : $" placeholder=\"{Html.Attr(Watermark)}\"";
        var name = Name is null ? "" : $" name=\"{Html.Attr(Name)}\"";
        var disabled = IsEnabled ? "" : " disabled";
        output.Content.SetHtmlContent(
            $"<input class=\"jp-text-box\" type=\"search\" autocomplete=\"off\" value=\"{Html.Attr(Text)}\"{watermark}{name}{disabled}{CommonAttributes()}>" +
            $"<script type=\"application/json\">{Json(Items)}</script>");
    }

    internal static string Json(object items) =>
        JsonSerializer.Serialize(items).Replace("</", "<\\/", StringComparison.Ordinal);
}

/// <summary>How a SearchComboBox matches what you type.</summary>
public enum SearchFilterMode
{
    Contains,
    StartsWith,
}

/// <summary>
/// <c>&lt;jp-search-combo-box items="@companies" display-member-path="Name" selected-value-path="Id" selected-value="1042" /&gt;</c>:
/// a combo box you type into to filter a long list. The items go to the page as JSON.
/// </summary>
[HtmlTargetElement("jp-search-combo-box", TagStructure = TagStructure.WithoutEndTag)]
public class SearchComboBoxTagHelper : JetPackTagHelper
{
    public System.Collections.IEnumerable Items { get; set; } = Array.Empty<object>();

    public string? DisplayMemberPath { get; set; }

    public string? SelectedValuePath { get; set; }

    public object? SelectedValue { get; set; }

    /// <summary>The selected item itself (for lists of strings).</summary>
    public object? SelectedItem { get; set; }

    public SearchFilterMode FilterMode { get; set; }

    public string? Watermark { get; set; }

    public string? Name { get; set; }

    public string? Id { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-search-combo-box");
        if (Id is not null) output.Attributes.SetAttribute("id", Id);
        if (DisplayMemberPath is not null) output.Attributes.SetAttribute("data-jp-display-member", DisplayMemberPath);
        if (SelectedValuePath is not null) output.Attributes.SetAttribute("data-jp-value-member", SelectedValuePath);
        if (FilterMode == SearchFilterMode.StartsWith) output.Attributes.SetAttribute("data-jp-filter-mode", "StartsWith");
        var selected = SelectedValue ?? SelectedItem;
        if (selected is not null) output.Attributes.SetAttribute("data-jp-selected-value", Convert.ToString(selected, CultureInfo.InvariantCulture));
        var watermark = Watermark is null ? "" : $" placeholder=\"{Html.Attr(Watermark)}\"";
        var disabled = IsEnabled ? "" : " disabled";
        output.Content.SetHtmlContent(
            $"<input class=\"jp-text-box\" autocomplete=\"off\" role=\"combobox\"{watermark}{disabled}{CommonAttributes()}>" +
            $"<button type=\"button\" class=\"jp-text-box-button jp-text-box-button--drop-down\" tabindex=\"-1\" aria-label=\"Open\"{disabled}></button>" +
            (Name is null ? "" : $"<input type=\"hidden\" name=\"{Html.Attr(Name)}\">") +
            $"<script type=\"application/json\">{AutoSuggestBoxTagHelper.Json(Items)}</script>");
    }
}

/// <summary><c>&lt;jp-color-picker selected-color="#75A20A" /&gt;</c>: a swatch button that opens the theme palette.</summary>
[HtmlTargetElement("jp-color-picker", TagStructure = TagStructure.WithoutEndTag)]
public class ColorPickerTagHelper : JetPackTagHelper
{
    public string SelectedColor { get; set; } = "#75A20A";

    public string? Id { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "button";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("type", "button");
        AddClasses(output, "jp-color-picker");
        if (Id is not null) output.Attributes.SetAttribute("id", Id);
        output.Attributes.SetAttribute("data-selected-color", SelectedColor);
        output.Attributes.SetAttribute("aria-haspopup", "dialog");
        if (!IsEnabled) output.Attributes.SetAttribute("disabled", "disabled");
        ApplyCommon(output);
        output.Content.SetHtmlContent("<span class=\"jp-color-picker__swatch\"></span><span class=\"jp-color-picker__text\"></span>");
    }
}
