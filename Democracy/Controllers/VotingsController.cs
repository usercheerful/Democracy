using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Democracy.Models;
using System.IO;
using CrystalDecisions.CrystalReports.Engine;
using System.Configuration;
using System.Data.SqlClient;

namespace Democracy.Controllers
{
    
    public class VotingsController : Controller
    {
        private DemocracyContext db = new DemocracyContext();

        [Authorize(Roles = "Admin")]
        public ActionResult Close(int id)
        {
            var voting = db.Votings.Find(id);
            if (voting!=null)
            {
               
                var candidate = db.Candidates
                    .Where(c=>c.VotingId==voting.VotingId)  //extraemos todos los candidatos de una votacion
                    .OrderByDescending(c=>c.QuantityVotes).FirstOrDefault();  //obtenemos el candidato de mayor votación

                if (candidate!= null)
                {
                    var state = this.GetState("Closed");
                    voting.StateId = state.StateId;
                    voting.CandidateWinId = candidate.User.UserId;
                    db.Entry(voting).State = EntityState.Modified;
                    db.SaveChanges();
                }

            }

            return RedirectToAction("Index");
        }
        
        public ActionResult ShowResults(int id)
        {
            var report = this.GenerateResultReport(id);
            Stream stream = report.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            return File(stream, "application/pdf");
        }

        private ReportClass GenerateResultReport(int id)
        {
            DataTable datatable = new DataTable();
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                string sql = @"SELECT Votings.VotingId, Votings.Description AS Voting, States.Description AS State, 
                                        Users.FirstName + ' ' + Users.LastName AS Canditate, Candidates.QuantityVotes
                               FROM Candidates INNER JOIN
                                     Users ON Candidates.UserId = Users.UserId INNER JOIN
                                     Votings ON Candidates.VotingId = Votings.VotingId INNER JOIN
                                     States ON Votings.StateId = States.StateId
                                     where Votings.VotingId="+id;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var comand = new SqlCommand(sql, connection))
                    {
                        var adapter = new SqlDataAdapter(comand);
                        adapter.Fill(datatable);
                    }
                }

            }
            catch (Exception)
            {

            }

            var report = new ReportClass();
            report.FileName = Server.MapPath("/Reports/Results.rpt");
            //cargamos el reporte en memoria
            report.Load();
            //cargamos el origen de datos
            report.SetDataSource(datatable);

