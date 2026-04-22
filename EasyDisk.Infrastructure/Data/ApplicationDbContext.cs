using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options) 
        { 
        }

        public DbSet<FileEntity> Files { get; set; }
        public DbSet<FolderEntity> Folders { get; set; }
        public DbSet<ShareLinkEntity> ShareLinks { get; set; }
        public DbSet<FileVersionEntity> FileVersions { get; set; }
        public DbSet<TagEntity> Tags { get; set; }
        public DbSet<AuditLogEntity> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<FileEntity>().HasQueryFilter(f => f.DeletedAt == null);
            builder.Entity<FolderEntity>().HasQueryFilter(f => f.DeletedAt == null);

            builder.Entity<FolderEntity>()
                .HasOne(f => f.ParentFolder)
                .WithMany(f => f.Subfolders)
                .HasForeignKey(f => f.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FileEntity>()
                .HasOne(f => f.Folder)
                .WithMany(f => f.Files)
                .HasForeignKey(f => f.FolderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FileEntity>()
                .HasMany(f => f.Tags)
                .WithMany(t => t.Files)
                .UsingEntity(j => j.ToTable("FileTags"));

            builder.Entity<ShareLinkEntity>()
                .HasOne(s => s.File)
                .WithMany(f => f.ShareLinks)
                .HasForeignKey(s => s.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ShareLinkEntity>()
                .HasOne(s => s.Folder)
                .WithMany()
                .HasForeignKey(s => s.FolderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FileVersionEntity>()
                .HasOne(v => v.File)
                .WithMany(f => f.Versions)
                .HasForeignKey(v => v.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
