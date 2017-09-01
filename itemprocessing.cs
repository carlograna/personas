namespace PersonLoad
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class itemprocessing : DbContext
    {
        public itemprocessing()
            : base("name=itemprocessingCS")
        {
        }

        public virtual DbSet<KCCas> KCCases { get; set; }
        public virtual DbSet<KCPerson> KCPersons { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KCCas>()
                .Property(e => e.CaseNumber)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<KCCas>()
                .Property(e => e.Caption)
                .IsUnicode(false);

            modelBuilder.Entity<KCCas>()
                .Property(e => e.CaseNotes)
                .IsUnicode(false);

            modelBuilder.Entity<KCCas>()
                .Property(e => e.AllocationMethod)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<KCCas>()
                .Property(e => e.ModifiedBy)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<KCCas>()
                .Property(e => e.CountyID)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<KCCas>()
                .Property(e => e.DebtType)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<KCCas>()
                .Property(e => e.CPName)
                .IsUnicode(false);

            modelBuilder.Entity<KCCas>()
                .Property(e => e.ThirdPartyPayeeName)
                .IsUnicode(false);

            modelBuilder.Entity<KCCas>()
                .Property(e => e.CaseStatus)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<KCCas>()
                .Property(e => e.AltCaseNumber)
                .IsUnicode(false);

            modelBuilder.Entity<KCCas>()
                .Property(e => e.OtherID)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.SSN)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.LastName)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.FirstName)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.MiddleName)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.Suffix)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.Address)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.City)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.State)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.Zip)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.HomePhone)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.CellPhone)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.WorkPhone)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.WorkExtension)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.eMail)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.Country)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.Notes)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.ModifiedBy)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.PersonID)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.Pager)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.AlternateEmail)
                .IsUnicode(false);

            modelBuilder.Entity<KCPerson>()
                .Property(e => e.BillingEmail)
                .IsUnicode(false);
        }
    }
}
