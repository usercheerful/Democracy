using Democracy.Migrations;
using Democracy.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Democracy
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DemocracyContext, Configuration>());
            this.CheckSuperuser();

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private void CheckSuperuser()
        {
            var userContext = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));
            var db = new DemocracyContext();
            
            this.CheckRole("Admin", userContext);
            this.CheckRole("User", userContext);


            //verificamos si el usuario existe en la tabla users
            var user = db.Users
                .Where(u=>u.UserName.ToLower().Equals("rosswil94@gmail.com")).FirstOrDefault();

            if (user == null)
            {
              //save record
                user = new User
                {
                    Address = "Calle Luna Calle sol",
                    FirstName = "Wild",
                    LastName = "Rosales",
                    Phone = "223 3345 3454",
                    UserName = "rosswil94@gmail.com",
                    Photo = "~/Content/Photos/android.jpg"
                };
                db.Users.Add(user);
                db.SaveChanges();
            }

            //verificamos si el usuario existe en la tabla AspNetUsers
            var userASP = userManager.FindByName(user.UserName);

            if (userASP == null)
            {
                //Creamos ASP USER
                userASP = new ApplicationUser
                {
                    UserName = user.UserName,
                    Email = user.UserName,
                    PhoneNumber = user.Phone
                };

                userManager.Create(userASP, "wilder123*");
            }
            //userASP = userManager.FindByName(userView.UserName);
            userManager.AddToRole(userASP.Id,"Admin");


        }

        private void CheckRole(string roleName, ApplicationDbContext userContext)
        {

            //User management
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(userContext));

            //verificar si existe, si no existe lo creamos
            if (!roleManager.RoleExists(roleName))
            {
                roleManager.Create(new IdentityRole(roleName));
            }



        }
    }
}
