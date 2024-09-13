using MPWeb.Code;
using MPWeb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MPWeb.Controllers
{
    [AuthorizeByRole("User")]
    public class CustomControllerBase : Controller
    {
        protected ServerUserContext serverUserContext
        {
            get
            {
                var obj = Session["ServerUserContext"] as ServerUserContext;
                if (obj == null)
                {
                    obj = new ServerUserContext {
                        Id = HttpContext.GetOwinContext().Authentication.User.Claims.FirstOrDefault(c => c.Type == "nonce")?.Value,
                        Name = HttpContext.GetOwinContext().Authentication.User.Identity.Name
                    };
                    Session["ServerUserContext"] = obj;
                }
                return obj;
            }
        }

        public static ActionResult ErrorStatusCodeResult(string message)
            //A francia szövegekl megjelenítéséhez kell az UTF-8 encoding
   //         => new HttpStatusCodeResult(506, Encoding.UTF8.GetString(Encoding.Default.GetBytes(message)));
            => new HttpStatusCodeResult(506, message );

        protected ActionResult nsJson(object o, DateFormatHandling dfh = DateFormatHandling.IsoDateFormat)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings { DateFormatHandling = dfh};
            Type t = o.GetType();
            if( t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                //  settings.Formatting = Formatting.Indented;
                settings.ContractResolver = new DictionaryAsArrayResolver();
            }

            string so = JsonConvert.SerializeObject(o, settings);

            return Content(so, "application/json");
        }
         protected string modelStateErrors => string.Join(" <br /> ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
        protected List<ModelError> modelStateErrorsList => ModelState.Values.SelectMany(x => x.Errors).ToList();
  
    }
    class DictionaryAsArrayResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            if (objectType.GetInterfaces().Any(i => i == typeof(IDictionary) ||
                (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))))
            {
                return base.CreateArrayContract(objectType);
            }

            return base.CreateContract(objectType);
        }
    }
}