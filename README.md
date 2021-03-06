# Content Localiztion

Content Localization is a framework for the delivery and real-time updating of localized content for applications targeting Asp.Net, Asp.Net Core, and Xamarin.

It works by chaining multiple sources for the content together. For example, on a web site, you may choose the primary source to use the in-place memory cache, which if empty will load up a locally stored protobuf file, which if not present may request an for the data the first time. A backgorund thread will then ensure any new changes from the api are saved to the file, which will update the memory cache.

You may opt for different set of sources for a mobile application. This would favor smaller, granular calls which preserves bandwidth, and saves memory. A sqllite source may be the choice here.

Special care needs to be considered for concurrency. Larger web sites will have many separate server processes hitting the same local files, and site boot-up routines need to consider a flood of new requests all trying to hydrate the cache at the same time.

## Sources

* **[MemoryContentSource](src/Content.Localization/MemoryContentSource.cs)**: Optimized for speed, concurrency, and thread-safety. Handles blocking only when a cache needs hydrating for the first time, with no blocking penalty after. 
* **[ProtoFileContentSource](src/Content.Localization.ProtoFile/ProtoFileContentSource.cs)**: Uses [protobuf-net](https://github.com/protobuf-net/protobuf-net) to serialize resource set to and from local files. This is typically the middle source in between Memory and final source, such as Api or Sql. The benefits are speed, and no run-time dependency on a network resource for your content. These files can be deployed with your application with the background updater replacing them as necessary.
* **[JsonFileContentSource](src/Content.Localization.JsonFile/JsonFileContentSource.cs)**: Uses [Json.Net](https://www.newtonsoft.com/json) to serialize resource set to and from local files. This can be used as an alternative to protobuf, and serves the same purpose. Benefits include files being human readable. In tests, it is a little be slower than protobuf.
* **[ApiFileContentSource](src/Content.Localization.Api/ApiContentSource.cs)**: Accesses the [Exigo Api](http://api.exigo.com/3.0/ExigoApi.asmx?op=GetResourceSetItems) as the source of content. The background updater thread periodically pings for new changes, and will feed these into the upline source, usually a file cache.


## Runtime Environments

* **AspNetFramework**: Contains the plumbing necassary to use the content in the legacy Asp.Net Mvc system. 
* **AspNetCore**: Creates an IStringLocalizer provider to hook into the core Asp.Net Core localization system.
* **Xamarin**: Configures sources which are optimized for low-bandwidth, low-memory scenarios. 
* **Blazor**: Future feature. Helpers optimized for WebAssembly Blazor apps. 

## Getting Started

##### Source Code Method
Clone the repo and copy the projects you want to use into your project, and link to them directly. 

#### NuGet Method

###### Asp.Net Framework

```
Install-Package Content.Localization.AspNetFramework
```

Then in your Global.cs file insert the initialization script:

```csharp

Localizer.Content  = new LocalizerConfiguration()
    .AddMemorySource()
    .AddProtoFileSource(o=> 
    { 
        o.Location          = Server.MapPath("~/App_Data/Content/Common");
    })
    .AddApiSource(o=> 
    {
        o.ApiUri            = new Uri("https://yourcompany-api.exigo.com/3.0/");
        o.LoginName         = "yourloginname";
        o.Password          = "yourpassword";
        o.Company           = "yourcompany";
        o.SubscriptionKey   = "yoursubscription";
        o.EnvironmentCode   = "prod";
    })
    .AddUpdater(o=> 
    {
        o.Frequency         = TimeSpan.FromMinutes(10);
    })
    .AddSerilog()
    .BuildLocalizer();
});

```


#### Strongly Typed Class

You can have the site build out a strongly typed class by adding the AddClassGenerator call to it:

```csharp

 Localizer.Content  = new LocalizerConfiguration()
    //Add this after the other options
    .AddClassGenerator(o=>
    {
        o.ClassName         = "Common";
        o.Location          = Server.MapPath("~/Localization");
    })
    .BuildLocalizer();

    //Once the class generates, you assign the context to it:
    Resources.Common.Content = Localizer.Content;
   
    //Now all the resources are available as properties 
    Resorces.Common.YourProperty;
```



