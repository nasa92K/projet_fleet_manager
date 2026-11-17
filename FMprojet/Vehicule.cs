
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

        /// <summary>
        /// Date de mise en circulation (mise en service) du véhicule.
        /// Colonne `circulation` DATE dans la table vehicules.
        /// Null si non renseignée.
        /// </summary>
        public DateTime? Circulation { get; set; }

        // ─── Propriétés calculées ─────────────────────────────────────────

        /// <summary>
        /// Âge du véhicule en années entières, calculé à partir de la date de
        /// circulation si elle est renseignée, sinon à partir de l'année de fabrication.
        /// </summary>
        public int AgeCalcule
        {
            get
            {
                if (Circulation.HasValue)
                {
                    var today = DateTime.Today;
                    int age = today.Year - Circulation.Value.Year;
                    // Soustraire 1 si l'anniversaire n'est pas encore passé cette année
                    if (Circulation.Value.Date > today.AddYears(-age)) age--;
                    return age;
                }
                // Fallback : utiliser l'année de fabrication
                return DateTime.Now.Year - Annee;
            }
        }

        /// <summary>
        /// Indique si le véhicule dépasse les deux critères de remplacement :
        ///   • Âge ≥ 4 ans
        ///   • Kilométrage ≥ 80 000 km
        /// </summary>
        public bool EstARemplacer => AgeCalcule >= 4 && Kilometrage >= 80_000;

        /// <summary>
        /// Indique si le véhicule peut être commandé par un client :
        ///   • Âge < 4 ans
        ///   • Kilométrage < 80 000 km
        /// </summary>
        public bool EstCommandable => AgeCalcule < 4 && Kilometrage < 80_000;

        /// <summary>
        /// Statut de remplacement calculé dynamiquement.
        /// 🚨 À REMPLACER / ⚠️ À MONITORER / ✅ EN SERVICE
        /// </summary>
        public string StatutRemplacement
        {
            get
            {
                bool ageTrop = AgeCalcule >= 4;
                bool kmTrop  = Kilometrage >= 80_000;
                if (ageTrop && kmTrop)  return "🚨 À REMPLACER";
                if (ageTrop || kmTrop)  return "⚠️ À MONITORER";
                return "✅ EN SERVICE";
            }
        }
    }
}