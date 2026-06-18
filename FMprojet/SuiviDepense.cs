using System;

namespace FMprojet.Models
{
    public class SuiviDepense
    {
        public int Id { get; set; }
        public int VehiculeId { get; set; }
        public DateTime DateJour { get; set; }

        public int? Kilometrage { get; set; }

        public decimal? CarburantQte { get; set; }
        public decimal? CarburantPrix { get; set; }

        public decimal? PeagesFrais { get; set; }

        public string? EntretienType { get; set; }
        public decimal? EntretienCout { get; set; }

        public DateTime? AssuranceEcheance { get; set; }
        public decimal? AssuranceMontant { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

