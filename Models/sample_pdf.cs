using System.Collections.Generic;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Dropdowns.Models
{
 
    public class ModelLista
    {

  
        [Required]
        [Display(Name = "State")]
        public string State { get; set; }

        public IEnumerable<SelectListItem> optiuni { get; set; }
    }
}