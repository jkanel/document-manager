using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Entity.Migrations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using FileManager.Model;

namespace FileManager.Data
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(string ConnectionString) : base(ConnectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // prevent cascading deletes
            // foreign keys handled in model objects through annotations
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.HasDefaultSchema("dbo");

            base.OnModelCreating(modelBuilder);

        }

        public void RefreshDocuments()
        {
            this.Database.ExecuteSqlCommand("EXEC dbo.DocumentRefresh");
        }

        public void TruncateDocumentWords()
        {
            this.Database.ExecuteSqlCommand("TRUNCATE TABLE dbo.DocumentWord");
        }

        public virtual DbSet<Document> Documents { get; set; }
        public virtual DbSet<DocumentFile> DocumentFiles { get; set; }
        public virtual DbSet<DocumentWord> DocumentWords { get; set; }

    }
}
