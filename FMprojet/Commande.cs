namespace FMprojet.Models
{
    public class Commande
    {
        public int Id { get; set; }
        public int UtilisateurId { get; set; }
        public string UtilisateurNom { get; set; } = string.Empty;
        public int VehiculeId { get; set; }
        public string VehiculeMarqueModele { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public DateTime DateCommande { get; set; }
    }
}