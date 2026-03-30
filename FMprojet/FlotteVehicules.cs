using FMprojet.Models;
using System.Collections.Generic;
using System.Linq;

namespace FMprojet
{
    /// <summary>
    /// Gestion en mémoire d'une flotte de véhicules (ajout/suppression/recherches).
    /// </summary>
    public class FlotteVehicules
    {
        public List<Vehicule> Vehicules { get; } = new();

        public void Ajouter(Vehicule vehicule) => Vehicules.Add(vehicule);

        public bool SupprimerParImmatriculation(string immatriculation)
        {
            var cible = Vehicules.FirstOrDefault(v =>
                v.Immatriculation.Equals(immatriculation, System.StringComparison.OrdinalIgnoreCase));
            if (cible is null) return false;
            Vehicules.Remove(cible);
            return true;
        }

        public Vehicule? RechercherParImmatriculation(string immatriculation) =>
            Vehicules.FirstOrDefault(v =>
                v.Immatriculation.Equals(immatriculation, System.StringComparison.OrdinalIgnoreCase));

        public List<Vehicule> RechercherParMarque(string marque) =>
            Vehicules.Where(v => v.Marque.Contains(marque, System.StringComparison.OrdinalIgnoreCase)).ToList();

        public List<Vehicule> RechercherParEnergie(string energie) =>
            Vehicules.Where(v => v.Carburant.Contains(energie, System.StringComparison.OrdinalIgnoreCase)).ToList();

        public List<Vehicule> RechercherParKilometrageMax(int kilometrageMax) =>
            Vehicules.Where(v => v.Kilometrage <= kilometrageMax).ToList();
    }
}


