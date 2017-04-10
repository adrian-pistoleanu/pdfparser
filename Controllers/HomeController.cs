using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using System.Web.Mvc;
using iTextSharp.tutorial.Chapter1;
using iTextSharp.tutorial.detectie_tabel;
namespace MVCParser.Controllers
{
    public class HomeController : Controller
    {
        public static string filename = String.Empty;
        public ActionResult Index()
        {
            Session["fisier"] = "2coloane.pdf";
            if (!Request.IsAuthenticated)
                return RedirectToAction("Login","Account");
            else
            
            return View();
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file)
        {
           
            Boolean fileOK = false;
            if (file!=null && file.ContentLength>0)
            {
                String fileExtension =
                    System.IO.Path.GetExtension(file.FileName).ToLower();
                String[] allowedExtensions =
                    {".pdf"};
                for (int i = 0; i < allowedExtensions.Length; i++)
                {
                    if (fileExtension == allowedExtensions[i])
                    {
                        fileOK = true;
                    }
                }
            }

            if (file != null && file.ContentLength > 0 && fileOK)
                try
                {
          string path = Path.Combine(Server.MapPath("~/PDFs"), Path.GetFileName(file.FileName));
                    filename = file.FileName;
                    Session["fisier"] = filename;
                    file.SaveAs(path);
                    ViewBag.Message = "File uploaded successfully";
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "ERROR:" + ex.Message.ToString();
                }
            else
            {
                ViewBag.Message = "You have not specified a file.";
            }
            return View();
        }

      
        public ActionResult ButtonClick3()
        {
            ViewBag.Text1 = "clicked2";
        
            return View();
        }

    
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ButtonClick(string button1)
        {
    
            ViewBag.Text1 = "clicked";
            if (filename != String.Empty)
            {
                ParserText ex1 = new ParserText();
                String path = Server.MapPath("~/PDFs/");
                string output = ex1.ReadPdfFile(path + filename);
                ViewBag.Text1 = output;
            }
           return PartialView() ;
         
        }

    


        [Authorize]
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}