using Content.Localization.Tests;

using System;
using System.Collections.Generic;
using System.IO;

using Xunit;

namespace Content.Localization.AspNetFramework.Tests
{
    public sealed class LocalizerConfigurationTests : IDisposable
    {
        private readonly string  _location;
        public LocalizerConfigurationTests()
        {
            _location = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_location);
        }

        public void Dispose()
        {
            Directory.Delete(_location, true);
        }

        [Fact]
        public void LocalizerConfuration_WithMemory_File_AndMock_BuildsAndLocalizes()
        {
            //Arrange
            var source = new MockContentSource();
            source.SetData("en-US", new Dictionary<string, string> { { "A", "ValA"} });

            //Act
            var localizer = new LocalizerConfiguration()
                .AddMemorySource()
                .AddProtoFileSource( o => o.Location = _location )
                .AddContentSource( () => source )
                .BuildLocalizer();

            //Assert
            Assert.Equal("ValA", localizer["A"] );
        }
        


    }
}
