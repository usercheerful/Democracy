using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using Democracy.Models;
using Newtonsoft.Json.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Democracy.Classes;

namespace Democracy.Controllers.API
{

    [RoutePrefix("api/Users")] // Todos los metodos que estan en este controlador empezaran con api/Users
    public class UsersController : ApiController
    {
        private DemocracyContext db = new DemocracyContext();

        /*[HttpGet]
        [Route("Login/{email}/{password}")]// El nombre de email y password debe ser igual al nombre puesto en los parametros del metodo
        public IHttpActionResult Login(string email, string password) 
        {
            var user = db.Users.FirstOrDefault();
            return this.Ok(user);
        }*/

        //api/Users/Login para el metodo POST
        [HttpPost]
        [Route("Login")]
        public IHttpActionResult Login(JObject form)
        {
            dynamic jsonObject = form;
            var email = String.Empty;
            var password = String.Empty;

            /*var emal = form.GetValue("email");
            var pass = form.GetValue("password");*/

            try
            {
                email = jsonObject.email.Value;
                
                
            }
            catch { }

            if ( string.IsNullOrEmpty(email))
            {
                return this.BadRequest("Incorrect call");
            }

            try
            {
                password = jsonObject.password.Value;
            }
            catch { }

            if (string.IsNullOrEmpty(password))
            {
                return this.BadRequest("Incorrect call");
            }


            var userContext = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));

            var userASP = userManager.Find(email, password);
            if (userASP == null)
            {
                return this.BadRequest("Incorrect user or password");
            }

            var user = db.Users.Where(u=>u.UserName == email).FirstOrDefault();

            if (user == null)
            {
                return this.BadRequest("Problem better call saul");
            }

            return this.Ok(user);
        }


        /*  // GET: api/Users
          public IQueryable<User> GetUsers()
          {
              return db.Users;
          }

          // GET: api/Users/5
          [ResponseType(typeof(User))]
          public IHttpActionResult GetUser(int id)
          {
              User user = db.Users.Find(id);
              if (user == null)
              {
                  return NotFound();
              }

              return Ok(user);
          }*/

        [HttpPut]
        [Route("ChangePassword/{userId}")]
        /*[Route("ChangePassword")]
        public IHttpActionResult ChangePassword(JObject form)*/
        public IHttpActionResult ChangePassword(int userId,JObject form )
        {
            //int userId = 0;
            string oldPassword = string.Empty;
            string newPassword = string.Empty;
            dynamic jsonObject = form;

            

            try
            {
                //userId = (int)jsonObject.userId.Value;
                oldPassword = jsonObject.oldPassword.Value;
                newPassword = jsonObject.newPassword.Value;

            }
            catch {
                return this.BadRequest("Incorrect call");
            }

            var user = db.Users.Find(userId);
            if (user==null)
            {
                return this.BadRequest("Usuario no ha sido encontrado");
            }

            var userContext = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));

            var userASP = userManager.Find(user.UserName, oldPassword);
            if (userASP == null)
            {
                return this.BadRequest("Incorrect oldpassword");
            }

            //cambiamos la contraseña
            var response = userManager.ChangePassword(userASP.Id,oldPassword,newPassword);

            //verificamos la respouesta
            if (response.Succeeded)
            {
                //return this.Ok("La contraseña fue cambiada satisfactoriamente");
                return this.Ok<object>(new {Message= "La contraseña fue cambiada satisfactoriamente" });
                //return this.Json<object>(new { Message= "La contraseña fue cambiada satisfactoriamente" });
                

            }
            else
            {
                return this.BadRequest(response.Errors.ToString());
            }

        }



        // PUT: api/Users/5
        //[ResponseType(typeof(void))]
        [HttpPut]
        public IHttpActionResult PutUser(int id, UserChange user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != user.UserId)
            {
                return BadRequest();
            }

            var currentUser = db.Users.Find(id);
            if (currentUser == null)
            {
                return this.BadRequest("User no ha sido encontrado");
            }

            var userContext = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));
            var userASP = userManager.Find(currentUser.UserName,user.CurrentPassword);
            if (userASP == null)
            {
                return this.BadRequest("Password equivocado");
            }

            //verificamos si el usuarioa ha cambiado de correo
            if (currentUser.UserName != user.UserName)
            {
                //cambiamos el correo del usuario, enviamos el dato actual del usuario(UserName) 
                //y el objeto user con los nuevos datos
                Utilities.ChangeUserName(currentUser.UserName, user);
            }

            var userModel = new User() {
                Address = user.Address,
                FirstName = user.FirstName,
                Grade = user.Grade,
                Group = user.Group,
                LastName = user.LastName,
                Phone = user.Phone,
                UserId = user.UserId,
                UserName = user.UserName
            };
            

            db.Entry(userModel).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return this.BadRequest(ex.Message.ToString());
            }

            return this.Ok(user);
        }

        // POST: api/Users
        /*[ResponseType(typeof(User))]
        public IHttpActionResult PostUser(User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Users.Add(user);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = user.UserId }, user);
        }*/

        [HttpPost]
        public IHttpActionResult PostUser(RegisterUserView userView)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new User
            {
                Address = userView.Address,
                FirstName = userView.FirstName,
                Grade = userView.Grade,
                Group = userView.Group,
                LastName = userView.LastName,
                Phone = userView.Phone,
                UserName = userView.UserName
            };

            db.Users.Add(user);
            db.SaveChanges();

            Utilities.CreateASPUser(userView);
            return CreatedAtRoute("DefaultApi", new { id = user.UserId }, user);
        }


       /* // DELETE: api/Users/5
        [ResponseType(typeof(User))]
        public IHttpActionResult DeleteUser(int id)
        {
            User user = db.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            db.Users.Remove(user);
            db.SaveChanges();

            return Ok(user);
        }*/

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool UserExists(int id)
        {
            return db.Users.Count(e => e.UserId == id) > 0;
        }
    }
}