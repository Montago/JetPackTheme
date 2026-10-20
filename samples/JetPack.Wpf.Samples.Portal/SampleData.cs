namespace JetPack.Wpf.Samples.Portal;

// Fixed sample content for the pages. A real application would load this from its own data layer.

public sealed record Product(string Name, string Category, string Supplier, decimal Price, int InStock, int ReorderLevel,
    int Rating, bool Discontinued, string Description)
{
    public bool IsLow => InStock <= ReorderLevel;
}

public sealed record OrderLine(string Product, int Quantity, decimal Price)
{
    public decimal Amount => Quantity * Price;
}

public sealed record Order(string Number, string Customer, DateTime Date, string Status, string ShipTo, IReadOnlyList<OrderLine> Lines)
{
    public decimal Total => Lines.Sum(l => l.Amount);
}

public sealed record Customer(string Company, string Contact, string Title, string Phone, string Email, string City, string Country)
{
    public IEnumerable<Order> Orders => SampleData.Orders.Where(o => o.Customer == Company);
}

public sealed record Country(string Name, IReadOnlyList<Customer> Customers);

public static class SampleData
{
    public static IReadOnlyList<string> Categories { get; } = ["All categories", "Beverages", "Condiments", "Confections", "Dairy", "Produce", "Seafood"];

    public static IReadOnlyList<string> OrderStatuses { get; } = ["All orders", "New", "Packing", "Shipped", "Delivered"];

    public static IReadOnlyList<Product> Products { get; } =
    [
        new("Chai", "Beverages", "Exotic Liquids", 18.00m, 39, 10, 5, false, "Spiced black tea from Assam, 10 boxes of 20 bags."),
        new("Chang", "Beverages", "Exotic Liquids", 19.00m, 17, 25, 4, false, "Light lager brewed in Chiang Mai, 24 bottles of 12 oz."),
        new("Aniseed Syrup", "Condiments", "Exotic Liquids", 10.00m, 13, 25, 3, false, "Sweet syrup for desserts and cocktails, 12 bottles of 550 ml."),
        new("Chef Anton's Cajun Seasoning", "Condiments", "New Orleans Cajun Delights", 22.00m, 53, 0, 4, false, "A classic blend of paprika, garlic and cayenne, 48 jars of 6 oz."),
        new("Grandma's Boysenberry Spread", "Condiments", "Grandma Kelly's Homestead", 25.00m, 120, 25, 5, false, "Slow-cooked berry spread, 12 jars of 8 oz."),
        new("Pavlova", "Confections", "Pavlova, Ltd.", 17.45m, 29, 10, 4, false, "Meringue cake with a crisp crust and soft centre, 32 boxes of 500 g."),
        new("Teatime Chocolate Biscuits", "Confections", "Specialty Biscuits, Ltd.", 9.20m, 25, 5, 3, false, "Butter biscuits dipped in milk chocolate, 10 boxes of 12 pieces."),
        new("Gorgonzola Telino", "Dairy", "Formaggi Fortini s.r.l.", 12.50m, 0, 20, 4, false, "Creamy blue cheese from Lombardy, 12 packs of 100 g."),
        new("Mascarpone Fabioli", "Dairy", "Formaggi Fortini s.r.l.", 32.00m, 9, 25, 5, false, "Fresh Italian cream cheese, 24 tubs of 200 g."),
        new("Uncle Bob's Organic Dried Pears", "Produce", "Grandma Kelly's Homestead", 30.00m, 15, 10, 4, false, "Sun-dried pears without added sugar, 12 packs of 1 lb."),
        new("Tofu", "Produce", "Mayumi's", 23.25m, 35, 0, 3, false, "Firm tofu made the traditional way, 40 packs of 100 g."),
        new("Ikura", "Seafood", "Tokyo Traders", 31.00m, 31, 0, 5, false, "Salmon roe, 12 jars of 200 ml."),
        new("Boston Crab Meat", "Seafood", "New England Seafood Cannery", 18.40m, 123, 30, 4, false, "Hand-picked crab meat, 24 tins of 4 oz."),
        new("Røgede sild", "Seafood", "Lyngbysild", 9.50m, 5, 15, 3, true, "Smoked herring, 1 kg packs. Replaced by the 500 g pack from 2027."),
    ];

