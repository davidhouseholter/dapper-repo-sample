using System.Diagnostics;
using Core.Repository.Entity;
using Microsoft.AspNetCore.Mvc;
using SampleMVC.Models;
using SampleMVC.Services;

namespace SampleMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _userService;
        public HomeController(ILogger<HomeController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        public IActionResult Index()
        {
            var userNew = _userService.AddUser($"John {DateTime.Now}");
            var user = _userService.GetUserByID(userNew.Id);
            return View();
        }
      
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
