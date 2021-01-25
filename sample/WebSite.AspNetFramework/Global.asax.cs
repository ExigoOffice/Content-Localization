using Content.Localization;


using Serilog;
using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebSite.AspNetFramework
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            
            Resources.CDEN.Content = new LocalizerConfiguration()
                .AddMemorySource()
                .AddProtoFileSource(o=> 
                { 
                    o.Location          = Server.MapPath("~/App_Data");
                })
                .AddApiSource(o=> 
                {
                    o.ApiUri            = new Uri("http://exigodemov6-api.exigo.com/3.0/");
                    o.LoginName         = "Administrator";
                    o.Password          = "whodaman";
                    o.Company           = "exigodemov6";
                    o.SubscriptionKey   = "CDEN";
                    o.EnvironmentCode   = "dev";
                })
                .AddUpdater(o=> 
                {
                    o.Frequency         = TimeSpan.FromSeconds(1);
                })
                .AddClassGenerator(o=>
                {
                    o.ClassName         = "CDEN";
                    o.Location          = Server.MapPath("~/App_Data");
                })
                .AddSerilog()
                .BuildLocalizer();
        }
    }
}
