using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Content.Localization.Tests
{
    public class MemoryContentStoreTests
    {
        [Fact]
        public void GetResourceItem_Called_Twice_OnlyCalls_Inner_Once()
        {
            //Arrange
            var inner = new Mock<IContentSource>();

            inner.Setup(o=>o.GetAllContentItemsAsync("en-US",null))
                .ReturnsAsync(new List<ContentItem> { new ContentItem { Name = "A", Value = "ValA", Enabled = true  } });

            var memoryStore = new MemoryContentSource(inner.Object);

            //Act
            var item1 = memoryStore.GetContentItem("A", "en-US");
            var item2 = memoryStore.GetContentItem("A", "en-US");

            //Assert
            Assert.Equal("ValA", item1);
            Assert.Same(item1, item2);
            inner.Verify( o=> o.GetAllContentItemsAsync("en-US",null), Times.Once);

        }


        [Fact]
        public void GetResourceItem_With_Exception_Recovers()
        {
            //Arrange
            var inner = new Mock<IContentSource>();

            inner.SetupSequence(o=>o.GetAllContentItemsAsync("en-US",null))
                .Throws(new Exception("Test Error"))
                .ReturnsAsync(new List<ContentItem> { new ContentItem { Name = "A", Value = "ValA", Enabled = true  } });

            var memoryStore = new MemoryContentSource(inner.Object);

            //Act
            Assert.ThrowsAny<Exception>(() => memoryStore.GetContentItem("A", "en-US") );

            var item = memoryStore.GetContentItem("A", "en-US");

            //Assert
            inner.Verify( o=> o.GetAllContentItemsAsync("en-US",null), Times.Exactly(2));
        }

        [Fact]
        public void GetResourceItem_With_ManyThreads_OnlyCalls_Inner_Once()
        {
            //Arrange
            var inner = new Mock<IContentSource>();

            inner.SetupSequence(o=>o.GetAllContentItemsAsync("en-US",null))
                .ReturnsAsync(() => { 
                    Thread.Sleep(500);
                    return new List<ContentItem> { new ContentItem { Name = "A", Value = "ValA", Enabled = true  } }; 
                    });

            var memoryStore = new MemoryContentSource(inner.Object);

            //Act
            Parallel.For(0, 20, t=>
            {
                for (int i = 0; i < 10; i++)
                {
                    memoryStore.GetContentItem("A", "en-US");
                }
            });

            //Assert
            inner.Verify( o=> o.GetAllContentItemsAsync("en-US",null), Times.Once);
        }


        [Fact]
        public void GetResourceItem_With_ManyThreads_Errors_Twice_Then_Recovers()
        {
            //Arrange
            var inner = new Mock<IContentSource>();

            inner.SetupSequence(o=>o.GetAllContentItemsAsync("en-US",null))
                .Throws(new Exception("Test Error1"))
                .Throws(new Exception("Test Error2"))
                .ReturnsAsync(() => { 
                    Thread.Sleep(500);
                    return new List<ContentItem> { new ContentItem { Name = "A", Value = "ValA", Enabled = true  } }; 
                    });

            var memoryStore = new MemoryContentSource(inner.Object);

            //Act

            Parallel.For(0, 20, t=>
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    { 
                        memoryStore.GetContentItem("A", "en-US");
                    } 
                    catch(Exception)
                    {

                    }
                }
            });

            //Assert
            inner.Verify( o=> o.GetAllContentItemsAsync("en-US",null), Times.Exactly(3));
        }


      






    }
}
