﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TalentHunt1.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class Talent_HuntEntities7 : DbContext
    {
        public Talent_HuntEntities7()
            : base("name=Talent_HuntEntities7")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Apply> Apply { get; set; }
        public virtual DbSet<AssignedMember> AssignedMember { get; set; }
        public virtual DbSet<CommitteeMember> CommitteeMember { get; set; }
        public virtual DbSet<Event> Event { get; set; }
        public virtual DbSet<EventReviews> EventReviews { get; set; }
        public virtual DbSet<Marks> Marks { get; set; }
        public virtual DbSet<Submission> Submission { get; set; }
        public virtual DbSet<Task> Task { get; set; }
        public virtual DbSet<Users> Users { get; set; }
    }
}
