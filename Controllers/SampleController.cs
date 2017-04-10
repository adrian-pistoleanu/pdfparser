using System.Collections.Generic;
using System.Web.Mvc;
using Dropdowns.Models;

namespace Dropdowns.Controllers
{
    public class SampleController : Controller
    {
       
        public ActionResult sample()
        {
         
            var states = GetAllStates();

            var model = new ModelLista();

          
            model.optiuni = GetSelectListItems(states);

            return View(model);
        }

   
        [HttpPost]
        public ActionResult sample(ModelLista model)
        {
          
            var states = GetAllStates();

      
            model.optiuni = GetSelectListItems(states);

        
            if (ModelState.IsValid)
            {
                Session["ModelLista"] = model;
                return RedirectToAction("Done");
            }

            return PartialView("sample", model);
        }

      
       
        public ActionResult Done()
        {
           
            var model = Session["ModelLista"] as ModelLista;

        
            return PartialView(model);
        }

    
        private IEnumerable<string> GetAllStates()
        {
            return new List<string>
            {
                "tabel3.pdf",
                "tabel4.pdf",
            };
        }


        private IEnumerable<SelectListItem> GetSelectListItems(IEnumerable<string> elements)
        {
       
            var selectList = new List<SelectListItem>();

       
            if (Session["fisier"] != null) selectList.Add(new SelectListItem
            {
                Value = Session["fisier"].ToString(),
                Text = Session["fisier"].ToString()
            }); 
            foreach (var element in elements)
            {
                selectList.Add(new SelectListItem
                {
                    Value = element,
                    Text = element
                });
            }
            
                return selectList;
        }
    }
}