    public static IReadOnlyList<Product> TopRated { get; } = Products.Where(p => p.Rating == 5).ToList();

    public static IReadOnlyList<Order> Orders { get; } =
    [
        new("SO-10842", "Alfreds Futterkiste", new(2026, 10, 16), "New", "Berlin, Germany", [new("Chai", 12, 18.00m), new("Pavlova", 6, 17.45m)]),
        new("SO-10841", "Ernst Handel", new(2026, 10, 15), "Packing", "Graz, Austria", [new("Boston Crab Meat", 30, 18.40m), new("Ikura", 8, 31.00m), new("Tofu", 20, 23.25m)]),
        new("SO-10840", "Around the Horn", new(2026, 10, 15), "Packing", "London, UK", [new("Chang", 24, 19.00m)]),
        new("SO-10839", "Bon app'", new(2026, 10, 14), "Shipped", "Marseille, France", [new("Mascarpone Fabioli", 10, 32.00m), new("Gorgonzola Telino", 12, 12.50m)]),
        new("SO-10838", "Simons bistro", new(2026, 10, 13), "Shipped", "Copenhagen, Denmark", [new("Røgede sild", 15, 9.50m), new("Chai", 4, 18.00m)]),
        new("SO-10837", "Alfreds Futterkiste", new(2026, 10, 12), "Delivered", "Berlin, Germany", [new("Grandma's Boysenberry Spread", 20, 25.00m)]),
        new("SO-10836", "Great Lakes Food Market", new(2026, 10, 9), "Delivered", "Eugene, USA", [new("Chef Anton's Cajun Seasoning", 18, 22.00m), new("Boston Crab Meat", 40, 18.40m)]),
        new("SO-10835", "Ernst Handel", new(2026, 10, 8), "Delivered", "Graz, Austria", [new("Teatime Chocolate Biscuits", 50, 9.20m)]),
        new("SO-10834", "Bon app'", new(2026, 10, 7), "Delivered", "Marseille, France", [new("Uncle Bob's Organic Dried Pears", 14, 30.00m), new("Pavlova", 10, 17.45m)]),
        new("SO-10833", "Around the Horn", new(2026, 10, 6), "Delivered", "London, UK", [new("Ikura", 6, 31.00m)]),
    ];

    public static IReadOnlyList<Country> Countries { get; } =
    [
        new("Austria", [new("Ernst Handel", "Roland Mendel", "Sales manager", "7675-3425", "roland@ernsthandel.at", "Graz", "Austria")]),
        new("Denmark", [new("Simons bistro", "Jytte Petersen", "Owner", "31 12 34 56", "jytte@simonsbistro.dk", "Copenhagen", "Denmark")]),
        new("France", [new("Bon app'", "Laurence Lebihan", "Owner", "91.24.45.40", "laurence@bonapp.fr", "Marseille", "France")]),
        new("Germany",
        [
            new("Alfreds Futterkiste", "Maria Anders", "Sales representative", "030-0074321", "maria@alfreds.de", "Berlin", "Germany"),
            new("Blauer See Delikatessen", "Hanna Moos", "Sales representative", "0621-08460", "hanna@blauersee.de", "Mannheim", "Germany"),
        ]),
        new("United Kingdom", [new("Around the Horn", "Thomas Hardy", "Sales representative", "(171) 555-7788", "thomas@aroundthehorn.co.uk", "London", "UK")]),
        new("United States", [new("Great Lakes Food Market", "Howard Snyder", "Marketing manager", "(503) 555-7555", "howard@greatlakes.com", "Eugene", "USA")]),
    ];

    public static IReadOnlyList<string> CustomerNames { get; } = Countries.SelectMany(c => c.Customers).Select(c => c.Company).Order().ToList();
}
