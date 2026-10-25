// The JetPack admin sample as an ASP.NET Core MVC site: the same pages and data as
// samples/JetPack.Wpf.Samples.Admin, drawn with the JetPack.Web classes and Tailwind for layout.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

var app = builder.Build();

// English everywhere, so dates and numbers match the WPF sample (and type=number posts "37.5")
app.UseRequestLocalization("en-US");
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
