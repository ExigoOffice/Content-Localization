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
            
           CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
           Console.WriteLine(CultureInfo.CurrentUICulture.Name + "e");

            return View();
        }
    }
}