using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace vehiclecrash.Models
{
    public class ContributingFactor
        {
            [Key]
            public int ContributingFactorID { get; set; }
            public string ContributingFactorName { get; set; }

            // Navigation property for the Crash
            public virtual ICollection<Crash> Crashes { get; set; }
        }

        public class Crash
        {
            [Key]
            public int CrashID { get; set; }
            public DateTime CrashDate { get; set; }
            public TimeSpan CrashTime { get; set; }

            // Foreign key 
            public int ContributingFactorID { get; set; }
            public int StreetID { get; set; }

            public int NumberOfPersonsInjured { get; set; }
            public int NumberOfPersonsKilled { get; set; }
            public int NumberOfPedestriansInjured { get; set; }
            public int NumberOfPedestriansKilled { get; set; }
            public int NumberOfCyclistInjured { get; set; }
            public int NumberOfCyclistKilled { get; set; }

            // Navigation properties
            [ForeignKey("ContributingFactorID")]
            public virtual ContributingFactor ContributingFactor { get; set; }

            [ForeignKey("StreetID")]
            public virtual Address Address { get; set; }
        }

        public class Address
        {
            [Key]
            public int StreetID { get; set; }
            public string StreetName { get; set; }
            
            // Navigation property for the Crash
            public virtual ICollection<Crash> Crashes { get; set; }
        }

    public class CrashViewModel
    {
        public int CollisionID { get; set; }
        public DateTime CrashDate { get; set; }
        public TimeSpan CrashTime { get; set; }
        public string StreetName { get; set; }
        public string ContributingFactor { get; set; }
        public int NumberOfPersonsInjured { get; set; }
        public int NumberOfPersonsKilled { get; set; }
        public int NumberOfPedestriansInjured { get; set; }
        public int NumberOfPedestriansKilled { get; set; }
        public int NumberOfCyclistInjured { get; set; }
        public int NumberOfCyclistKilled { get; set; }
        public int NumberOfMotoristInjured { get; set; }
        public int NumberOfMotoristKilled { get; set; }
    }
}
