using Microsoft.AspNetCore.Mvc;

namespace WeatherApp.Controllers
{
    //Can easily be expanded with more HttpGet methods.
    [ApiController]
    [Route("api/[controller]")]
    public class WindDataController(IConfiguration config, IHttpClientFactory httpClientFactory) : ControllerBase
    {
        [HttpGet]
        [Route("local")]
        public async Task<IActionResult> GetWindData()
        {
            var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "winddata.json"); //change the path to the location of the winddata.json file. Or change to match the file name and location in project.

            if (!System.IO.File.Exists(jsonFilePath))
            {
                return NotFound();
            }

            var jsonData = await System.IO.File.ReadAllTextAsync(jsonFilePath);

            return Content(jsonData, "application/json");
        }

        [HttpGet]
        [Route("remote")]
        public async Task<IActionResult> GetRemoteWindData()
        {
            var compressedFileUrl = config["AzureSettings:CompressedWindDataUrl"];
            if (string.IsNullOrEmpty(compressedFileUrl))
            {
                return BadRequest("Azure compressed file URL not configured.");
            }

            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync(compressedFileUrl, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to get remote wind data from storage.");
            }

            var contentStream = await response.Content.ReadAsStreamAsync();
            return File(contentStream, "application/brotli");
        }
    }
}