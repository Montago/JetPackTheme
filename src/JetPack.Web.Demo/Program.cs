// The JetPack demo for the web: one page with the tabs of src/JetPack.Wpf.Demo, each tab a partial
// view named after the WPF UserControl (Views/Demo/CorePage.cshtml ↔ Views/CorePage.xaml).
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

var app = builder.Build();

// en-US, as the WPF demo forces for its snapshots
app.UseRequestLocalization("en-US");
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Demo}/{action=Index}");

app.Run();
