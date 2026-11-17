namespace FMprojet.Models
{
    public class Commande
    {
        public int Id { get; set; }
        public int UtilisateurId { get; set; }
        public string UtilisateurNom { get; set; } = string.Empty;
        public int VehiculeId { get; set; }
        public string VehiculeMarqueModele { get; set; } = string.Empty;
        public string Statut { get; set; } = StatutCommande.EnAttente;
        public DateTime DateCommande { get; set; }

        /// <summary>Statut lisible pour l'affichage (ex : "En attente").</summary>
        public string StatutLibelle => StatutCommande.Libelle(Statut);
    }

    /// <summary>
    /// Valeurs autorisées par l'ENUM MySQL commandes.statut.
    /// </summary>
    public static class StatutCommande
    {
        public const string EnAttente = "en_attente";
        public const string Confirmee = "confirmee";
        public const string Annulee   = "annulee";

        public static string Libelle(string? statut) => statut switch
        {
            EnAttente => "En attente",
            Confirmee => "Confirmée",
            Annulee   => "Annulée",
            _         => statut ?? string.Empty
        };
    }
}
