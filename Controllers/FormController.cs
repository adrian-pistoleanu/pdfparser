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
    public class FormController : Controller
    {
        // GET: Form
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult sample()
        {
            Session["fisier"] = "form111fill.pdf";
            return View();
        }

        public ActionResult procesare(string numefisier)
        {
            string path = Path.Combine(Server.MapPath("~/PDFs/"), numefisier);
            List<List<string>> afisareformulare = tabledetect.formfinal(path);//extragere formular catre word
            CreareWord ws = new CreareWord();
            string filename = "output" + DateTime.Now.ToString("yyyy-MM-dd--hh-mm-ss") + ".docx";
            CreareWord.form2word(Server.MapPath("~/PDFs/") + filename, afisareformulare);
            textprocesare model = new textprocesare(filename);
            return View(model);
        }

        //pentru download se apeleaza functia din TextController 
    }
}