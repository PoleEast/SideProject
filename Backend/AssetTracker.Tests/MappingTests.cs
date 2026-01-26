using Mapster;
using Project.Data.Model;
using Project.Shared.DTOs.Auth;

namespace AssetTracker.Tests
{
    /// <summary>
    /// Mapster 設定 Fixture，確保整個測試類別只初始化一次
    /// </summary>
    public class MapsterFixture
    {
        public MapsterFixture()
        {
            MapsterConfig.SettingGlobalConfig();
        }
    }

    public class MappingTest(MapsterFixture fixture) : IClassFixture<MapsterFixture>
    {
        private readonly MapsterFixture _fixture = fixture;

        [Fact]
        public void RegisterRequest_To_User_ShouldMapCorrectly()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Account = "testuser",
                Password = "testpassword123",
                Name = "Test User"
            };

            // Act
            var user = request.Adapt<User>();

            // Assert
            Assert.Equal(request.Account, user.Account);
            Assert.Equal(request.Name, user.Name);

            // Password 不應該被映射（名稱不同：Password vs PasswordHash）
            Assert.Equal(string.Empty, user.PasswordHash);
        }
    }
}
