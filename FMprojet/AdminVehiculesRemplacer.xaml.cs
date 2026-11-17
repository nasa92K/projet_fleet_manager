using FMprojet.Database;
using FMprojet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FMprojet.Views
{
    /// <summary>
    /// Fenêtre admin affichant les véhicules à remplacer (âge ≥ 4 ans ET km ≥ 80 000).
    /// Accessible uniquement aux rôles admin et super_admin.
    /// </summary>
    public partial class AdminVehiculesRemplacer : Window
    {
        private readonly DatabaseManager _dbManager = new();

        // Liste complète chargée depuis la BDD (référence pour le filtrage)
        private List<Vehicule> _tousLesVehicules = new();

        public AdminVehiculesRemplacer()
        {
            InitializeComponent();

            // Vérification des droits d'accès (double sécurité côté code)
            if (!SessionManager.EstAdmin)
            {
                MessageBox.Show(
                    "⛔  Accès refusé.\n\nCette section est réservée aux administrateurs.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            Loaded += (_, _) => ChargerDonnees();
        }

        // ─── Chargement ───────────────────────────────────────────────────────

        /// <summary>
        /// Charge les véhicules à remplacer depuis la base de données,
        /// met à jour les KPI et rafraîchit le DataGrid.
        /// </summary>
        private void ChargerDonnees()
        {
            try
            {
                _tousLesVehicules = _dbManager.ObtenirVehiculesARemplacer();
                MettreAJourKpi(_tousLesVehicules);
                AppliquerFiltres();
                txtFooter.Text = $"Données chargées le {DateTime.Now:dd/MM/yyyy à HH:mm}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors du chargement des données :\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── KPI ──────────────────────────────────────────────────────────────

        private void MettreAJourKpi(List<Vehicule> vehicules)
        {
            txtKpiTotal.Text   = vehicules.Count.ToString();
            txtKpiKmTotal.Text = vehicules.Sum(v => v.Kilometrage).ToString("N0");

            if (vehicules.Count > 0)
                txtKpiAgeMoyen.Text = vehicules.Average(v => v.AgeCalcule).ToString("0.0");
            else
                txtKpiAgeMoyen.Text = "—";
        }

        // ─── Filtrage & tri ───────────────────────────────────────────────────

        private void AppliquerFiltres()
        {
            var recherche = txtRecherche.Text.Trim().ToLower();

            // Filtre textuel (marque ou modèle)
            IEnumerable<Vehicule> resultats = _tousLesVehicules;
            if (!string.IsNullOrWhiteSpace(recherche))
                resultats = resultats.Where(v =>
                    v.Marque.ToLower().Contains(recherche) ||
                    v.Modele.ToLower().Contains(recherche) ||
                    v.Immatriculation.ToLower().Contains(recherche));

            // Tri
            resultats = (cbTri.SelectedIndex) switch
            {
                0 => resultats.OrderByDescending(v => v.Kilometrage),
                1 => resultats.OrderByDescending(v => v.AgeCalcule),
                2 => resultats.OrderBy(v => v.Marque),
                _ => resultats
            };

            var liste = resultats.ToList();
            dgVehicules.ItemsSource = liste;
            txtFooter.Text = $"{liste.Count} véhicule(s) affiché(s) sur {_tousLesVehicules.Count} à remplacer.";
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e) => AppliquerFiltres();
        private void CbTri_SelectionChanged(object sender, SelectionChangedEventArgs e) => AppliquerFiltres();

        private void BtnActualiser_Click(object sender, RoutedEventArgs e) => ChargerDonnees();

        // ─── Actions sur les véhicules ────────────────────────────────────────

        /// <summary>
        /// Affiche une MessageBox avec les détails complets du véhicule sélectionné.
        /// </summary>
        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not Vehicule v) return;

            string circulationInfo = v.Circulation.HasValue
                ? $"{v.Circulation.Value:dd/MM/yyyy}  (il y a {v.AgeCalcule} ans)"
                : $"Non renseignée  (âge estimé depuis l'année de fabrication : {v.AgeCalcule} ans)";

            MessageBox.Show(
                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                $"  🚨  VÉHICULE À REMPLACER\n" +
                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                $"  🆔  ID                  :  {v.Id}\n" +
                $"  🚗  Marque / Modèle     :  {v.Marque} {v.Modele}\n" +
                $"  🔑  Immatriculation     :  {v.Immatriculation}\n" +
                $"  📅  Année fabrication   :  {v.Annee}\n" +
                $"  🗓️  Date de circulation :  {circulationInfo}\n" +
                $"  🛣️  Kilométrage         :  {v.Kilometrage:N0} km\n" +
                $"  ⛽  Carburant           :  {v.Carburant}\n" +
                $"  ⚙️  Transmission        :  {v.Transmission}\n" +
                $"  💰  Prix                :  {v.Prix:N2} €\n\n" +
                $"  📊  Statut              :  {v.StatutRemplacement}\n\n" +
                $"  ⚠️  Ce véhicule dépasse les 2 critères de remplacement :\n" +
                $"      • Âge ≥ 4 ans  ✗  ({v.AgeCalcule} ans)\n" +
                $"      • Km ≥ 80 000  ✗  ({v.Kilometrage:N0} km)",
                "Détails du véhicule",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        /// <summary>
        /// Supprime le véhicule de la base de données après confirmation.
        /// </summary>
        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not Vehicule v) return;

            var confirm = MessageBox.Show(
                $"⚠️  ATTENTION – Suppression définitive\n\n" +
                $"  Véhicule  :  {v.Marque} {v.Modele}\n" +
                $"  ID        :  {v.Id}\n" +
                $"  Immat.    :  {v.Immatriculation}\n" +
                $"  Km        :  {v.Kilometrage:N0} km\n\n" +
                $"Cette action ne peut pas être annulée !\n" +
                $"Les commandes associées seront également supprimées.",
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                bool succes = _dbManager.SupprimerVehicule(v.Id);
                if (succes)
                {
                    txtFooter.Text = $"🗑️  Véhicule {v.Marque} {v.Modele} (ID {v.Id}) supprimé.";
                    ChargerDonnees(); // Rafraîchir la liste et les KPI
                }
                else
                {
                    MessageBox.Show(
                        "La suppression a échoué. Vérifiez les contraintes de clés étrangères.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la suppression :\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── Navigation ────────────────────────────────────────────────────────

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            var accueil = new Accueil();
            accueil.Show();
            Close();
        }
    }
}
