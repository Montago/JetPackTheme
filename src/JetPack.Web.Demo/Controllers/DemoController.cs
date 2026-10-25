using Microsoft.AspNetCore.Mvc;

namespace JetPack.Web.Demo.Controllers;

public class DemoController : Controller
{
    public IActionResult Index() => View();
}