            return report;
        }

        [Authorize(Roles = "User")]
        public ActionResult Results()
        {
            var state = this.GetState("Closed");
            var votings2 = db.Votings
                .Where(v=>v.StateId==state.StateId)
                .Include(v => v.State).ToList();
            
            var views = new List<VotingIndexView>();
            User user = null;
            foreach (var voting in votings2)
            {
                if (voting.CandidateWinId != 0)
                {
                    user = db.Users.Find(voting.CandidateWinId);
                }
                else
                {
                    user = null;
                }

                views.Add(new VotingIndexView()
                {
                    CandidateWinId = voting.CandidateWinId,
                    DateTimeEnd = voting.DateTimeEnd,
                    DateTimeStart = voting.DateTimeStart,
                    Description = voting.Description,
                    IsEnabledBlankVote = voting.IsEnabledBlankVote,
                    IsForAllUsers = voting.IsForAllUsers,
                    QuantityBlankVotes = voting.QuantityVotes,
                    Remarks = voting.Remarks,
                    StateId = voting.StateId,
                    State = voting.State,
                    VotingId = voting.VotingId,
                    Winner = user
                });
            }


            return View(views);
        }
        
        [Authorize(Roles = "User")]
        public ActionResult VoteForCandidate(int candidateId, int votingId)
        {
            //validamos el usuario
            var user = db.Users.Where(u=>u.UserName==this.User.Identity.Name).FirstOrDefault();
            if (user == null)
            {
                return RedirectToAction("Index","Home");
            }

            //validamos el canditato
            var candidate = db.Candidates.Find(candidateId);
            if (candidate == null)
            {
                return RedirectToAction("Index", "Home");
            }

            //validamos el voting
            var voting = db.Votings.Find(votingId);
            if (voting == null)
            {
                return RedirectToAction("Index", "Home");
            }

            //si el usuario llega a votar
            if (this.VoteCandidate(user,candidate,voting))
            {
                return RedirectToAction("MyVotings");
            }

            //Llega a esta linea si el usuario no puede  votar
            return RedirectToAction("Index", "Home");
        }

        private bool VoteCandidate(User user, Candidate candidate, Voting voting)
        {
            //Debido que el voto se registrara en varias tablas, usaremos transacciones
            using (var transaction = db.Database.BeginTransaction())
            {
                var votingDetail = new VotingDetail() {
                    CandidateId = candidate.CandidateId,
                    DateTime = DateTime.Now,
                    UserId= user.UserId,
                    VotingId= voting.VotingId
                };

                //agregamos el el voting details
                db.VotingDetails.Add(votingDetail);

                //modificamos la cantidad de votos del candidato
                candidate.QuantityVotes++;
                db.Entry(candidate).State = EntityState.Modified;

                //modificamos la cantidad de votos del voting
                voting.QuantityVotes++;
                db.Entry(voting).State = EntityState.Modified;

                try
                {
                    //guardamos los registros
                    db.SaveChanges();
                    //si no hay ningun error se confirma la transacción
                    transaction.Commit();
                    return true;
                }
                catch (Exception)
                {
                    //deshacemos los registros modificados antes de que haya ocurrido el error
                    transaction.Rollback();
                }
            }

            return false;
        }

        [Authorize(Roles = "User")]
        public ActionResult Vote(int VotingId)
        {
            var voting = db.Votings.Find(VotingId);
            var d = db.Votings.First(p=>p.VotingId== VotingId);
            var d2 = db.Votings.Where(p => p.VotingId == VotingId).First();
            var view = new VotingVoteView() {
                
                DateTimeStart = voting.DateTimeStart,
                DateTimeEnd = voting.DateTimeEnd,
                Description = voting.Description,
                IsEnabledBlankVote = voting.IsEnabledBlankVote,
                Candidates= voting.Candidates.ToList(),
                MyCandidates = voting.Candidates.ToList(),
                Remarks = voting.Remarks,
                VotingId = voting.VotingId
            };

            return View(view);
        }

        [Authorize(Roles = "User")]
        public ActionResult MyVotings()
        {
            //this.User.Identity.Name -> Devuelve el nombre del usuario logeado
            var user = db.Users
                .Where(u => u.UserName == this.User.Identity.Name).FirstOrDefault();

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Hay un error con el usuario actual, llamar a soporte :)");
                return View();
            }



            //Traemos todas las votaciones que esten en estado abierto y que esten en el rango de fechas
            //correspondiente

            //Obtenemos el estado buscado 
            var state = this.GetState("Open");

            //Obtener de event votings por el tiempo actual
            var votings = db.Votings
                .Where(v => v.StateId == state.StateId &&
                v.DateTimeStart <= DateTime.Now && v.DateTimeEnd >= DateTime.Now)
                .Include(v => v.Candidates)
                .Include(v => v.VotingGroups)
                .Include(v => v.State)
                .ToList();
            

            //haciendo pruebas es lo mismo que el este codigo
            /*var votings22 = db.Votings
                .Where(v => v.StateId == state.StateId &&
                v.DateTimeStart <= DateTime.Now && v.DateTimeEnd >= DateTime.Now).ToList();*/

            //Descartar los eventos en los cuales el usuario ya votó
            int userId = user.UserId;
            int votingId;
            VotingDetail votingDetail;

            //opcional para eliminar (1)
            List<Voting> elementosEliminar = new List<Voting>();


            for (int i = 0; i < votings.Count; i++)
            {
                votingId =votings[i].VotingId;

                //veremos si el usuaio ya realizo un voto
                votingDetail = db.VotingDetails
                    .Where(vd => vd.VotingId == votingId && vd.UserId == userId)
                    .FirstOrDefault();

                if (votingDetail != null)
                {
                    //votings.RemoveAt(i);

                    //opcional para eliminar (1)
                    elementosEliminar.Add(votings[i]);

                    //opcional para eliminar (2)
                    /*votings.RemoveAll(v=>db.VotingDetails
                                    .Where(vd => vd.VotingId == v.VotingId && vd.UserId == userId)
                                    .FirstOrDefault()!=null);*/

                }

            }
            //opcional para eliminar (1)
            foreach (var item in elementosEliminar)
            {
                votings.Remove(item);
            }

            elementosEliminar.Clear();

            //Descartamos los eventos por grupos en los cuales el usuario no está incluido

            for (int i = 0; i < votings.Count; i++)
            {
                //evaluamos si el voting es para todos los usuarios
                if (!votings[i].IsForAllUsers)
                {
                    bool userBelongsToGroup = false;
                    var lista = votings[i].VotingGroups;
                    foreach (var votingGroup in votings[i].VotingGroups)
                    {
                        //vemos si el usuario es miembro de algun grupo
                        var userGroup = votingGroup.Group.GroupMembers
                            .Where(gm=>gm.UserId == user.UserId)
                            .FirstOrDefault();

                        /*var usgroup = (from p in votingGroup.Group.GroupMembers
                                      where p.UserId == user.UserId
                                      select p).FirstOrDefault();*/

                        //el usuario pertenece a almenos a un grupo
                        if (userGroup != null)
                        {
                            userBelongsToGroup = true;
                            break;
                        }
                        
                    }

                    //si el usuario no pertenece al grupo
                    if (!userBelongsToGroup)
                    {
                        //votings.RemoveAt(i);
                        elementosEliminar.Add(votings[i]);
                    }
                }
                
            }

            //opcional para eliminar (1)
            foreach (var item in elementosEliminar)
            {
                votings.Remove(item);
            }

            elementosEliminar.Clear();


            return View(votings);
        }


        //Nos aseguramos el estado  exista en la tabla, si no lo creamos
        private State GetState(string stateName)
        {
            var state = db.States
                .Where(s=>s.Description==stateName)
                .FirstOrDefault();

            if (state == null)
            {
                state = new State(){
                    Description = stateName
                };
                db.States.Add(state);
                db.SaveChanges();
                
            }
            return state;
        }

        [Authorize(Roles = "Admin")]
        public ActionResult DeleteGroup(int id )
        {
            var votingGroup = db.VotingGroups.Find(id);

            if (votingGroup != null)
            {
                db.VotingGroups.Remove(votingGroup);
                db.SaveChanges();
            }

            return RedirectToAction("Details", new { id = votingGroup.VotingId });
        }

        [Authorize(Roles = "Admin")]
        public ActionResult DeleteCandidate(int id)
        {
            var candidate = db.Candidates.Find(id);

            if (candidate != null)
            {
                db.Candidates.Remove(candidate);
                db.SaveChanges();
            }

            return RedirectToAction("Details", new { id = candidate.VotingId });
        }


        [Authorize(Roles = "Admin")]
        public ActionResult AddCandidate(int id)
        {

            var view = new AddCandidateView
            {
                VotingId = id
            };

            ViewBag.UserId = new SelectList(db.Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName), "UserId", "FullName");
            return View(view);
        }

        [HttpPost]
        public ActionResult AddCandidate(AddCandidateView view)
        {
            if (ModelState.IsValid)
            {
                var candidate = db.Candidates
                    .Where(c => c.VotingId == view.VotingId && c.UserId == view.UserId)
                    .FirstOrDefault();

                //Si en caso ya esta asignado ese candidato al voting
                if (candidate != null)
                {
                    ModelState.AddModelError(string.Empty, "The candidate already belong to voting");
                    //ViewBag.Error = "";
                    ViewBag.UserId = new SelectList(db.Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName), "UserId", "FullName");
                    return View(view);

                }
                candidate = new Candidate()
                {
                    VotingId = view.VotingId,
                    UserId = view.UserId
                };


                db.Candidates.Add(candidate);
                db.SaveChanges();

                return RedirectToAction("Details", new { id = view.VotingId });
                //return RedirectToAction(string.Format("Details/{0}",view.VotingId));

            }

            ViewBag.UserId = new SelectList(db.Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName), "UserId", "FullName");

            return View(view);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult AddGroup(int id)
        {
            ViewBag.GroupId = new SelectList(db.Groups.OrderBy(g=>g.Description), 
                "GroupId", 
                "Description");

            var view = new AddGroupView
            {
                VotingId = id
            };

            
            return View(view);
        }

        [HttpPost]
        public ActionResult AddGroup(AddGroupView view)
        {
            if (ModelState.IsValid)
            {
                var votingGroup = db.VotingGroups
                    .Where(vg=>vg.VotingId==view.VotingId && vg.GroupId== view.GroupId)
                    .FirstOrDefault();

                //Si en caso ya esta asignado ese grupo al voting
                if (votingGroup!= null)
                {
                    ModelState.AddModelError(string.Empty, "The group already belong to group");
                    //ViewBag.Error = "";
                    ViewBag.GroupId = new SelectList(db.Groups.OrderBy(g => g.Description),
                        "GroupId",
                        "Description");

                    return View(view);

                }
                votingGroup = new VotingGroup() {
                    VotingId = view.VotingId,
                    GroupId = view.GroupId
                };


                db.VotingGroups.Add(votingGroup);
                db.SaveChanges();
                
                return RedirectToAction("Details" , new { id =  view.VotingId});
                //return RedirectToAction(string.Format("Details/{0}",view.VotingId));

            }

            ViewBag.GroupId = new SelectList(db.Groups.OrderBy(g => g.Description),
                "GroupId",
                "Description");

            return View(view);

        }


        [Authorize(Roles = "Admin")]
        // GET: Votings
        public ActionResult Index()
        {
            /*
             Ejm:
             var customers = context.Customers.ToList();  
             SELECT * FROM Customers;

             var customersWithOrderDetail = context.Customers.Include("Orders").ToList();
             SELECT * FROM Customers JOIN Orders ON Customers.Id = Orders.CustomerId;
             */

            //var votings = db.Votings.Include(v => v.State);
            var votings2 = db.Votings.Include(v => v.State).ToList();
            //var votings3 = db.Votings.ToList();
            
            var views = new List<VotingIndexView>();
            User user = null;
            foreach (var voting in votings2)
            {
                if (voting.CandidateWinId !=0)
                {
                    user = db.Users.Find(voting.CandidateWinId);
                }
                else
                {
                    user = null;
                }

                views.Add(new VotingIndexView() {
                    CandidateWinId = voting.CandidateWinId,
                    DateTimeEnd = voting.DateTimeEnd,
                    DateTimeStart = voting.DateTimeStart,
                    Description = voting.Description,
                    IsEnabledBlankVote = voting.IsEnabledBlankVote,
                    IsForAllUsers = voting.IsForAllUsers,
                    QuantityBlankVotes = voting.QuantityVotes,
                    Remarks = voting.Remarks,
                    StateId = voting.StateId,
                    State = voting.State,
                    VotingId = voting.VotingId,
                    Winner = user
                });
            }


            return View(views);
            
        }

        [Authorize(Roles = "Admin")]
        // GET: Votings/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Voting voting = db.Votings.Find(id);
            
            if (voting == null)
            {
                return HttpNotFound();
            }

            var view = new DetailsVotingView() {
                Candidates = voting.Candidates.ToList(),
                CandidateWinId = voting.CandidateWinId,
                DateTimeEnd = voting.DateTimeEnd,
                DateTimeStart = voting.DateTimeStart,
                Description = voting.Description,
                IsEnabledBlankVote = voting.IsEnabledBlankVote,
                IsForAllUsers = voting.IsForAllUsers,
                QuantityBlankVotes = voting.QuantityBlankVotes,
                QuantityVotes = voting.QuantityVotes,
                Remarks = voting.Remarks,
                StateId = voting.StateId,
                State= voting.State,
                VotingGroups = voting.VotingGroups.ToList(),
                VotingId= voting.VotingId
            };


            return View(view);
        }


        [Authorize(Roles = "Admin")]
        // GET: Votings/Create
        public ActionResult Create()
        {
            ViewBag.StateId = new SelectList(db.States, "StateId", "Description");

            var view = new VotingView {
                DateStart = DateTime.Now,
                DateEnd = DateTime.Now
            };

            return View(view);
        }

        // POST: Votings/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que desea enlazarse. Para obtener 
        // más información vea http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(VotingView view)
        {
            if (ModelState.IsValid)
            {
                var voting = new Voting {
                    DateTimeEnd = view.DateEnd
                                    .AddHours(view.TimeEnd.Hour)
                                    .AddMinutes(view.TimeEnd.Minute),

                    DateTimeStart = view.DateStart
                                    .AddHours(view.TimeStart.Hour)
                                    .AddMinutes(view.TimeStart.Minute),

                    Description = view.Description,
                    IsEnabledBlankVote = view.IsEnabledBlankVote,
                    IsForAllUsers= view.IsForAllUsers,
                    Remarks = view.Remarks,
                    StateId = view.StateId
                };

                db.Votings.Add(voting);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.StateId = new SelectList(db.States, "StateId", "Description", view.StateId);
            return View(view);
        }


        [Authorize(Roles = "Admin")]
        // GET: Votings/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Voting voting = db.Votings.Find(id);
            if (voting == null)
            {
                return HttpNotFound();
            }
            ViewBag.StateId = new SelectList(db.States, "StateId", "Description", voting.StateId);

            VotingView view = new VotingView {
                DateEnd = voting.DateTimeEnd,
                DateStart = voting.DateTimeStart,
                Description=voting.Description,
                IsEnabledBlankVote = voting.IsEnabledBlankVote,
                IsForAllUsers = voting.IsForAllUsers,
                Remarks = voting.Remarks,
                StateId = voting.StateId,
                TimeEnd = voting.DateTimeEnd,
                TimeStart = voting.DateTimeStart,
                VotingId = voting.VotingId
                
            };

            return View(view);
        }

        // POST: Votings/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que desea enlazarse. Para obtener 
        // más información vea http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "VotingId,Description,StateId,Remarks,DateTimeStart,DateTimeEnd,IsForAllUsers,IsEnabledBlankVote,QuantityVotes,QuantityBlankVotes,CandidateWinId")] Voting voting)
        public ActionResult Edit(VotingView view)
        {
            if (ModelState.IsValid)
            {

                var voting = new Voting
                {
                    DateTimeEnd = view.DateEnd
                                   .AddHours(view.TimeEnd.Hour)
                                   .AddMinutes(view.TimeEnd.Minute),

                    DateTimeStart = view.DateStart
                                   .AddHours(view.TimeStart.Hour)
                                   .AddMinutes(view.TimeStart.Minute),

                    Description = view.Description,
                    IsEnabledBlankVote = view.IsEnabledBlankVote,
                    IsForAllUsers = view.IsForAllUsers,
                    Remarks = view.Remarks,
                    StateId = view.StateId,
                    VotingId = view.VotingId
                };

                
                db.Entry(voting).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.StateId = new SelectList(db.States, "StateId", "Description", view.StateId);
            return View(view);
        }


        [Authorize(Roles = "Admin")]
        // GET: Votings/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Voting voting = db.Votings.Find(id);
            if (voting == null)
            {
                return HttpNotFound();
            }
            return View(voting);
        }

        // POST: Votings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Voting voting = db.Votings.Find(id);
            db.Votings.Remove(voting);
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
