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
        public ActionResult Index()
        {  
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("ru");
            Console.WriteLine(Thread.CurrentThread.CurrentUICulture + "e");

            return View();
        }
    }
}