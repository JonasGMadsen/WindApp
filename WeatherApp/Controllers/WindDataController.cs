using Microsoft.AspNetCore.Mvc;

namespace WeatherApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WindDataController : ControllerBase
    {

        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public WindDataController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        [Route("local")]
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

        [HttpGet]
        [Route("remote")]
        public async Task<IActionResult> GetRemoteWindData()
        {
            // 1) Get the secret URL & token from config
            var compressedFileUrl = _config["AzureSettings:CompressedWindDataUrl"];
            if (string.IsNullOrEmpty(compressedFileUrl))
            {
                return BadRequest("Azure compressed file URL not configured.");
            }

            // 2) Use HttpClientFactory to fetch the remote compressed data
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(compressedFileUrl, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to get remote wind data from storage.");
            }

            // 3) Return the Brotli stream directly to the browser.
            //    The MIME type here is not strictly "application/brotli" in official registries,
            //    but it’s fine to use if your front-end expects it.
            var contentStream = await response.Content.ReadAsStreamAsync();
            return File(contentStream, "application/brotli");
        }
    }
}