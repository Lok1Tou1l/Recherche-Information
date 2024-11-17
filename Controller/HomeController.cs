using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

public class HomeController : Controller
{
    private static readonly List<string> documents = new List<string>
    {
        "AI models are used in game development.",
        "Game development involves AI techniques.",
        "Python is widely used in AI and machine learning.",
        "Game engines like Unity and Unreal offer tools for game development."
    };

    private static readonly VectorSearchModel vectorSearchModel = new VectorSearchModel(documents);

    [HttpGet]
    public IActionResult Search(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return Content("No query provided.");
        }

        string bestMatch = vectorSearchModel.Search(query);
        return Content($"Best matching document: {bestMatch}");
    }

    public IActionResult Index()
    {
        return View();
    }
}
