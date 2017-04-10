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
    
    /// <summary>
/// extracts text from pdfs and saves them to word documents
/// </summary>
    public class TextController : Controller
    {
        //controller for extracting content from pdf file and save to word file
        // GET: Text
        public ActionResult Index()
        {
            return View();
        }



        public ActionResult procesare(string numefisier)
        {
            string path = Path.Combine(Server.MapPath("~/PDFs/"), numefisier);
            objdescrieri container = tabledetect.pdftoword(path);
            string filename = "output" + DateTime.Now.ToString("yyyy-MM-dd--hh-mm-ss") + ".docx";
CreareWord.CreateWordprocessingDocument(Server.MapPath("~/PDFs/") + filename, container); textprocesare model = new textprocesare(filename);
            return View(model);
        }

        public ActionResult download_word(string numefisier)
        {
            string path = Server.MapPath("~/PDFs/") + numefisier;
            string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            return File(path, contentType, numefisier);
        }


    }
}