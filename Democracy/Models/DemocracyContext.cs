﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;

namespace Democracy.Models
{
    public class DemocracyContext:DbContext
    {
        public DemocracyContext()
            :base ("DefaultConnection")
        {

        }

        
        //QUITAMOS LA CONVENCION DE ELIMINACION EN CASCADA
        //NO ELIMINARA UN REGISTRO(TABLA PADRE) SI ENCUENTRA REFERENCIAS EN TABLAS HIJAS
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
        }

        public DbSet<State> States { get; set; }

        public DbSet<Group> Groups { get; set; }

        public DbSet<Voting> Votings { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<GroupMember> GroupMembers { get; set; }

        public DbSet<VotingGroup> VotingGroups { get; set; }

        public DbSet<Candidate> Candidates { get; set; }
    }

}