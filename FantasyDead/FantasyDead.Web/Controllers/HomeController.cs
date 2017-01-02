using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FantasyDead.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Legal()
        {          
            return View();
        }

        public ActionResult Privacy()
        {
            return View();
        }
    }
}