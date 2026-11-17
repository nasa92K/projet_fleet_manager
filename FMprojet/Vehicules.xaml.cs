using FMprojet.Database;
using FMprojet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FMprojet.Views
{
    /// <summary>
    /// Fenêtre affichant le catalogue des véhicules.
    /// Permet aux clients de rechercher un véhicule et de passer une commande.
    /// </summary>
    public partial class Vehicules : Window
    {
        // ──────────────────────────────────────────────────────────
        //  Champs privés
        // ──────────────────────────────────────────────────────────

        private readonly DatabaseManager _dbManager = new();

        /// <summary>Véhicule actuellement sélectionné dans le DataGrid.</summary>
        private Vehicule? _vehiculeSelectionne;

        // ──────────────────────────────────────────────────────────
        //  Constructeur
        // ──────────────────────────────────────────────────────────

        public Vehicules()
        {
            InitializeComponent();

            // Chargement initial au démarrage de la fenêtre
            Loaded += (_, _) => Initialiser();
        }

        // ──────────────────────────────────────────────────────────
        //  Initialisation
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// Configure l'interface selon le rôle de l'utilisateur connecté,
        /// puis charge tous les véhicules.
        /// </summary>
        private void Initialiser()
        {
            var utilisateur = SessionManager.UtilisateurActuel;

            if (utilisateur != null)
            {
                txtUtilisateurInfo.Text = $"Connecté en tant que : {utilisateur.Prenom} {utilisateur.Nom}  ({utilisateur.Role})";

                bool estClient = utilisateur.Role.Equals("client", StringComparison.OrdinalIgnoreCase) 
                              || utilisateur.Role.Equals("utilisateur", StringComparison.OrdinalIgnoreCase);
                if (estClient)
                {
                    // Panneau commander + aperçu commandes
                    panneauCommander.Visibility    = Visibility.Visible;
                    panneauMesCommandes.Visibility = Visibility.Visible;
                    // Bouton "Mes Commandes" dans l'en-tête
                    btnMesCommandes.Visibility     = Visibility.Visible;
                    colActionCommander.Visibility  = Visibility.Visible;
                    ChargerMesCommandes();
                }
                else
                {
                    colActionCommander.Visibility  = Visibility.Collapsed;
                }
            }
            else
            {
                txtUtilisateurInfo.Text = "Non connecté";
            }

            // Charger uniquement les véhicules commandables (< 4 ans ET < 80 000 km)
            ChargerVehicules();
        }

        // ──────────────────────────────────────────────────────────
        //  Chargement des données
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// Charge uniquement les véhicules commandables :
        /// âge &lt; 4 ans ET kilométrage &lt; 80 000 km.
        /// Le filtre est appliqué côté client (via AgeCalcule) pour utiliser
        /// la date de circulation si disponible.
        /// </summary>
        private void ChargerVehicules()
        {
            var tous = _dbManager.ObtenirTousLesVehicules();
            // Affiche tous les véhicules sans filtrer, mais les boutons "Commander" seront grisés si non commandables
            ActualiserListeVehicules(tous);
        }

        /// <summary>Charge les commandes de l'utilisateur connecté.</summary>
        private void ChargerMesCommandes()
        {
            var utilisateur = SessionManager.UtilisateurActuel;
            if (utilisateur == null) return;

            var commandes = _dbManager.ObtenirCommandesParUtilisateur(utilisateur.Id);
            dgMesCommandes.ItemsSource = commandes;
        }

        /// <summary>Met à jour le DataGrid avec la liste fournie et actualise le compteur.</summary>
        private void ActualiserListeVehicules(List<Vehicule> vehicules)
        {
            dgVehicules.ItemsSource = vehicules;

            // Mise à jour du compteur affiché
            txtCompteurVehicules.Text = vehicules.Count == 0
                ? "Aucun résultat"
                : $"{vehicules.Count} véhicule(s)";

            // Réinitialiser la sélection et le bouton Commander
            _vehiculeSelectionne       = null;
            btnCommander.IsEnabled     = false;
            txtVehiculeSelectionne.Text = "—";
        }

        // ──────────────────────────────────────────────────────────
        //  Gestion du placeholder de la zone de recherche
        // ──────────────────────────────────────────────────────────

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Affiche/masque le placeholder selon le contenu du TextBox
            txtReference.TextChanged += (_, _) =>
            {
                txtPlaceholder.Visibility = string.IsNullOrEmpty(txtReference.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };
        }

        // ──────────────────────────────────────────────────────────
        //  Événements – Recherche
        // ──────────────────────────────────────────────────────────

        private void BtnRechercher_Click(object sender, RoutedEventArgs e)
        {
            var reference = txtReference.Text?.Trim();

            if (string.IsNullOrWhiteSpace(reference))
            {
                ChargerVehicules();
                return;
            }

            // Rechercher sur l'ensemble des véhicules
            var resultats = _dbManager.RechercherVehiculesParReference(reference).ToList();
            ActualiserListeVehicules(resultats);
        }

        /// <summary>Lance la recherche en appuyant sur Entrée dans le TextBox.</summary>
        private void TxtReference_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnRechercher_Click(sender, e);
        }

        /// <summary>Réinitialise la recherche et affiche tous les véhicules.</summary>
        private void BtnTout_Click(object sender, RoutedEventArgs e)
        {
            txtReference.Clear();
            ChargerVehicules();
        }

        // ──────────────────────────────────────────────────────────
        //  Événements – Sélection dans le DataGrid
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// Met à jour le véhicule sélectionné et active/désactive le bouton Commander
        /// selon si un véhicule est sélectionné et si l'utilisateur est un client.
        /// </summary>
        private void DgVehicules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _vehiculeSelectionne = dgVehicules.SelectedItem as Vehicule;

            var utilisateur = SessionManager.UtilisateurActuel;
            bool estClient = false;
            if (utilisateur != null)
            {
                estClient = utilisateur.Role.Equals("client", StringComparison.OrdinalIgnoreCase) 
                         || utilisateur.Role.Equals("utilisateur", StringComparison.OrdinalIgnoreCase);
            }

            if (_vehiculeSelectionne != null && estClient)
            {
                // Affichage du libellé du véhicule sélectionné
                txtVehiculeSelectionne.Text =
                    $"{_vehiculeSelectionne.Marque} {_vehiculeSelectionne.Modele}  –  {_vehiculeSelectionne.Prix:N2} €";
                btnCommander.IsEnabled = _vehiculeSelectionne.EstCommandable;
            }
            else
            {
                txtVehiculeSelectionne.Text = "—";
                btnCommander.IsEnabled      = false;
            }
        }

        // ──────────────────────────────────────────────────────────
        //  Événements – Commander un véhicule
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// Crée une commande en base de données pour le véhicule sélectionné.
        /// Vérifie la session, applique les règles métiers (âge et kilométrage),
        /// demande confirmation, puis appelle AjouterCommande().
        /// 
        /// Règles métiers obligatoires :
        ///   • Âge du véhicule     : DateTime.Now.Year - Annee  &lt; 4 ans
        ///   • Kilométrage maximum : Kilometrage                &lt; 80 000 km
        /// </summary>
        private void BtnCommander_Click(object sender, RoutedEventArgs e)
        {
            // 1. Vérification : un véhicule doit être sélectionné
            if (_vehiculeSelectionne == null)
            {
                AfficherMessageErreur("Veuillez sélectionner un véhicule dans la liste.");
                return;
            }

            // 2. Vérification : l'utilisateur doit être connecté
            var utilisateur = SessionManager.UtilisateurActuel;
            if (utilisateur == null)
            {
                AfficherMessageErreur(
                    "Vous devez être connecté pour passer une commande.\n" +
                    "Veuillez vous reconnecter.");
                return;
            }

            // ── 3. VALIDATION DES RÈGLES MÉTIERS ─────────────────────────────────
            //
            //  Un client ne peut commander un véhicule que si :
            //    • Son âge réel est inférieur à 4 ans
            //      (calculé depuis la date de circulation si renseignée, sinon depuis l'année de fabrication)
            //    • Son kilométrage est inférieur à 80 000 km
            //
            //  Les deux conditions doivent être vraies simultanément.
            // ─────────────────────────────────────────────────────────────────────

            // AgeCalcule utilise Vehicule.Circulation en priorité (cf. Vehicule.cs)
            int  ageVehicule       = _vehiculeSelectionne.AgeCalcule;
            bool ageValide         = ageVehicule        < 4;
            bool kilometrageValide = _vehiculeSelectionne.Kilometrage < 80_000;

            if (!ageValide || !kilometrageValide)
            {
                // Construction d'un message d'erreur précis et pédagogique
                var lignesErreur = new System.Text.StringBuilder();
                lignesErreur.AppendLine("⛔  Vous ne pouvez pas acheter cette voiture :");
                lignesErreur.AppendLine();

                if (!ageValide)
                {
                    // Préciser si l'âge vient de la date de circulation ou de l'année de fabrication
                    string sourceAge = _vehiculeSelectionne.Circulation.HasValue
                        ? $"(date de mise en circulation : {_vehiculeSelectionne.Circulation.Value:dd/MM/yyyy})"
                        : $"(année de fabrication : {_vehiculeSelectionne.Annee})";

                    lignesErreur.AppendLine(
                        $"  ❌  Âge trop élevé : {ageVehicule} an(s) {sourceAge}\n" +
                        $"       Maximum autorisé : 3 ans (mise en service au plus tôt le {DateTime.Today.AddYears(-3):dd/MM/yyyy})");
                }

                if (!kilometrageValide)
                {
                    lignesErreur.AppendLine(
                        $"  ❌  Kilométrage trop élevé : {_vehiculeSelectionne.Kilometrage:N0} km\n" +
                        $"       Maximum autorisé : 79 999 km");
                }

                lignesErreur.AppendLine();
                lignesErreur.AppendLine("La commande a été bloquée. Veuillez sélectionner un autre véhicule.");

                MessageBox.Show(
                    lignesErreur.ToString(),
                    "FleetManager – Commande refusée",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);

                // Aucune insertion en base → on sort immédiatement
                return;
            }

            // 4. Boîte de dialogue de confirmation
            // Les règles métiers sont validées → on affiche les infos du véhicule avec l'âge calculé
            var result = AfficherConfirmation(
                "Confirmer la commande",
                $"Vous êtes sur le point de commander :\n\n" +
                $"  🚗  {_vehiculeSelectionne.Marque} {_vehiculeSelectionne.Modele}\n" +
                $"  📅  Année : {_vehiculeSelectionne.Annee}  (âge : {ageVehicule} an(s))\n" +
                $"  🛣️  Kilométrage : {_vehiculeSelectionne.Kilometrage:N0} km\n" +
                $"  💰  Prix : {_vehiculeSelectionne.Prix:N2} €\n" +
                $"  ⛽  Carburant : {_vehiculeSelectionne.Carburant}\n\n" +
                $"✅  Ce véhicule respecte tous les critères de commande.\n" +
                $"Voulez-vous confirmer cette commande ?");

            if (result != MessageBoxResult.Yes)
                return; // L'utilisateur a annulé

            // Construction de l'objet Commande (les propriétés correspondent exactement au modèle)
            var nouvelleCommande = new Commande
            {
                UtilisateurId = utilisateur.Id,
                VehiculeId    = _vehiculeSelectionne.Id,
                Statut        = StatutCommande.EnAttente,    // Statut initial conformément à l'ENUM MySQL
                DateCommande  = DateTime.Now
            };

            // Désactiver temporairement le bouton pour éviter les doubles clics
            btnCommander.IsEnabled = false;

            // Appel à la méthode existante du DatabaseManager
            bool succes = _dbManager.AjouterCommande(nouvelleCommande);

            if (succes)
            {
                // Succès : affichage du message de confirmation
                AfficherMessageSucces(
                    "✅  Commande enregistrée !",
                    $"Votre commande pour le véhicule\n" +
                    $"\"{_vehiculeSelectionne.Marque} {_vehiculeSelectionne.Modele}\"\n" +
                    $"a bien été enregistrée avec le statut « En attente ».\n\n" +
                    $"Notre équipe reviendra vers vous prochainement.");

                // Rafraîchir le panneau "Mes Commandes"
                ChargerMesCommandes();

                // Réinitialiser la sélection
                dgVehicules.UnselectAll();
            }
            else
            {
                // Échec : ré-activer le bouton pour permettre une nouvelle tentative
                btnCommander.IsEnabled = true;
            }
        }

        /// <summary>Gère le clic sur le bouton "Commander" directement dans le DataGrid.</summary>
        private void BtnCommanderDirect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Vehicule vehicule)
            {
                _vehiculeSelectionne = vehicule;
                dgVehicules.SelectedItem = vehicule;
                BtnCommander_Click(sender, e);
            }
        }

        /// <summary>Rafraîchit manuellement le panneau "Mes Commandes".</summary>
        private void BtnRafraichirCommandes_Click(object sender, RoutedEventArgs e)
        {
            ChargerMesCommandes();
        }

        // ──────────────────────────────────────────────────────────
        //  Navigation
        // ──────────────────────────────────────────────────────────

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            var accueil = new Accueil();
            accueil.Show();
            Close();
        }

        /// <summary>
        /// Ouvre la page dédiée de suivi des commandes du client.
        /// </summary>
        private void BtnMesCommandes_Click(object sender, RoutedEventArgs e)
        {
            var page = new SuiviCommandes();
            page.Show();
            Close();
        }

        // ──────────────────────────────────────────────────────────
        //  Helpers – Messages propres
        // ──────────────────────────────────────────────────────────

        /// <summary>Affiche une boîte de dialogue d'erreur stylée.</summary>
        private static void AfficherMessageErreur(string message)
        {
            MessageBox.Show(
                message,
                "FleetManager – Attention",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        /// <summary>Affiche une boîte de dialogue de succès stylée.</summary>
        private static void AfficherMessageSucces(string titre, string message)
        {
            MessageBox.Show(
                message,
                titre,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// Affiche une boîte de dialogue de confirmation (Oui / Non) et retourne le résultat.
        /// </summary>
        private static MessageBoxResult AfficherConfirmation(string titre, string message)
        {
            return MessageBox.Show(
                message,
                titre,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
        }
    }
}
