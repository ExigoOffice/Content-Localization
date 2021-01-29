using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Content.Localization.Tests
{
    //This allows a single set of tests to be run across multiple combination of 
    //sources tied together
    public sealed class ContentSourceStackTests : IDisposable
    {
        private const string CircularReferenceDetectionMessage = "Circular Reference Detected";
        private const string SelfReferenceMessage = "Self Reference Detected";
        public static IEnumerable<object[]> ContentSources
        {
            get
            {


                var list =new List<object[]>
                {
                    //--> Memory over mock
                    new object[] {"MemoryOverMock", new Func<IContentSource>(() => 
                        new MemoryContentSource {
                            NextSource =  DefaultMock()
                        }
                    )},

                    //--> Proto over mock
                    new object[] {"ProtoOverMock", new Func<IContentSource>(() =>
                        new ProtoFileContentSource(_location, new NullContentLogger()) { 
                            NextSource = DefaultMock()
                        }
                           
                    )},


                    //--> Json over mock
                    new object[] {"JsonOverMock", new Func<IContentSource>(() =>
                        new JsonFileContentSource(_location) {
                            NextSource = DefaultMock() 
                        }
                    )},

                    //--> Memory over Proto over Mock
                    new object[] {"MemoryOverProtoOverMock", new Func<IContentSource>(() =>
                        new MemoryContentSource {
                            NextSource = new ProtoFileContentSource(_location, new NullContentLogger()) {
                                NextSource =  DefaultMock()
                            }
                        }
                    )},

                    //--> Memory over Json over Mock
                    new object[] {"MemoryOverJsonOverMock", new Func<IContentSource>(() =>
                        new MemoryContentSource {
                            NextSource = new JsonFileContentSource(_location) {
                                NextSource =   DefaultMock()
                            }
                        }
                    )},

                    //--> For fun, Proto Over Json Over Mock (You would never do this in real life)
                    new object[] {"ProtoOverJsonOverMock", new Func<IContentSource>(() =>
                        new ProtoFileContentSource(_location, new NullContentLogger()) {
                            NextSource = new JsonFileContentSource(_location) {
                                NextSource = DefaultMock()
                            }
                        }
                    )}


                };

                return list;
            }
        }


        [Theory]
        [MemberData(nameof(ContentSources))]
        public void Disabled_ShouldWriteBlankForResource(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");
            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}"
            });
            var item = mock.GetAllContentItemsAsync("en-US")
                .ContinueWith(task => task.Result.First(i => i.Name == "SomeKey"))
                .ConfigureAwait(false).GetAwaiter().GetResult();
            item.Enabled = false;
            //Act/Assert
            Assert.Equal("", localizer["SomeKey"] );
            Assert.Equal(localizer["SomeKey"], localizer["ReferenceSomeKey"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public async Task CheckForChangesAsync_CalledTwice_Calls_Data_Once(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source = factory();
            var mock   = GetMock(source);

            //Act
            _ = await source.CheckForChangesAsync();
            var version = await source.CheckForChangesAsync();

            //Assert
            Assert.Equal(mock.ContentVersion.Version, version.Version);
            Assert.Equal(1, mock.GetAllContentItemsInvokeCount); //we should not have invoked at all
        }

        [Theory]
        [MemberData(nameof(ContentSources))]
        public void GetContentItem_ColdBoot_LoadsFrom_CoreSource(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source = factory();
            var mock   = GetMock(source);

            //Act
            _ = source.GetContentItem("A", "en-US");
            var item = source.GetContentItem("A", "en-US");

            //Assert
            Assert.Equal("ValA", item);
            Assert.Equal(1, mock.GetAllContentItemsInvokeCount); 
        }


        [Theory]
        [MemberData(nameof(ContentSources))]
        public async Task VersionChange_PropigatesUp(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source = factory();
            var mock   = GetMock(source);

            //Act/Assert
            for (int i = 0; i < 10; i++)
            { 
                Assert.Equal("ValA", source.GetContentItem("A", "en-US"));
                Assert.Equal(1, mock.GetAllContentItemsInvokeCount); 
            }

            //--> Version Change
            mock.SetVersion("2.0", new DateTime(2020, 1, 1));
            mock.SetData("en-US", new Dictionary<string, string> { { "A", "ValB"} });

            await source.CheckForChangesAsync();

            for (int i = 0; i < 10; i++)
            { 
                Assert.Equal("ValB", source.GetContentItem("A", "en-US"));            
                Assert.Equal(2, mock.GetAllContentItemsInvokeCount); 
            }

            //--> Modified date change
            mock.SetVersion("2.0", new DateTime(2020, 1, 2));
            mock.SetData("en-US", new Dictionary<string, string> { { "A", "ValC"} });

            await source.CheckForChangesAsync();

            for (int i = 0; i < 10; i++)
            { 
                Assert.Equal("ValC", source.GetContentItem("A", "en-US"));            
                Assert.Equal(3, mock.GetAllContentItemsInvokeCount); 
            }
        }

        [Theory]
        [MemberData(nameof(ContentSources))]
        public void GetItem_With_Updates_OverMany_Threads(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source = factory();
            var mock   = GetMock(source);
            
            //Act/Assert
            
            
            //This will blow up if there are file locking issues when those sources aren't protected
            //behind the memory one. This demonstrates that multiple processes will not kill each other
            Parallel.For(0, 20, t=>
            {
                for (int i = 0; i < 10; i++)
                {
                    var item = source.GetContentItem("A", "en-US");
                    Assert.Equal("ValA", item);
                }
            });

            
            //Data gets updated
            mock.SetVersion("2.0", new DateTime(2020, 1, 1));
            mock.SetData("en-US", new Dictionary<string, string> { { "A", "ValB"} });


            //Unlikely concurrent updates (simulates multiple worker processes etc)
            Parallel.For(0,20, t=>
            {
                var newVersion = source.CheckForChangesAsync()
                    .GetAwaiter()
                    .GetResult();

                Assert.Equal("2.0", newVersion.Version);
            });

            Parallel.For(0,20, t=>
            {
                for (int i = 0; i < 10; i++)
                {
                    var item = source.GetContentItem("A", "en-US");
                    Assert.Equal("ValB", item);
                }
            });
        }


        [Theory]
        [MemberData(nameof(ContentSources))]
        public void Localizer_Returns_Localized_Content(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string> { { "SomeKey", "Val-en-US" } });
            mock.SetData("es-MX", new Dictionary<string, string> { { "SomeKey", "Val-es-MX" } });

            //Act/Assert
            Assert.True(string.IsNullOrEmpty(localizer["Some unknown key"]));

            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            Assert.Equal("Val-en-US", localizer["SomeKey"] );

            CultureInfo.CurrentUICulture = new CultureInfo("es-MX");
            Assert.Equal("Val-es-MX", localizer["SomeKey"] );


            CultureInfo.CurrentUICulture = new CultureInfo("es-FR"); //we don't have this so it should resort to default
            Assert.Equal("Val-en-US", localizer["SomeKey"] );
        }

        [Theory]
        [MemberData(nameof(ContentSources))]
        public void Locizer_Falls_Back_To_Two_Letter_Culture(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string> { { "SomeKey", "Val-en"} });
            
            mock.SetData("es", new Dictionary<string, string> { { "SomeKey", "Val-es"} });

            //Act/Assert
            CultureInfo.CurrentUICulture = new CultureInfo("es-MX");
            Assert.Equal("Val-es", localizer["SomeKey"] );
        }

        [Theory]
        [MemberData(nameof(ContentSources))]
        public void NestedResource_ShouldEvaluateNestedResource(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}"
            });

            //Act/Assert
            Assert.Equal("SomeValue", localizer["SomeKey"] );
            Assert.Equal(localizer["SomeKey"], localizer["ReferenceSomeKey"] );
        }

        [Theory]
        [MemberData(nameof(ContentSources))]
        public void MultipleNestedResources_ShouldEvaluateNestedResources(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}-{{SomeKey}}"
            });

            //Act/Assert
            Assert.Equal("SomeValue", localizer["SomeKey"] );
            Assert.Equal($"{localizer["SomeKey"]}-{localizer["SomeKey"]}", localizer["ReferenceSomeKey"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void MultipleLayersOfNestedResources_ShouldEvaluateNestedResources(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}-{{SomeKey}}",
                ["Reference_ReferenceSomeKey"] = "{{ReferenceSomeKey}}-{{ReferenceSomeKey}}",
            });

            //Act/Assert
            Assert.Equal("SomeValue", localizer["SomeKey"] );
            Assert.Equal($"{localizer["SomeKey"]}-{localizer["SomeKey"]}", localizer["ReferenceSomeKey"] );
            Assert.Equal($"{localizer["ReferenceSomeKey"]}-{localizer["ReferenceSomeKey"]}", localizer["Reference_ReferenceSomeKey"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void MultipleLayersOfNestedResources2_ShouldEvaluateNestedResources(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}-{{SomeKey}}",
                ["Reference_ReferenceSomeKey"] = "{{SomeKey}}-{{ReferenceSomeKey}}",
                ["Reference_Reference_ReferenceSomeKey"] = "{{ReferenceSomeKey}}-{{Reference_ReferenceSomeKey}}",
            });

            //Act/Assert
            Assert.Equal("SomeValue", localizer["SomeKey"] );
            Assert.Equal($"{localizer["SomeKey"]}-{localizer["SomeKey"]}", localizer["ReferenceSomeKey"] );
            Assert.Equal($"{localizer["SomeKey"]}-{localizer["ReferenceSomeKey"]}", localizer["Reference_ReferenceSomeKey"] );
            Assert.Equal($"{localizer["ReferenceSomeKey"]}-{localizer["Reference_ReferenceSomeKey"]}", localizer["Reference_Reference_ReferenceSomeKey"] );
        }

        [Theory]
        [MemberData(nameof(ContentSources))]
        public void ResourceNotFoundForSpecifiedCulture_ExistsInDefaultCulture_ShouldGetValueFromDefaultCulture(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}"
            });

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("af");
            
            //Act/Assert
            Assert.Equal("SomeValue", localizer["SomeKey"] );
            Assert.Equal(localizer["SomeKey"], localizer["ReferenceSomeKey"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void ResourceNotFoundForSpecifiedCulture_ExistsInDefaultCulture_MultipleNestedResources_ShouldGetValuesFromDefaultCulture(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}-{{SomeKey}}"
            });

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("af");

            //Act/Assert
            Assert.Equal("SomeValue", localizer["SomeKey"] );
            Assert.Equal($"{localizer["SomeKey"]}-{localizer["SomeKey"]}", localizer["ReferenceSomeKey"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void ResourceNotFoundForSpecifiedCulture_ExistsInDefaultCulture_MultipleLayersOfNestedResources_ShouldGetValuesFromDefaultCulture(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}-{{SomeKey}}",
                ["Reference_ReferenceSomeKey"] = "{{ReferenceSomeKey}}-{{ReferenceSomeKey}}",
            });
            
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("af");

            //Act/Assert
            Assert.Equal("SomeValue", localizer["SomeKey"] );
            Assert.Equal($"{localizer["SomeKey"]}-{localizer["SomeKey"]}", localizer["ReferenceSomeKey"] );
            Assert.Equal($"{localizer["ReferenceSomeKey"]}-{localizer["ReferenceSomeKey"]}", localizer["Reference_ReferenceSomeKey"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void SomeResourceFoundForSpecifiedCulture_ShouldGetValueFromSpecifiedCultureIfExistsOtherwiseDefaultCulture(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}"
            });
            
            mock.SetData("af", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValueAf", 
            });

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("af");
            
            //Act/Assert
            Assert.Equal("SomeValueAf", localizer["SomeKey"] );
            Assert.Equal(localizer["SomeKey"], localizer["ReferenceSomeKey"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void SomeResourceFoundForSpecifiedCulture_MultipleResources_ShouldGetValueFromSpecifiedCultureIfExistsOtherwiseDefaultCulture(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}-{{SomeKey}}"
            });
            
            mock.SetData("af", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValueAf", 
            });

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("af");

            //Act/Assert
            Assert.Equal("SomeValueAf", localizer["SomeKey"] );
            Assert.Equal($"{localizer["SomeKey"]}-{localizer["SomeKey"]}", localizer["ReferenceSomeKey"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void SomeResourceFoundForSpecifiedCulture_MultipleLayersOfResources_ShouldGetValueFromSpecifiedCultureIfExistsOtherwiseDefaultCulture(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}-{{SomeKey}}",
                ["Reference_ReferenceSomeKey"] = "{{ReferenceSomeKey}}-{{ReferenceSomeKey}}",
            });
            
            mock.SetData("af", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValueAf", 
            });
            
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("af");

            //Act/Assert
            Assert.Equal("SomeValueAf", localizer["SomeKey"] );
            Assert.Equal($"{localizer["SomeKey"]}-{localizer["SomeKey"]}", localizer["ReferenceSomeKey"] );
            Assert.Equal($"{localizer["ReferenceSomeKey"]}-{localizer["ReferenceSomeKey"]}", localizer["Reference_ReferenceSomeKey"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void SomeResourceFoundForNeutralVariantOfSpecifiedCulture_ShouldGetValueFromNeutralVariantOfSpecifiedCultureIfExistsOtherwiseDefaultCulture(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}"
            });
            
            mock.SetData("es", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValueAf", 
            });

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-US");
            
            //Act/Assert
            Assert.Equal("SomeValueAf", localizer["SomeKey"] );
            Assert.Equal(localizer["SomeKey"], localizer["ReferenceSomeKey"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void SomeResourceFoundForNeutralVariantOfSpecifiedCulture_MultipleResources_ShouldGetValueFromNeutralVariantOfSpecifiedCultureIfExistsOtherwiseDefaultCulture(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}-{{SomeKey}}"
            });
            
            mock.SetData("es", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValueAf", 
            });

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-US");

            //Act/Assert
            Assert.Equal("SomeValueAf", localizer["SomeKey"] );
            Assert.Equal($"{localizer["SomeKey"]}-{localizer["SomeKey"]}", localizer["ReferenceSomeKey"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void SomeResourceFoundForNeutralVariantOfSpecifiedCulture_MultipleLayersOfResources_ShouldGetValueFromNeutralVariantOfSpecifiedCultureIfExistsOtherwiseDefaultCulture(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValue", 
                ["ReferenceSomeKey"] = "{{SomeKey}}-{{SomeKey}}",
                ["Reference_ReferenceSomeKey"] = "{{ReferenceSomeKey}}-{{ReferenceSomeKey}}",
            });
            
            mock.SetData("es", new Dictionary<string, string>
            {
                ["SomeKey"] = "SomeValueAf", 
            });

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-US");

            //Act/Assert
            Assert.Equal("SomeValueAf", localizer["SomeKey"] );
            Assert.Equal($"{localizer["SomeKey"]}-{localizer["SomeKey"]}", localizer["ReferenceSomeKey"] );
            Assert.Equal($"{localizer["ReferenceSomeKey"]}-{localizer["ReferenceSomeKey"]}", localizer["Reference_ReferenceSomeKey"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void ResourceNotFound_ShouldReturnEmptyValue(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["ReferenceSomeKeyThatDoesntExist"] = "{{SomeKeyThatDoesntExist}}"
            });

            //Act/Assert
            Assert.Equal(string.Empty, localizer["SomeKeyThatDoesntExist"] );
            Assert.Equal("", localizer["ReferenceSomeKeyThatDoesntExist"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void ResourceNotFound_MultipleResources_ShouldReturnEmptyValue(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["ReferenceSomeKeyThatDoesntExist"] = "{{SomeKeyThatDoesntExist}}-{{SomeKeyThatDoesntExist}}"
            });

            //Act/Assert
            Assert.Equal(string.Empty, localizer["SomeKeyThatDoesntExist"] );
            Assert.Equal("-", localizer["ReferenceSomeKeyThatDoesntExist"] );
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void ResourceNotFound_MultipleLayersOfResources_ShouldReturnEmptyValue(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            
            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["ReferenceSomeKeyThatDoesntExist"] = "{{SomeKeyThatDoesntExist}}-{{SomeKeyThatDoesntExist}}",
                ["Reference_ReferenceSomeKeyThatDoesntExist"] = "{{ReferenceSomeKeyThatDoesntExist}}-{{ReferenceSomeKeyThatDoesntExist}}"
            });

            //Act/Assert
            Assert.Equal(string.Empty, localizer["SomeKeyThatDoesntExist"]);
            Assert.Equal("-", localizer["ReferenceSomeKeyThatDoesntExist"]);
            Assert.Equal("---", localizer["Reference_ReferenceSomeKeyThatDoesntExist"]);
        }

        [Theory]
        [MemberData(nameof(ContentSources))]
        public void ResourceSelfReference_ShouldReturnSelfReferenceErrorMessage(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            
            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "{{SomeKey}}",
            });

            //Act/Assert
            Assert.Equal($"{SelfReferenceMessage} [SomeKey]", localizer["SomeKey"]);
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void ResourceCircularReference_ShouldReturnCircularReferenceErrorMessage(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            
            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "{{SomeKey2}}",
                ["SomeKey2"] = "{{SomeKey}}",
            });

            //Act/Assert
            Assert.Equal($"{CircularReferenceDetectionMessage} [SomeKey]", localizer["SomeKey"]);
            Assert.Equal($"{CircularReferenceDetectionMessage} [SomeKey2]", localizer["SomeKey2"]);
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void ResourceCircularReference_Transitive_ShouldReturnCircularReferenceErrorMessage(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            
            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "{{SomeKey2}}",
                ["SomeKey2"] = "{{SomeKey3}}",
                ["SomeKey3"] = "{{SomeKey}}"
            });

            //Act/Assert
            Assert.Equal($"{CircularReferenceDetectionMessage} [SomeKey]", localizer["SomeKey"]);
            Assert.Equal($"{CircularReferenceDetectionMessage} [SomeKey2]", localizer["SomeKey2"]);
            Assert.Equal($"{CircularReferenceDetectionMessage} [SomeKey3]", localizer["SomeKey3"]);
        }
        
        [Theory]
        [MemberData(nameof(ContentSources))]
        public void ResourceCircularReference_Transitive2_ShouldReturnCircularReferenceErrorMessage(string name, Func<IContentSource> factory)
        {
            //Arrange
            var source      = factory();
            var mock        = GetMock(source);
            var localizer   = new ContentLocalizer(source, "en-US");

            
            mock.SetData("en-US", new Dictionary<string, string>
            {
                ["SomeKey"] = "{{SomeKey2}}",
                ["SomeKey2"] = "{{SomeKey3}}",
                ["SomeKey3"] = "{{SomeKey2}}"
            });

            //Act/Assert
            Assert.Equal($"{CircularReferenceDetectionMessage} [SomeKey2]", localizer["SomeKey"]);
            Assert.Equal($"{CircularReferenceDetectionMessage} [SomeKey2]", localizer["SomeKey2"]);
            Assert.Equal($"{CircularReferenceDetectionMessage} [SomeKey3]", localizer["SomeKey3"]);
        }
        
        MockContentSource GetMock(IContentSource source)
        {
            while (source !=null )
            {
                if (source is MockContentSource mockContentSource)
                    return mockContentSource;

                source = source.NextSource;
            }

            return null;
        }

        static MockContentSource DefaultMock()
        {
            var source = new MockContentSource();
            source.SetVersion("1.0", new DateTime(2020, 1, 1));
            source.SetData("en-US", new Dictionary<string, string> { { "A", "ValA"} });

            return source;
        }

        //Setup/cleanup

        static string _location;
        public ContentSourceStackTests()
        {
            _location = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_location);
        }
        
        public void Dispose()
        {
            if (_location!=null)
            {
                Directory.Delete(_location, true);

                _location = null;
            }
        }
    }
}
