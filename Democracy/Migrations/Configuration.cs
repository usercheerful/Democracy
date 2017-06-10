namespace Democracy.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Democracy.Models.DemocracyContext>
    {
        public Configuration()
        {
            AutomaticMigrationDataLossAllowed = true; //habilita que se pierdan datos en caso que algunas de las migraciones lo haga
            AutomaticMigrationsEnabled = true;
            ContextKey = "Democracy.Models.DemocracyContext";
        }

        protected override void Seed(Democracy.Models.DemocracyContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
        }
    }
}
