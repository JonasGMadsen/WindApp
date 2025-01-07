using System.Text;

namespace WeatherAppTest;

public class ChangeLocalDataTests
{
    //TODO consider using reflection instead just copying the code
    private static bool IsValidJsonTestWrapper(Stream stream)
    {
        try
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            var content = reader.ReadToEnd();
            System.Text.Json.JsonDocument.Parse(content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public void IsValidJson_ValidJson_ReturnsTrue()
    {
        // Arrange
        string validJson = "{\"name\":\"Drone\",\"speed\":10}";
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(validJson));

        // Act
        bool result = IsValidJsonTestWrapper(memoryStream);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidJson_InvalidJson_ReturnsFalse()
    {
        // Arrange
        // Invalid JSON by missing closing "}"
        string invalidJson = "{\"name\":\"Drone\",\"speed\":10";
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));

        // Act
        bool result = IsValidJsonTestWrapper(memoryStream);

        // Assert
        Assert.False(result);
    }
}
