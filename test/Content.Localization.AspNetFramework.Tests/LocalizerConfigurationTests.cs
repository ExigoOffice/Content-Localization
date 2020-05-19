using Content.Localization.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
        

        [Fact]
        public async Task Memory_Over_Prto_Over_MockSource_Updates_Through_Stack()
        {
            //Arrange
            var source = new MockContentSource();

            var version1 = new ContentVersion {  ReleaseDate = new DateTime(2020, 1, 1)};

            source.SetData(version1, "en-US", new Dictionary<string, string> { { "A", "ValA"} });

            var localizer = new LocalizerConfiguration()
                .AddMemorySource()
                .AddProtoFileSource( o => o.Location = _location )
                .AddContentSource( () => source )
                .AddUpdater( o=> 
                { 
                    o.StartupDelay  = TimeSpan.Zero;
                    o.Frequency     = TimeSpan.FromMilliseconds(1); 
                })
                .BuildLocalizer();

            //Act/Assert
            Assert.Equal( "ValA", localizer["A"] );

            //new version
            var version2 = new ContentVersion {  ReleaseDate = new DateTime(2020, 1, 2)};
            source.SetData(version2, "en-US", new Dictionary<string, string> { { "A", "ValA-2"} });

            

            //give the updater a chance to do its magic
            await Task.Delay(500);
            
            Assert.Equal( "ValA-2", localizer["A"] );
            //Do we have the class file? 

            ((IDisposable)localizer).Dispose();
        }

        [Fact]
        public async Task ClassGeneration_Updtes_Through_Stack()
        {
            //Arrange
            var source = new MockContentSource();

            var version1 = new ContentVersion {  ReleaseDate = new DateTime(2020, 1, 1)};
            source.SetData(version1, "en-US", new Dictionary<string, string> { { "A", "ValA"} });

            var localizer = new LocalizerConfiguration()
                .AddMemorySource()
                .AddProtoFileSource( o => o.Location = _location )
                .AddContentSource( () => source )
                .AddUpdater( o=> 
                { 
                    o.StartupDelay  = TimeSpan.Zero;
                    o.Frequency     = TimeSpan.FromMilliseconds(1); 
                })
                .AddClassGenerator( o=>
                {
                    o.Location      = _location;
                    o.ClassName     = "OurClass";
                })
                .BuildLocalizer();

            //Act/Assert
            // new version

            await Task.Delay(100);

            var version2 = new ContentVersion {  ReleaseDate = new DateTime(2020, 1, 2) };
            source.SetData(version2, "en-US", new Dictionary<string, string> { { "A", "ValA-2" } });

            // give the updater a chance to do its magic
            await Task.Delay(100);

            var generator  = new StaticContentClassGenerator(new ClassGeneratorOptions
            {
                 ClassName = "OurClass",
                 Location  = _location
            });   
            
            ((IDisposable)localizer).Dispose();

            var version = await generator.GetExistingVersionAsync();
            Assert.Equal(version2.ReleaseDate, version.ReleaseDate);

            
        }





    }
}
