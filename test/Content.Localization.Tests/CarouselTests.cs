using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Content.Localization.Tests
{
    public class CarouselTests
    {
        private readonly ITestOutputHelper _output;

        public CarouselTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GenerateCarousel_Merges_Two_Banners()
        {
            //Arrange
            var source = new Mock<IContentSource>();

            source.Setup(o=>o.GetContentItem("Carousel", "en-US"))
                .Returns(new ContentItem { Name = "Carousel",  Enabled = true, Value = 
                    @"<exigocarousel><exigocarouselattributes type=""bootstrap3"" /><exigobanner name=""Banner_One"" /><exigobanner name=""Banner_Two"" /></exigocarousel>"});

            source.Setup(o=>o.GetContentItem("Banner_One", "en-US"))
                .Returns(new ContentItem { Name = "Carousel",  Enabled = true, Value = 
                    @"Banner_One_Content"});

            source.Setup(o=>o.GetContentItem("Banner_Two", "en-US"))
                .Returns(new ContentItem { Name = "Carousel",  Enabled = true, Value = 
                    @"Banner_Two_Content"});

            source.Setup(contentSource => contentSource.GetAllContentItemsAsync("en-US", It.IsAny<ContentVersion>()))
                .Returns(() => Task.FromResult<IEnumerable<ContentItem>>(new[]
                {
                    new ContentItem
                    {
                        Name = "Carousel", Enabled = true, Value = @"<exigocarousel><exigocarouselattributes type=""bootstrap3"" /><exigobanner name=""Banner_One"" /><exigobanner name=""Banner_Two"" /></exigocarousel>"
                    },
                    new ContentItem
                    {
                        Name = "Banner_One", Enabled = true, Value = @"Banner_One_Content"
                    },
                    new ContentItem
                    {
                        Name = "Banner_Two", Enabled = true, Value = @"Banner_Two_Content"
                    }
                }));

            var localizer = new ContentLocalizer(source.Object, "en-US");

            //Act
            var value = localizer.GenerateCarousel("Carousel", null);
            
            //Assert
            _output.WriteLine(value);

            Assert.False(string.IsNullOrEmpty(value), "We should have a value");

            Assert.Contains("Banner_One_Content", value);
            Assert.Contains("Banner_Two_Content", value);
        }





    }
}
