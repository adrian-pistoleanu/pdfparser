using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Mvc;
using MVCParser.Models;
using iTextSharp.tutorial.detectie_tabel;
using iTextSharp.tutorial.Chapter1;
namespace MVCParser.Controllers
{
    public class GridController : Controller
    {
        // GET: Grid
        public ActionResult Index(int? pageNumber)
        {
            ProductModel model = new ProductModel();
            model.PageNumber = (pageNumber == null ? 1 : Convert.ToInt32(pageNumber));
            model.PageSize = 4;

            List<Product> products = Product.GetSampleProducts();

            if (products != null)
            {
                model.Products = products.OrderBy(x => x.Id)
                         .Skip(model.PageSize * (model.PageNumber - 1))
                         .Take(model.PageSize).ToList();

                model.TotalCount = products.Count();
                var page = (model.TotalCount / model.PageSize) - (model.TotalCount % model.PageSize == 0 ? 1 : 0);
                model.PagerCount = page + 1;
            }

            return View(model);
        }

        /// <summary>
        /// Grid fara paginare = se afiseaza toate liniile --- la fel va arata si-n Excel
        /// </summary>
        /// <returns></returns>
        public ActionResult grid(string numefisier)
        {
            string path = Path.Combine(Server.MapPath("~/PDFs/"),numefisier);
 
            objlist model = tabledetect.tabel_cu_linii(path);
            ViewBag.tabele = model;
            //creating excel files
            objstring stringlist = new objstring(create_excel());
            string root = Server.MapPath("~/PDFs/");
            List<string> denumiri2 = new List<string>();
              foreach (string s in stringlist.st)
                 denumiri2.Add(root + s);
            zipfiles.CreateDocumentationZipFile(0,  Server.MapPath("~/PDFs/"), denumiri2);
            ViewBag.den = stringlist; 
            return View(model);
        }

        /// <summary>
        /// only for testing
        /// </summary>
        /// <returns></returns>
        public ActionResult grids()
        {
            return RedirectToAction("grid",new { numefisier = "C3.pdf" });
        }
       

        public List<string> create_excel()
        {
            string path = Server.MapPath("~/PDFs/");
            List<string> denumiri = new List<string>();
            objlist model2 = ViewBag.tabele;
            if (model2 != null)
            {
                for (int i = 0; i < model2.toate.Count; i++)
                {
                    string s1 = CreareExcel.excelfile(path, model2.toate[i], model2.cuvinte, i);
                    if (denumiri.Contains(s1)==false)
                    denumiri.Add(s1);
                }

            }
      
            return denumiri;
        }


        public ActionResult download_excel(int nr, string numefisier)
        {
        
            string path = String.Empty;

                path = Server.MapPath("~/PDFs/") + numefisier;
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(path, contentType, numefisier);
 
        }

        public ActionResult download_zip()
        {

            string path = Server.MapPath("~/PDFs/");
            string contentType = "application/zip";
            return File(path+ "0excel.zip", contentType, Path.GetFileName("0excel.zip"));
            // return PartialView();
        }


        public ActionResult test()
        {
            string path = Path.Combine(Server.MapPath("~/PDFs/"), "C3.pdf");
            ViewBag.test = path;
            return View();
        }

        /// <summary>
        /// se afiseaza toate liniile
        /// </summary>
        /// <returns></returns>
        public ActionResult tabel_linii()
        {
            ProductModel model = new ProductModel();


            List<Product> products = Product.GetSampleProducts();
            model.Products = products;
            return View(model);
        }


        [HttpGet]
        public ActionResult WebGrid()
        {
            ProductModel model = new ProductModel();
            model.PageSize = 4;

            List<Product> products = Product.GetSampleProducts();

            if (products != null)
            {
                model.TotalCount = products.Count();
                model.Products = products;
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult jqGrid()
        {
            return View();
        }

        [HttpGet]
        public ActionResult AngularJS()
        {
            return View();
        }

        public ActionResult GetProducts(string sidx, string sord, int page, int rows)
        {
            var products = Product.GetSampleProducts();
            int pageIndex = Convert.ToInt32(page) - 1;
            int pageSize = rows;
            int totalRecords = products.Count();
            int totalPages = (int)Math.Ceiling((float)totalRecords / (float)pageSize);

            var data = products.OrderBy(x => x.Id)
                         .Skip(pageSize * (page - 1))
                         .Take(pageSize).ToList();

            var jsonData = new
            {
                total = totalPages,
                page = page,
                records = totalRecords,
                rows = data
            };

            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetProductById(int id)
        {
            var products = Product.GetSampleProducts().Where(x => x.Id == id); ;

            if (products != null)
            {
                ProductModel model = new ProductModel();

                foreach (var item in products)
                {
                    model.Name = item.Name;
                    model.Price = item.Price;
                    model.Department = item.Department;
                }

                return PartialView("_GridEditPartial", model);
            }

            return View();
        }

        [HttpPost]
        public ActionResult UpdateProduct(ProductModel model)
        {
            //update database
            return Content("Record updated!!", "text/html");
        }



    }
}