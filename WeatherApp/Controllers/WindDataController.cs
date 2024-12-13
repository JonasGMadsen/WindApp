using Microsoft.AspNetCore.Mvc;

namespace WeatherApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WindDataController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetWindData()
        {
            var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "winddata.json"); //Change the path to the location of the winddata.json file

            if (!System.IO.File.Exists(jsonFilePath))
            {
                return NotFound();
            }

            var jsonData = await System.IO.File.ReadAllTextAsync(jsonFilePath);

            return Content(jsonData, "application/json");
        }
    }
}