using Content.Localization;


using Resources;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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


            psttest.Content = new LocalizerConfiguration()
                .AddMemorySource()
                .AddJsonFileSource(o=> 
                { 
                    o.Location      = Server.MapPath("~/App_Data/Json");
                })
                .AddApiSource(o=> 
                {
                    o.ApiUri            = new Uri("https://exigodemov6-api.exigo.com/3.0/");
                    o.LoginName         = "*";
                    o.Password          = "*";
                    o.Company           = "*";
                    o.SubscriptionKey   = "*";
                    o.EnvironmentCode   = "prod";
                })
                .AddUpdater(TimeSpan.FromSeconds(5))
                .BuildLocalizer();
         
            
            Localizer.Content  = new LocalizerConfiguration()
                .AddMemorySource()
                .AddProtoFileSource(o=> 
                { 
                    o.Location      = Server.MapPath("~/App_Data/Proto");
                })
                .AddApiSource(o=> 
                {
                    o.ApiUri            = new Uri("https://exigodemov6-api.exigo.com/3.0/");
                    o.LoginName         = "*";
                    o.Password          = "*";
                    o.Company           = "*";
                    o.SubscriptionKey   = "*";
                    o.EnvironmentCode   = "prod";
                })
                .AddUpdater(TimeSpan.FromSeconds(5))
                .BuildLocalizer();

        }
    }





}
