using Microsoft.EntityFrameworkCore;
using vehiclecrash.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace vehiclecrash.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<ContributingFactor> ContributingFactorData { get; set; }
        public DbSet<Crash> CrashData { get; set; }
        public DbSet<Address> AddressData { get; set; }
    }
}
