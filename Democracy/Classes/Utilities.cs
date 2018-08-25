using Democracy.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace Democracy.Classes
{
    public class Utilities: IDisposable
    {
        private static DemocracyContext db = new DemocracyContext();

        public static string UploadPhoto(HttpPostedFileBase file)
        {
            //Upload image
            string path = String.Empty;
            string pic = String.Empty;

            if (file != null)
            {
                

                pic = Path.GetFileName(file.FileName);
                path = Path.Combine(HttpContext.Current.Server.MapPath("~/Content/Photos"), pic);
                file.SaveAs(path);
                
                using (MemoryStream ms = new MemoryStream())
                {
                    file.InputStream.CopyTo(ms);
                    byte[] array = ms.GetBuffer();
                }
            }

            return pic;
        }

        public static void CreateASPUser(UserView userView)
        {
            //User management
            var userContext = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(userContext));

            //Create User role
            string roleName = "User";

            //verificar si existe, si no existe lo creamos
            if (!roleManager.RoleExists(roleName))
            {
                roleManager.Create(new IdentityRole(roleName));
            }

            //Creamos ASP USER
            var userASP = new ApplicationUser
            {
                UserName = userView.UserName,
                Email = userView.UserName,
                PhoneNumber = userView.Phone
            };

            userManager.Create(userASP, userASP.UserName);

            //Agregamos Rol al usuario
            userASP = userManager.FindByName(userView.UserName);
            userManager.AddToRole(userASP.Id, "User");


        }

        public static void ChangeUserName(string currentUserName, UserChange user)
        {
            //User management
            var userContext = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));

            var userASP = userManager.FindByEmail(currentUserName);
            if (userASP==null)
            {
                return;
            }

            userManager.Delete(userASP);
            
            //Creamos ASP USER
            userASP = new ApplicationUser
            {
                UserName = user.UserName,
                Email = user.UserName,
                PhoneNumber = user.Phone
            };

            userManager.Create(userASP, user.CurrentPassword);

            //userContext.Entry(userASP).State = System.Data.Entity.EntityState.Modified;
            
            userManager.AddToRole(userASP.Id, "User");


        }

        public static List<Voting> MyVotings(User user)
        {
            //Traemos todas las votaciones que esten en estado abierto y que esten en el rango de fechas
            //correspondiente

            //Obtenemos el estado buscado 
            var state = GetState("Open");

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
                votingId = votings[i].VotingId;

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
                            .Where(gm => gm.UserId == user.UserId)
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

            return votings;

        }

        //Nos aseguramos el estado  exista en la tabla, si no lo creamos
        public static State GetState(string stateName)
        {
            var state = db.States
                .Where(s => s.Description == stateName)
                .FirstOrDefault();

            if (state == null)
            {
                state = new State()
                {
                    Description = stateName
                };
                db.States.Add(state);
                db.SaveChanges();

            }
            return state;
        }

        public void Dispose()
        {
            db.Dispose();
        }
    }
}
