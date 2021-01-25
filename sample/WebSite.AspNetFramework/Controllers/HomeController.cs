using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace WebSite.AspNetFramework.Controllers
{
    public class HomeController : Controller
    {
        static CultureInfo CultureInfo = CultureInfo.CurrentUICulture;

        private static Task T = Task.Run(() =>
        {
            //var cultures = new[] { "es", "en", "fr", "fr-CA", "ru" };
            //var i = 0;
            //while (true)
            //{

            //    CultureInfo = CultureInfo.GetCultureInfo(cultures[i++ % cultures.Length]);
            //    Task.Delay(1000).GetAwaiter().GetResult();
            //}

            CultureInfo = CultureInfo.GetCultureInfo("fr-CA");

        });
        // GET: Default
        public ActionResult Index()
        {
            
           CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-CA");
           Console.WriteLine(CultureInfo.CurrentUICulture.Name + "e");

            return View();
        }
    }
}