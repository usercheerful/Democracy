using Democracy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Democracy.Controllers
{
    public class StatesController : Controller
    {

        DemocracyContext db = new DemocracyContext();

        // GET: States
        public ActionResult Index()
        {
            return View(db.States.ToList());
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(State state)
        {
            if (!ModelState.IsValid)
            {
                return View(state);
            }
            db.States.Add(state);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}