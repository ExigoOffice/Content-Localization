using Content.Localization.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace Content.Localization.AspNetFramework.Tests
{
    public class StaticContentClassGeneratorTests : IDisposable
    {

        private readonly string  _location;
        private readonly ITestOutputHelper _output;

        public StaticContentClassGeneratorTests(ITestOutputHelper output)
        {
            _location = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_location);
            _output = output;
        }

        public void Dispose()
        {
            Directory.Delete(_location, true);
        }

        [Fact]
        public async Task GenerateAndSaveIfChangedAsync_Writes_File()
        {
            //Arrange
            var generator = new StaticContentClassGenerator(new ClassGeneratorOptions
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
        public async Task Carousel_GenerateAndSaves()
        {
            //Arrange
            var generator = new StaticContentClassGenerator(new ClassGeneratorOptions
            {
                 ClassName = "OurTest",
                 Location  = _location
            });

            var source = new MockContentSource();
            source.SetData("en-US", 
                new Dictionary<string, string> { 
                    { "A", "ValA" },
                    { "C", @"<exigocarousel><exigocarouselattributes type=""bootstrap3"" /><exigobanner name=""Banner_One"" /><exigobanner name=""Banner_Two"" /></exigocarousel>" },
                    { "Banner_One", "Banner_One_Value" },
                    { "Banner_Two", "Banner_Two_Value" },
                });


            //Act
            await generator.GenerateAndSaveIfChangedAsync(new ContentVersion {  Version="1.0"}, source);

            //Assert
            var contents = File.ReadAllText(generator.GetFullFileName());
            _output.WriteLine(contents);

            Assert.Contains("string A =>", contents);
            

        }



        [Fact]
        public async Task GenerateAndSaveIfChangedAsync_Pulls_Version_FromHeader()
        {
            //Arrange
            var options = new ClassGeneratorOptions
            {
                 ClassName = "OurTest",
                 Location  = _location
            }; 

            var generator = new StaticContentClassGenerator(options);

            var source = new MockContentSource();
            source.SetData("en-US", new Dictionary<string, string> { { "A", "ValA"} });
            await generator.GenerateAndSaveIfChangedAsync(new ContentVersion {  Version="1.0", ReleaseDate = new DateTime(2020,1,1)}, source);


            //Act
            var generator2 = new StaticContentClassGenerator(options);
            var version = await generator2.GetExistingVersionAsync();

            //Assert
            Assert.Equal("1.0", version.Version);
            Assert.Equal(new DateTime(2020,1,1), version.ReleaseDate);

        }


        [Fact]
        public async Task GeneratedFile_OnlyUpdates_For_New_Version()
        {
            //Arrange
            var options = new ClassGeneratorOptions
            {
                 ClassName = "OurTest",
                 Location  = _location
            }; 

            var generator = new StaticContentClassGenerator(options);

            var source = new MockContentSource();
            source.SetData("en-US", new Dictionary<string, string> { { "A", "ValA"} });

            await Task.Delay(100);

            //Act
            await generator.GenerateAndSaveIfChangedAsync(new ContentVersion {  Version="1.0", ReleaseDate = new DateTime(2020,1,1)}, source);
            var date1 = File.GetLastWriteTime(generator.GetFullFileName());

            await Task.Delay(100);

            //same version
            await generator.GenerateAndSaveIfChangedAsync(new ContentVersion {  Version="1.0", ReleaseDate = new DateTime(2020,1,1)}, source);
            var date2 = File.GetLastWriteTime(generator.GetFullFileName());

            await Task.Delay(100);

            //new version
            await generator.GenerateAndSaveIfChangedAsync(new ContentVersion {  Version="2.0", ReleaseDate = new DateTime(2020,1,1)}, source);
            var date3 = File.GetLastWriteTime(generator.GetFullFileName());


            //Assert
            Assert.Equal(date1, date2);
            Assert.NotEqual(date1, date3);

        }

    }
}
