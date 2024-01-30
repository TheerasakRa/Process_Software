using Microsoft.EntityFrameworkCore;
using Process_Software.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using System;

namespace Process_Software.Models
{
    public class Process_Software_Context : DbContext
    {
        public DbSet<Provider> Provider { get; set; }
        public DbSet<ProviderLog> ProviderLog { get; set; }
        public DbSet<Status> Status { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Work> Work { get; set; }
        public DbSet<WorkLog> WorkLog { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=DESKTOP-07MEU62\SQLEXPRESS;Initial Catalog=Process_Software_Model; Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False");
        }
    }
}