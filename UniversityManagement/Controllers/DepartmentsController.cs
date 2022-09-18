using Microsoft.AspNetCore.Mvc;

namespace UniversityManagement.Controllers;

public class DepartmentsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}