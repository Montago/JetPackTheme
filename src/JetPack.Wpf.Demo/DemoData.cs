// The demo's fixed data. Plain C# without WPF types, so the web demo (src/JetPack.Web.Demo) links
// this file and shows exactly the same rows.
namespace JetPack.Wpf.Demo;

public sealed record Timesheet(string Employee, string Department, double Hours, int Week, string Status);

public sealed record Company(int Id, string Name, string City);

/// <summary>Fixed lists for the samples.</summary>
public static class DemoData
{
    /// <summary>The DataGrid and ListView samples.</summary>
    public static readonly Timesheet[] Timesheets =
    [
        new("Charles Hoyle", "Consulting", 37.5, 43, "Approved"),
        new("Daniel Tiedemann", "Development", 41.0, 43, "Pending"),
        new("David Ritter", "Development", 32.0, 43, "Draft"),
        new("Ida Kragh", "Finance", 37.0, 43, "Approved"),
        new("Lindsey Ballard", "Sales", 18.5, 43, "Rejected"),
        new("Mads Lund", "Support", 39.5, 43, "Approved"),
        new("Vivian Smail", "Sales", 36.0, 43, "Approved"),
    ];

    /// <summary>The AutoSuggestBox sample.</summary>
    public static readonly string[] SuggestCompanies =
    [
        "Contoso Corporation", "Fabrikam", "Fabrikam Residences", "Southridge Video", "Soar Airlines", "Margie's Travel", "Northwind Traders", "Fourth Coffee",
    ];

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

public sealed record Car(
    int StockNo, string Make, string Model, string Trim, string Body, int Year, int Mileage, string Fuel, string Gearbox, string Drive,
    int Power, int Co2, string Colour, int Doors, int Seats, int Owners, DateTime Registered, decimal Price, string Dealer, string Status)
{
    /// <summary>For the check box column of the JetPackGrid samples.</summary>
    public bool IsSold => Status == "Sold";
}

/// <summary>3,000 used cars with 20 columns for the JetPackGrid samples; the same every run.</summary>
public static class CarData
{
    private sealed record CarModel(string Make, string Model, string Body, bool Electric, int Price, int Seats = 5);

    private static readonly CarModel[] Models =
    {
        new("Volvo", "XC40", "SUV", false, 42000), new("Volvo", "XC60", "SUV", false, 55000), new("Volvo", "XC90", "SUV", false, 72000, 7),
        new("Volvo", "V60", "Estate", false, 46000), new("Volvo", "V90", "Estate", false, 58000), new("Volvo", "S60", "Sedan", false, 45000),
        new("Volvo", "S90", "Sedan", false, 57000), new("Volvo", "EX30", "SUV", true, 38000),
        new("Toyota", "Yaris", "Hatchback", false, 21000), new("Toyota", "Corolla", "Hatchback", false, 28000), new("Toyota", "Corolla Touring", "Estate", false, 30000),
        new("Toyota", "RAV4", "SUV", false, 39000), new("Toyota", "C-HR", "SUV", false, 33000), new("Toyota", "Camry", "Sedan", false, 37000),
        new("Volkswagen", "Polo", "Hatchback", false, 20000), new("Volkswagen", "Golf", "Hatchback", false, 29000), new("Volkswagen", "Passat Variant", "Estate", false, 41000),
        new("Volkswagen", "Tiguan", "SUV", false, 40000), new("Volkswagen", "ID.4", "SUV", true, 45000), new("Volkswagen", "Touran", "Van", false, 34000, 7),
        new("BMW", "1 Series", "Hatchback", false, 32000), new("BMW", "3 Series", "Sedan", false, 45000), new("BMW", "3 Series Touring", "Estate", false, 47000),
        new("BMW", "X3", "SUV", false, 56000), new("BMW", "i4", "Coupe", true, 59000), new("BMW", "Z4", "Convertible", false, 52000, 2),
        new("Ford", "Fiesta", "Hatchback", false, 19000), new("Ford", "Focus", "Hatchback", false, 26000), new("Ford", "Kuga", "SUV", false, 35000),
        new("Ford", "Mustang", "Coupe", false, 55000, 4), new("Ford", "Transit Custom", "Van", false, 38000, 3),
        new("Skoda", "Fabia", "Hatchback", false, 19000), new("Skoda", "Octavia Combi", "Estate", false, 30000), new("Skoda", "Superb Combi", "Estate", false, 40000),
        new("Skoda", "Kodiaq", "SUV", false, 42000, 7), new("Skoda", "Enyaq", "SUV", true, 46000),
        new("Audi", "A3 Sportback", "Hatchback", false, 34000), new("Audi", "A4 Avant", "Estate", false, 46000), new("Audi", "A6", "Sedan", false, 58000),
        new("Audi", "Q5", "SUV", false, 57000), new("Audi", "e-tron GT", "Coupe", true, 105000, 4),
        new("Peugeot", "208", "Hatchback", false, 21000), new("Peugeot", "308", "Hatchback", false, 28000), new("Peugeot", "3008", "SUV", false, 36000),
        new("Peugeot", "5008", "SUV", false, 40000, 7),
        new("Mercedes-Benz", "A-Class", "Hatchback", false, 35000), new("Mercedes-Benz", "C-Class", "Sedan", false, 49000),
        new("Mercedes-Benz", "E-Class Estate", "Estate", false, 63000), new("Mercedes-Benz", "GLC", "SUV", false, 60000),
        new("Tesla", "Model 3", "Sedan", true, 42000), new("Tesla", "Model Y", "SUV", true, 46000),
        new("Kia", "Picanto", "Hatchback", false, 15000), new("Kia", "Ceed", "Hatchback", false, 24000), new("Kia", "Sportage", "SUV", false, 34000),
        new("Kia", "EV6", "SUV", true, 48000),
        new("Renault", "Clio", "Hatchback", false, 19000), new("Renault", "Megane", "Hatchback", false, 25000), new("Renault", "Captur", "SUV", false, 26000),
        new("Renault", "Kangoo", "Van", false, 27000),
    };

