using AutoMapper;
using Xunit;

namespace AudioStore.Tests.UnitTests.Mappings;

public class MappingProfileTests
{
    [Fact]
    public void AutoMapper_Configuration_ShouldBeValid()
    {
        // Arrange & Act
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(AudioStore.Application.Mapping.MappingProfile).Assembly);
        });

        // Assert - This will throw if configuration is invalid
        configuration.AssertConfigurationIsValid();
    }
}
