using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using iTextSharp.tutorial.detectie_tabel;
using iTextSharp.tutorial.Chapter1;
using MVCParser.Models;
namespace MVCParser.Controllers
{
    public class NolinesController : Controller
    {
        // GET: Nolines
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        ///Acesta este fisierul sample 
        /// </summary>
        /// <returns></returns>
        public ActionResult sample()
        {
            Session["fisier"] = "tbfaralinii.pdf";
            return View();
        }


        public ActionResult procesare(string numefisier)
        {
            string path = Path.Combine(Server.MapPath("~/PDFs/"), numefisier);
            objtbnolines obj = tabledetect.tbnolines(path);


            
            return View(obj);
        }
    }
}