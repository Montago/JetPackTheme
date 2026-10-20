using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace JetPack.Wpf.Demo;

/// <summary>Small attached helpers that keep the demo XAML short.</summary>
public static class Demo
{
    /// <summary>Vertical gap between the children of a Panel.</summary>
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.RegisterAttached(
        "Spacing", typeof(double), typeof(Demo), new PropertyMetadata(0d, OnSpacingChanged));

    public static double GetSpacing(DependencyObject o) => (double)o.GetValue(SpacingProperty);
    public static void SetSpacing(DependencyObject o, double value) => o.SetValue(SpacingProperty, value);

    private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Panel panel)
        {
            panel.Loaded += (_, _) => ApplySpacing(panel);
        }
    }

    private static void ApplySpacing(Panel panel)
    {
        var gap = GetSpacing(panel);
        var horizontal = panel is StackPanel { Orientation: Orientation.Horizontal };
        var children = panel.Children.OfType<FrameworkElement>().ToList();
        for (var i = 0; i < children.Count - 1; i++)
        {
            var m = children[i].Margin;
            children[i].Margin = horizontal ? new Thickness(m.Left, m.Top, gap, m.Bottom) : new Thickness(m.Left, m.Top, m.Right, gap);
        }
    }

    /// <summary>Gallery samples: open this element's drop-down, menu or tool tip for the snapshot.</summary>
    public static readonly DependencyProperty OpenProperty = DependencyProperty.RegisterAttached(
        "Open", typeof(bool), typeof(Demo), new PropertyMetadata(false));

    public static bool GetOpen(DependencyObject o) => (bool)o.GetValue(OpenProperty);
    public static void SetOpen(DependencyObject o, bool value) => o.SetValue(OpenProperty, value);

    /// <summary>Marks an element as invalid with the given message so the validation look shows.</summary>
    public static readonly DependencyProperty ErrorProperty = DependencyProperty.RegisterAttached(
        "Error", typeof(string), typeof(Demo), new PropertyMetadata(null, OnErrorChanged));

    public static string? GetError(DependencyObject o) => (string?)o.GetValue(ErrorProperty);
    public static void SetError(DependencyObject o, string? value) => o.SetValue(ErrorProperty, value);

    private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not string message)
        {
            return;
        }
        fe.Loaded += (_, _) =>
        {
            var expression = BindingOperations.SetBinding(fe, FrameworkElement.TagProperty, new Binding { Source = message });
            Validation.MarkInvalid(expression, new ValidationError(new ExceptionValidationRule(), expression, message, null));
        };
    }
}

public sealed record Timesheet(string Employee, string Department, double Hours, int Week, string Status);

public sealed record Company(int Id, string Name, string City);

/// <summary>Fixed lists for the SearchComboBox samples.</summary>
public static class DemoData
{
    private static readonly string[] Names =
    {
        "A. Datum", "Adventure Works", "Alpine Ski House", "Bellows College", "Best For You Organics", "Blue Yonder Airlines",
        "City Power & Light", "Coho Winery", "Consolidated Messenger", "Contoso", "Fabrikam", "Fincher Architects", "Fourth Coffee",
        "Graphic Design Institute", "Humongous Insurance", "Lamna Healthcare", "Litware", "Lucerne Publishing", "Margie's Travel",
        "Munson's Pickles", "Nod Publishers", "Northwind Traders", "Proseware", "Relecloud", "School of Fine Art", "Southridge Video",
        "Tailspin Toys", "Tailwind Traders", "Trey Research", "VanArsdel", "Wide World Importers", "Wingtip Toys", "Woodgrove Bank",
        "Alpine Mountain", "Coastal Freight", "Hillside Dairy", "Lakeshore Energy", "Northern Lights Media", "Riverbend Foods",
        "Summit Engineering",
    };

    private static readonly string[] Branches =
    {
        "Consulting", "Holdings", "Logistics", "Retail", "Finance", "Labs", "Media", "Services", "Systems", "Group",
        "Partners", "Studios", "Software", "Supply", "Energy", "Health", "Foods", "Security", "Analytics", "Travel",
        "Design", "Motors", "Marine", "Robotics", "Capital", "Imports", "Exports", "Networks", "Outdoor", "Print",
        "Research", "Telecom", "Textiles", "Training", "Ventures", "Water", "Wholesale", "Works", "Hospitality", "Agency",
        "Construction", "Pharma", "Rentals", "Recycling", "Solar", "Clinics", "Academy", "Bakery", "Brewing", "Catering",
    };

    private static readonly string[] Cities = { "Seattle", "Portland", "Denver", "Boston", "Chicago", "Austin", "Copenhagen", "Oslo" };

    /// <summary>2,000 companies, sorted by name.</summary>
    public static readonly Company[] Companies = Names
        .SelectMany(n => Branches.Select(b => $"{n} {b}"))
        .Order(StringComparer.Ordinal)
        .Select((name, i) => new Company(1001 + i, name, Cities[i % Cities.Length]))
        .ToArray();

    public static readonly string[] CarBrands =
    {
        "Alfa Romeo", "Aston Martin", "Audi", "Bentley", "BMW", "Bugatti", "Cadillac", "Chevrolet", "Chrysler", "Citroën", "Cupra",
        "Dacia", "Dodge", "DS", "Ferrari", "Fiat", "Ford", "Genesis", "Honda", "Hyundai", "Jaguar", "Jeep", "Kia", "Lamborghini",
        "Land Rover", "Lexus", "Lotus", "Maserati", "Mazda", "McLaren", "Mercedes-Benz", "MG", "Mini", "Mitsubishi", "Nissan",
        "Opel", "Peugeot", "Polestar", "Porsche", "Renault", "Rolls-Royce", "Seat", "Škoda", "Smart", "Subaru", "Suzuki", "Tesla",
        "Toyota", "Volkswagen", "Volvo",
    };
}
