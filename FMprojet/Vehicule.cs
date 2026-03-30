
namespace FMprojet.Models
{
    public class Vehicule
    {
        public int Id { get; set; }
        public string Marque { get; set; } = string.Empty;
        public string Modele { get; set; } = string.Empty;
        public string Immatriculation { get; set; } = string.Empty;
        public int Annee { get; set; }
        public decimal Prix { get; set; }
        public int Kilometrage { get; set; }
        public string Carburant { get; set; } = string.Empty;
        public string Transmission { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Image { get; set; }
        public int CategorieId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}