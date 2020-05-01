using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Content.Localization.Tests
{
    public sealed class FileContentStoreTests : IDisposable
    {
        private readonly string  _location;
        private readonly ITestOutputHelper _output;

        public FileContentStoreTests(ITestOutputHelper output)
        {
            _location = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_location);
            _output = output;
            _output.WriteLine(_location);
        }

        public void Dispose()
        {
            Directory.Delete(_location, true);
        }

        [Fact]
        public async Task Proto_NewItemsSave_Then_ReadBack()
        {
            //Arrange
            var list = new List<ContentItem> { 
                new ContentItem { Name = "A", Value = "ValA", Enabled = true  },
                new ContentItem { 
                    Name                = "B", 
                    Value               = "ValB", 
                    Enabled             = true, 
                    EnabledStartDate    = new DateTime(2020, 1, 1), 
                    EnabledEndDate      = new DateTime(2020, 1, 31)
                }
            };

            var store = new ProtoFileContentSource(_location, new MockContentSource());

            //Act
            await store.SaveAllContentItemsAsync("en-US", list);

            var check = (await store.GetAllContentItemsAsync("en-Us")).ToArray();

            //Assert
            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(list[i].Name, check[i].Name);
                Assert.Equal(list[i].Value, check[i].Value);
                Assert.Equal(list[i].Enabled, check[i].Enabled);
                Assert.Equal(list[i].EnabledStartDate, check[i].EnabledStartDate);
                Assert.Equal(list[i].EnabledEndDate, check[i].EnabledEndDate);
            }
        }

        [Fact]
        public async Task Json_NewItemsSave_Then_ReadBack()
        {
            //Arrange
            var list = new List<ContentItem> { 
                new ContentItem { Name = "A", Value = "ValA", Enabled = true  },
                new ContentItem { 
                    Name                = "B", 
                    Value               = "ValB", 
                    Enabled             = true, 
                    EnabledStartDate    = new DateTime(2020, 1, 1), 
                    EnabledEndDate      = new DateTime(2020, 1, 31)
                }
            };

            var store = new JsonFileContentSource(_location, null);

            //Act
            await store.SaveAllContentItemsAsync("en-US", list);

            var check = (await store.GetAllContentItemsAsync("en-Us")).ToArray();

            //Assert
            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(list[i].Name, check[i].Name);
                Assert.Equal(list[i].Value, check[i].Value);
                Assert.Equal(list[i].Enabled, check[i].Enabled);
                Assert.Equal(list[i].EnabledStartDate, check[i].EnabledStartDate);
                Assert.Equal(list[i].EnabledEndDate, check[i].EnabledEndDate);
            }
        }

        [Theory()]
        [InlineData( 1000, 1000)]
        [InlineData( 1000, 10000)]
        [InlineData( 1000, 100000)]
        [InlineData( 10000, 1000)]
        [InlineData( 100000, 1000)]
        public async Task Proto_vs_Json(int resourceSize, int recordCount)
        {
            //Arrange
            string cultureCode = "en-US";

            var sb  = new StringBuilder(1000);
            for (int i = 0; i < resourceSize; i++) sb.Append("X");
            var list = new List<ContentItem>();
            for (int i = 0; i < recordCount; i++)
            {
                list.Add(new ContentItem {  
                    Name    = "Item" + i, 
                    Value   = sb.ToString(), 
                    Enabled = true,
                    EnabledStartDate    = new DateTime(2020, 1, 1), 
                    EnabledEndDate      = new DateTime(2020, 1, 31)                    
                    });
            }


            var jsonStore = new JsonFileContentSource(_location, null);
            var protoStore = new ProtoFileContentSource(_location, new MockContentSource());


            for (int i = 0; i < 3; i++)
            {
                var swJson = Stopwatch.StartNew();
                await jsonStore.SaveAllContentItemsAsync(cultureCode, list);
                swJson.Stop();
                var jsonFile = new FileInfo(jsonStore.GetCultureFileName(cultureCode));
                _output.WriteLine($"Json Save at {swJson.ElapsedMilliseconds:#,#}. ValueSize: {resourceSize:#,#}, Count: {recordCount:#,#},  FileSize: {jsonFile.Length:#,#}");


                var swProto = Stopwatch.StartNew();
                await protoStore.SaveAllContentItemsAsync(cultureCode, list);
                swProto.Stop();
                var protoFile = new FileInfo(protoStore.GetCultureFileName(cultureCode));
                _output.WriteLine($"Proto Save at {swProto.ElapsedMilliseconds:#,#}. ValueSize: {resourceSize:#,#}, Count: {recordCount:#,#},  FileSize: {protoFile.Length:#,#}");
            }

            for (int i = 0; i < 3; i++)
            {
                var swJson = Stopwatch.StartNew();
                _ = await jsonStore.GetAllContentItemsAsync(cultureCode);
                swJson.Stop();
                var jsonFile = new FileInfo(jsonStore.GetCultureFileName(cultureCode));
                _output.WriteLine($"Json Load at {swJson.ElapsedMilliseconds:#,#}. ValueSize: {resourceSize:#,#}, Count: {recordCount:#,#}, FileSize: {jsonFile.Length:#,#}");


                var swProto = Stopwatch.StartNew();
                _ = await protoStore.GetAllContentItemsAsync(cultureCode);
                swProto.Stop();
                var protoFile = new FileInfo(protoStore.GetCultureFileName(cultureCode));
                _output.WriteLine($"Proto Load at {swProto.ElapsedMilliseconds:#,#}. ValueSize: {resourceSize:#,#}, Count: {recordCount:#,#}, FileSize: {protoFile.Length::#,#}");
            }


            

        }

    }
}
