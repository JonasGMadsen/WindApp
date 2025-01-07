using System.Net;
using System.Text;
using Moq;
using Microsoft.Extensions.Configuration;
using WeatherApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq.Protected;

namespace WeatherAppTest
{
    public class WindDataControllerTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

        public WindDataControllerTests()
        {
            _configMock = new Mock<IConfiguration>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        }

        [Fact]
        public async Task GetWindData_FileDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var controller = new WindDataController(_configMock.Object, _httpClientFactoryMock.Object);

            // In this test I don't create a winddata.json file, so it will not exist

            // Act
            var result = await controller.GetWindData();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetWindData_FileExists_ReturnsJsonContent()
        {
            // Arrange
            var controller = new WindDataController(_configMock.Object, _httpClientFactoryMock.Object);

            // Temp folder and file
            var testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDirectory);

            var dataDirectory = Path.Combine(testDirectory, "wwwroot", "data");
            Directory.CreateDirectory(dataDirectory);

            var testFilePath = Path.Combine(dataDirectory, "winddata.json");
            const string sampleJson = "{\"key\":\"value\"}";
            await File.WriteAllTextAsync(testFilePath, sampleJson);

            // Change the current directory to this test directory 
            // so the controller picks it up in GetCurrentDirectory().
            Directory.SetCurrentDirectory(testDirectory);

            // Act
            var result = await controller.GetWindData();

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal(sampleJson, contentResult.Content);

            // Clean up temp
            Directory.SetCurrentDirectory(Path.GetTempPath()); // revert
            Directory.Delete(testDirectory, true);
        }

        [Fact]
        public async Task GetRemoteWindData_NoUrlConfigured_ReturnsBadRequest()
        {
            // Arrange
            // Not setting any "AzureSettings:CompressedWindDataUrl" in configMock
            var controller = new WindDataController(_configMock.Object, _httpClientFactoryMock.Object);

            // Act
            var result = await controller.GetRemoteWindData();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Azure compressed file URL not configured.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetRemoteWindData_FailToGetData_ReturnError()
        {
            // Arrange
            _configMock.Setup(c => c["AzureSettings:CompressedWindDataUrl"])
                       .Returns("http://example.com/invalid");

            // Setting up a mock HttpMessageHandler to simulate a non-success status code
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadGateway,
                    Content = new StringContent("Failed to get remote wind data from storage.")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>()))
                                 .Returns(httpClient);

            var controller = new WindDataController(_configMock.Object, _httpClientFactoryMock.Object);

            // Act
            var result = await controller.GetRemoteWindData();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal((int)HttpStatusCode.BadGateway, statusCodeResult.StatusCode);
            Assert.Equal("Failed to get remote wind data from storage.", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetRemoteWindData_SuccessfullyReturnsFile()
        {
            // Arrange
            _configMock.Setup(c => c["AzureSettings:CompressedWindDataUrl"])
                       .Returns("http://blobtest.com/compressedwinddata.br");

            var sampleCompressedBytes = "brotli compressed content"u8.ToArray();
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(sampleCompressedBytes)
                });

            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>()))
                                 .Returns(httpClient);

            var controller = new WindDataController(_configMock.Object, _httpClientFactoryMock.Object);

            // Act
            var result = await controller.GetRemoteWindData();

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("application/brotli", fileResult.ContentType);
        }
    }
}