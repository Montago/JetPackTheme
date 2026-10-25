using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JetPack.Web.TagHelpers;

/// <summary><c>&lt;jp-progress-bar value="75" /&gt;</c>, or is-indeterminate="true" for the running stripes.</summary>
[HtmlTargetElement("jp-progress-bar", TagStructure = TagStructure.WithoutEndTag)]
public class ProgressBarTagHelper : JetPackTagHelper
{
    public double Value { get; set; }

    public double Maximum { get; set; } = 100;

    public bool IsIndeterminate { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "progress";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-progress-bar");
        output.Attributes.SetAttribute("max", Html.Num(Maximum));
        if (!IsIndeterminate)
        {
            output.Attributes.SetAttribute("value", Html.Num(Value));
        }
        ApplyCommon(output);
    }
}

/// <summary><c>&lt;jp-progress-ring /&gt;</c>: the spinning accent arc (IsActive="False" hides it).</summary>
[HtmlTargetElement("jp-progress-ring", TagStructure = TagStructure.WithoutEndTag)]
public class ProgressRingTagHelper : JetPackTagHelper
{
    public bool IsActive { get; set; } = true;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-progress-ring");
        output.Attributes.SetAttribute("role", "progressbar");
        output.Attributes.SetAttribute("aria-label", "Working");
        output.Attributes.SetAttribute("data-active", IsActive ? "true" : "false");
        ApplyCommon(output);
    }
}

/// <summary>Where a Slider draws its ticks.</summary>
public enum TickPlacement
{
    None,
    BottomRight,
}

/// <summary>
/// <c>&lt;jp-slider value="4" maximum="10" tick-placement="BottomRight" tick-frequency="1" is-snap-to-tick-enabled="true" /&gt;</c>.
/// orientation="Vertical" stands it up; name posts the value.
/// </summary>
[HtmlTargetElement("jp-slider", TagStructure = TagStructure.WithoutEndTag)]
public class SliderTagHelper : JetPackTagHelper
{
    public double Value { get; set; }

    public double Minimum { get; set; }

    public double Maximum { get; set; } = 10;

    public double? SmallChange { get; set; }

    public double? LargeChange { get; set; }

    public string Orientation { get; set; } = "Horizontal";

    public TickPlacement TickPlacement { get; set; }

    public double TickFrequency { get; set; } = 1;

    public bool IsSnapToTickEnabled { get; set; }

    public string? Name { get; set; }

    /// <summary>Text for screen readers.</summary>
    public string? Label { get; set; }

    protected virtual bool Range => false;

    protected virtual double RangeLower => Minimum;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var vertical = Orientation.Equals("Vertical", StringComparison.OrdinalIgnoreCase);
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, Range ? "jp-range-slider" : "jp-slider", vertical ? "jp-slider--vertical" : null);
        output.Attributes.SetAttribute("role", "slider");
        output.Attributes.SetAttribute("aria-valuemin", Html.Num(Minimum));
        output.Attributes.SetAttribute("aria-valuemax", Html.Num(Maximum));
        output.Attributes.SetAttribute("aria-valuenow", Html.Num(Value));
        output.Attributes.SetAttribute("aria-orientation", vertical ? "vertical" : "horizontal");
        if (Label is not null) output.Attributes.SetAttribute("aria-label", Label);
        if (Range) output.Attributes.SetAttribute("data-jp-lower-value", Html.Num(RangeLower));
        if (SmallChange is { } small) output.Attributes.SetAttribute("data-jp-small-change", Html.Num(small));
        if (LargeChange is { } large) output.Attributes.SetAttribute("data-jp-large-change", Html.Num(large));
        if (TickPlacement != TickPlacement.None) output.Attributes.SetAttribute("data-jp-ticks", Html.Num(TickFrequency));
        if (IsSnapToTickEnabled)
        {
            output.Attributes.SetAttribute("data-jp-snap", "true");
            if (TickPlacement == TickPlacement.None) output.Attributes.SetAttribute("data-jp-ticks", Html.Num(TickFrequency));
        }
        if (!IsEnabled) output.Attributes.SetAttribute("aria-disabled", "true");
        ApplyCommon(output);

        var ticks = TickPlacement != TickPlacement.None ? "<div class=\"jp-slider__ticks\"></div>" : "";
        var inputs = Name is null ? "" : Range
            ? $"<input type=\"hidden\" name=\"{Html.Attr(Name)}.LowerValue\"><input type=\"hidden\" name=\"{Html.Attr(Name)}.UpperValue\">"
            : $"<input type=\"hidden\" name=\"{Html.Attr(Name)}\">";
        output.Content.SetHtmlContent("<div class=\"jp-slider__track\"></div><div class=\"jp-slider__fill\"></div>" + ticks + "<div class=\"jp-slider__thumb\"></div>" + inputs);
    }
}

/// <summary>
/// <c>&lt;jp-range-slider minimum="0" maximum="10000" lower-value="2500" upper-value="7000" small-change="100" /&gt;</c>:
/// two thumbs; jp-change gives { lowerValue, upperValue }.
/// </summary>
[HtmlTargetElement("jp-range-slider", TagStructure = TagStructure.WithoutEndTag)]
public class RangeSliderTagHelper : SliderTagHelper
{
    public double LowerValue { get; set; }

    public double UpperValue { get => Value; set => Value = value; }

    protected override bool Range => true;

    protected override double RangeLower => LowerValue;
}

/// <summary><c>&lt;jp-rating value="4" /&gt;</c>: stars to click, or is-read-only="true" to only show them.</summary>
[HtmlTargetElement("jp-rating", TagStructure = TagStructure.WithoutEndTag)]
public class RatingTagHelper : JetPackTagHelper
{
    public int Value { get; set; }

    public int Maximum { get; set; } = 5;

    public bool IsReadOnly { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        AddClasses(output, "jp-rating");
        output.Attributes.SetAttribute("style", $"--value: {Value}; --max: {Maximum};" + (output.Attributes["style"]?.Value ?? ""));
        if (IsReadOnly)
        {
            output.Attributes.SetAttribute("role", "img");
            output.Attributes.SetAttribute("aria-label", $"{Value} of {Maximum}");
        }
        else
        {
            output.Attributes.SetAttribute("role", "slider");
            output.Attributes.SetAttribute("aria-valuemin", "0");
            output.Attributes.SetAttribute("aria-valuemax", Maximum.ToString(System.Globalization.CultureInfo.InvariantCulture));
            output.Attributes.SetAttribute("aria-valuenow", Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            output.Attributes.SetAttribute("aria-label", "Rating");
            output.Attributes.SetAttribute("data-jp-input", "");
        }
        ApplyCommon(output);
    }
}
