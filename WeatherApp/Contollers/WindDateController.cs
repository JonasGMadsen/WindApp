using Microsoft.AspNetCore.Mvc;

namespace WeatherApp.Contollers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WindDataController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetWindData()
        {
            var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "winddata.json"); //Change

            if (!System.IO.File.Exists(jsonFilePath))
            {
                return NotFound();
            }

            var jsonData = await System.IO.File.ReadAllTextAsync(jsonFilePath);

            return Content(jsonData, "application/json");
        }
    }
}