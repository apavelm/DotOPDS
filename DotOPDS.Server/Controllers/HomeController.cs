using DotOPDS.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotOPDS.Server.Controllers
{
    [Authorize(Constants.AuthCookiePolicy)]
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

    }
}
