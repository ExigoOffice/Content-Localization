using Content.Localization.Tests;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Content.Localization.AspNetCore.Tests
{
    public sealed class DependancyInjectionTests : IDisposable
    {
   private readonly string  _location;
        public DependancyInjectionTests()
        {
            _location = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_location);
        }

        public void Dispose()
        {
            Directory.Delete(_location, true);
        }


        public void Configuration_With_DependancyInjection_Localizes()
        {
            //Arrange
            var source = new MockContentSource();
            source.SetData("en-US", new Dictionary<string, string> { { "A", "ValA"} });

            var sp = new ServiceCollection()
                .AddContentLocalization()
                    .AddMemorySource()
                    .AddProtoFileSource( o => o.Location = _location )
                    .AddContentSource( () => source )
                .GetServices()                        
                .BuildServiceProvider();
                
            //Act
            var localizer = sp.GetRequiredService<IContentLocalizer>();

            //Assert
            Assert.Equal("ValA", localizer["A"]);
        }

    }
}
