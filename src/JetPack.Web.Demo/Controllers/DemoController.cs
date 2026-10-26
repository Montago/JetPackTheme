using Microsoft.AspNetCore.Mvc;

namespace JetPack.Web.Demo.Controllers;

public class DemoController : Controller
{
    public IActionResult Index() => View();

    // One sample per control for the docs pictures (GalleryPage.xaml of the WPF demo)
    public IActionResult Gallery() => View();
}
