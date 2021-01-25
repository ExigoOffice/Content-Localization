using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebSite.AspNetFramework.Models;

namespace WebSite.AspNetFramework.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var someModel = new SomeModel
            {
                Languages = new List<Language>
                {
                    new Language {Value = "fr-CA", Text = "French Canada"},
                    new Language {Value = "fr", Text = "French"},
                    new Language {Value = "ru", Text = "Russian"},
                    new Language {Value = "en-US", Text = "English"}
                }
            };

            Thread.CurrentThread.CurrentUICulture = (Request["l"]==null) ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo(Request["l"]);

            Console.WriteLine(Thread.CurrentThread.CurrentUICulture + "e");

            return View( someModel);
        }

     
    }
}