using System;

namespace FMprojet.Models
{
    public class HistoriqueVehicule
    {
        public int Id { get; set; }
        public int VehiculeId { get; set; }
        public string Type { get; set; } = string.Empty; // kilometrage, carburant, entretien, peage, consommation
        public string? ValeurTexte { get; set; }
        public decimal? ValeurNum { get; set; }
        public decimal? Cout { get; set; }
        public DateTime DateEvent { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