    private static readonly string[] Trims = { "Base", "Comfort", "Sport", "Business", "Premium", "Edition", "Plus", "Line" };
    private static readonly string[] Colours = { "Black", "White", "Silver", "Grey", "Blue", "Red", "Green", "Beige", "Brown", "Dark Blue" };
    private static readonly string[] Dealers = { "Copenhagen", "Aarhus", "Odense", "Aalborg", "Esbjerg", "Oslo", "Malmö", "Gothenburg" };

    public static readonly Car[] Cars = Create(3000);

    private static Car[] Create(int count)
    {
        var random = new Random(2026);
        var cars = new Car[count];
        for (var i = 0; i < count; i++)
        {
            var model = Models[random.Next(Models.Length)];
            var year = 2014 + random.Next(12);
            var age = 2026 - year;
            var fuel = model.Electric ? "Electric" : random.Next(100) switch { < 45 => "Petrol", < 75 => "Diesel", _ => "Hybrid" };
            var gearbox = model.Electric || fuel == "Hybrid" || random.Next(100) < 50 ? "Automatic" : "Manual";
            var drive = model.Make is "BMW" or "Mercedes-Benz" || model.Model == "Mustang" ? "RWD"
                : model.Body == "SUV" && random.Next(100) < 45 || model.Electric && random.Next(100) < 30 ? "AWD" : "FWD";
            var doors = model.Body switch { "Coupe" or "Convertible" => 2, "Hatchback" when random.Next(100) < 20 => 3, "Van" => 4, _ => 5 };
            var power = model.Price / 300 + random.Next(-20, 60);
            var co2 = model.Electric ? 0 : fuel == "Hybrid" ? 95 + random.Next(30) : 115 + random.Next(80);
            var price = Math.Round(model.Price * (decimal)Math.Pow(0.87, age) * (0.9m + (decimal)random.NextDouble() * 0.2m) / 100) * 100;
            var status = random.Next(100) switch { < 72 => "In stock", < 87 => "Reserved", _ => "Sold" };
            cars[i] = new Car(
                10001 + i, model.Make, model.Model, Trims[random.Next(Trims.Length)], model.Body, year,
                Math.Max(age, 1) * (6000 + random.Next(16000)), fuel, gearbox, drive, power, co2, Colours[random.Next(Colours.Length)],
                doors, model.Seats, 1 + random.Next(Math.Min(age / 3 + 1, 4)), new DateTime(year, 1 + random.Next(12), 1 + random.Next(28)),
                price, Dealers[random.Next(Dealers.Length)], status);
        }
        return cars;
    }
}
