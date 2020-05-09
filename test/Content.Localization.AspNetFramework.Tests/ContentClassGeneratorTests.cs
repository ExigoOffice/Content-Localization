using Content.Localization.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Content.Localization.AspNetFramework.Tests
{
    public class ContentClassGeneratorTests : IDisposable
    {

        private readonly string  _location;
        public ContentClassGeneratorTests()
        {
            _location = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_location);
        }

        public void Dispose()
        {
            Directory.Delete(_location, true);
        }

        [Fact]
        public async Task GenerateAndSaveIfChangedAsync_Writes_File()
        {
            //Arrange
            var generator = new ContentClassGenerator(new ContentClassGeneratorOptions
            {
                 ClassName = "OurTest",
                 Location  = _location
            });

            var source = new MockContentSource();
            source.SetData("en-US", new Dictionary<string, string> { { "A", "ValA"} });


            //Act
            await generator.GenerateAndSaveIfChangedAsync(new ContentVersion {  Version="1.0"}, source);

            //Assert
            var contents = File.ReadAllText(generator.GetFullFileName());

            Assert.Contains("string A =>", contents);

        }

        [Fact]
        public async Task GenerateAndSaveIfChangedAsync_Pulls_Version_FromHeader()
        {
            //Arrange
            var options = new ContentClassGeneratorOptions
            {
                 ClassName = "OurTest",
                 Location  = _location
            }; 

            var generator = new ContentClassGenerator(options);

            var source = new MockContentSource();
            source.SetData("en-US", new Dictionary<string, string> { { "A", "ValA"} });
            await generator.GenerateAndSaveIfChangedAsync(new ContentVersion {  Version="1.0", ReleaseDate = new DateTime(2020,1,1)}, source);


            //Act
            var generator2 = new ContentClassGenerator(options);
            var version = await generator2.GetExistingVersionAsync();

            //Assert
            Assert.Equal("1.0", version.Version);
            Assert.Equal(new DateTime(2020,1,1), version.ReleaseDate);

        }


        [Fact]
        public async Task GeneratedFile_OnlyUpdates_For_New_Version()
        {
            //Arrange
            var options = new ContentClassGeneratorOptions
            {
                 ClassName = "OurTest",
                 Location  = _location
            }; 

            var generator = new ContentClassGenerator(options);

            var source = new MockContentSource();
            source.SetData("en-US", new Dictionary<string, string> { { "A", "ValA"} });

            //Act
            await generator.GenerateAndSaveIfChangedAsync(new ContentVersion {  Version="1.0", ReleaseDate = new DateTime(2020,1,1)}, source);
            var date1 = File.GetLastWriteTime(generator.GetFullFileName());

            //same version
            await generator.GenerateAndSaveIfChangedAsync(new ContentVersion {  Version="1.0", ReleaseDate = new DateTime(2020,1,1)}, source);
            var date2 = File.GetLastWriteTime(generator.GetFullFileName());

            //new version
            await generator.GenerateAndSaveIfChangedAsync(new ContentVersion {  Version="2.0", ReleaseDate = new DateTime(2020,1,1)}, source);
            var date3 = File.GetLastWriteTime(generator.GetFullFileName());


            //Assert
            Assert.Equal(date1, date2);
            Assert.NotEqual(date1, date3);

        }

    }
}
