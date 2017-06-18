﻿using Democracy.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
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

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var state = db.States.Find(id);

            if (state ==null)
            {
                return HttpNotFound();
            }

            return View(state);
        }

        [HttpPost]
        public ActionResult Edit(State state)
        {
            if (!ModelState.IsValid)
            {
                return View(state);
            }
            db.Entry(state).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var state = db.States.Find(id);

            if (state == null)
            {
                return HttpNotFound();
            }

            return View(state);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var state = db.States.Find(id);

            if (state == null)
            {
                return HttpNotFound();
            }

            return View(state);
        }

        [HttpPost]
        public ActionResult Delete(int? id, State state)
        {
            state = db.States.Find(id);

            if (state == null)
            {
                return HttpNotFound();
            }

            db.States.Remove(state);

            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {

                if (ex.InnerException != null &&
                    ex.InnerException.InnerException!= null &&
                    ex.InnerException.InnerException.Message.Contains("REFERENCE"))
                {
                    ViewBag.Error = "No se puede eliminar el registro, porque esta relacionado con otros registros";
                }else
                {
                    ViewBag.Error = ex.Message;
                }

                return View(state);
            }


            
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