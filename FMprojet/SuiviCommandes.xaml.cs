using FMprojet.Database;
using FMprojet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FMprojet.Views
{
    /// <summary>
    /// Page dédiée au suivi des commandes du client connecté.
    /// Affiche les KPI, l'historique complet filtrable, et une timeline
    /// de progression pour la commande sélectionnée.
    /// </summary>
    public partial class SuiviCommandes : Window
    {
        private readonly DatabaseManager _dbManager = new();
        private List<CommandeVue> _toutesLesCommandes = new();

        public SuiviCommandes()
        {
            InitializeComponent();

            // Vérification : seuls les utilisateurs connectés peuvent accéder
            if (SessionManager.UtilisateurActuel == null)
            {
                MessageBox.Show("Vous devez être connecté pour consulter vos commandes.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            Loaded += (_, _) => Initialiser();
        }

        // ─── Initialisation ───────────────────────────────────────────────────

        private void Initialiser()
        {
            var u = SessionManager.UtilisateurActuel!;
            txtSousTitre.Text = $"Connecté en tant que : {u.Prenom} {u.Nom}";
            ChargerCommandes();
        }

        // ─── Chargement & affichage ───────────────────────────────────────────

        private void ChargerCommandes()
        {
            var u = SessionManager.UtilisateurActuel;
            if (u == null) return;

            try
            {
                var commandes = _dbManager.ObtenirCommandesParUtilisateur(u.Id);

                // Enrichissement du modèle pour l'affichage
                _toutesLesCommandes = commandes.Select(c => new CommandeVue(c)).ToList();

                MettreAJourKpi(_toutesLesCommandes);
                AppliquerFiltre();

                txtFooter.Text = $"Données chargées le {DateTime.Now:dd/MM/yyyy à HH:mm}  •  {_toutesLesCommandes.Count} commande(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des commandes :\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── KPI ──────────────────────────────────────────────────────────────

        private void MettreAJourKpi(List<CommandeVue> commandes)
        {
            txtKpiTotal.Text      = commandes.Count.ToString();
            txtKpiAttente.Text    = commandes.Count(c => c.StatutNormalise == "en_attente").ToString();
            txtKpiConfirmees.Text = commandes.Count(c => c.StatutNormalise == "confirmee").ToString();
            txtKpiAnnulees.Text   = commandes.Count(c => c.StatutNormalise == "annulee").ToString();
        }

        // ─── Filtre par statut ────────────────────────────────────────────────

        private void AppliquerFiltre()
        {
            var filtre = (cbFiltre.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tous les statuts";

            var liste = filtre == "Tous les statuts"
                ? _toutesLesCommandes
                : _toutesLesCommandes
                    .Where(c => StatutCommande.Libelle(c.StatutNormalise).Equals(filtre, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            dgCommandes.ItemsSource = liste;
            txtFooter.Text = $"{liste.Count} commande(s) affichée(s) sur {_toutesLesCommandes.Count}.";
        }

        private void CbFiltre_SelectionChanged(object sender, SelectionChangedEventArgs e) => AppliquerFiltre();
        private void BtnActualiser_Click(object sender, RoutedEventArgs e) => ChargerCommandes();

        // ─── Détail d'une commande sélectionnée ──────────────────────────────

        private void DgCommandes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCommandes.SelectedItem is not CommandeVue cv)
            {
                panneauDetail.Visibility = Visibility.Collapsed;
                return;
            }

            panneauDetail.Visibility = Visibility.Visible;

            // Titre et date
            txtDetailNom.Text  = $"Commande #{cv.Id}  –  {cv.VehiculeMarqueModele}";
            txtDetailDate.Text = $"Passée le {cv.DateCommande:dddd dd MMMM yyyy à HH:mm}";

            // Badge statut
            switch (cv.StatutNormalise)
            {
                case "en_attente":
                    txtDetailStatut.Text       = "⏳ En attente";
                    txtDetailStatut.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    bdDetailStatut.Background  = new SolidColorBrush(Color.FromRgb(58, 42, 16));
                    MettreAJourTimeline(etape: 1, annulee: false);
                    txtDetailMessage.Text = "Votre commande a bien été reçue et est en cours d'examen par notre équipe. Vous serez contacté prochainement.";
                    break;
                case "confirmee":
                    txtDetailStatut.Text       = "✅ Confirmée";
                    txtDetailStatut.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    bdDetailStatut.Background  = new SolidColorBrush(Color.FromRgb(13, 46, 26));
                    MettreAJourTimeline(etape: 3, annulee: false);
                    txtDetailMessage.Text = "Votre commande est confirmée ! Notre équipe va vous contacter pour finaliser la livraison du véhicule.";
                    break;
                case "annulee":
                    txtDetailStatut.Text       = "❌ Annulée";
                    txtDetailStatut.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    bdDetailStatut.Background  = new SolidColorBrush(Color.FromRgb(61, 16, 16));
                    MettreAJourTimeline(etape: 2, annulee: true);
                    txtDetailMessage.Text = "Cette commande a été annulée. Contactez notre équipe si vous souhaitez plus d'informations.";
                    break;
                default:
                    txtDetailStatut.Text       = cv.StatutAffiche;
                    txtDetailStatut.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                    bdDetailStatut.Background  = new SolidColorBrush(Color.FromRgb(51, 65, 85));
                    MettreAJourTimeline(etape: 1, annulee: false);
                    txtDetailMessage.Text = "Statut en cours de mise à jour.";
                    break;
            }
        }

        /// <summary>
        /// Met à jour les couleurs de la timeline selon l'étape atteinte.
        /// etape = 1 (reçue), 2 (traitement), 3 (confirmée/terminée)
        /// annulee = true : étape 3 en rouge (annulation)
        /// </summary>
        private void MettreAJourTimeline(int etape, bool annulee)
        {
            Color vert    = Color.FromRgb(16, 185, 129);
            Color orange  = Color.FromRgb(245, 158, 11);
            Color rouge   = Color.FromRgb(239, 68, 68);
            Color gris    = Color.FromRgb(51, 65, 85);
            Color texteOk = Color.FromRgb(16, 185, 129);
            Color texteKo = Color.FromRgb(100, 116, 139);

            // Étape 1 : toujours verte (commande reçue)
            brushEtape2.Color    = etape >= 2 ? (annulee && etape == 2 ? rouge : orange) : gris;
            brushEtape2Text.Color = etape >= 2 ? (annulee && etape == 2 ? rouge : orange) : texteKo;
            brushLigne1.Color    = etape >= 2 ? vert : gris;

            brushEtape3.Color    = etape >= 3 ? (annulee ? rouge : vert) : gris;
            brushEtape3Text.Color = etape >= 3 ? (annulee ? rouge : vert) : texteKo;
            brushLigne2.Color    = etape >= 3 ? (annulee ? rouge : vert) : gris;

            txtEtape3Label.Text  = annulee ? "Annulée" : "Confirmée";
            txtEtape3Icone.Text  = annulee ? "✕" : "✓";
            txtEtape2Label.Text  = etape >= 2 ? "En traitement" : "En traitement";
        }

        // ─── Navigation ───────────────────────────────────────────────────────

        private void BtnCommander_Click(object sender, RoutedEventArgs e)
        {
            var page = new Vehicules();
            page.Show();
            Close();
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            var accueil = new Accueil();
            accueil.Show();
            Close();
        }
    }

    // ─── ViewModel léger ─────────────────────────────────────────────────────

    /// <summary>
    /// Enrichit le modèle Commande avec des propriétés d'affichage pour le DataGrid.
    /// </summary>
    public class CommandeVue
    {
        public int Id { get; }
        public int UtilisateurId { get; }
        public int VehiculeId { get; }
        public string VehiculeMarqueModele { get; }
        public string UtilisateurNom { get; }
        public DateTime DateCommande { get; }

        /// <summary>Statut brut venant de la BDD (ex: "en_attente", "confirmee", "annulee").</summary>
        public string Statut { get; }

        /// <summary>Statut normalisé en minuscule sans accents pour les comparaisons.</summary>
        public string StatutNormalise { get; }

        /// <summary>Statut formaté pour l'affichage (avec icône et majuscules).</summary>
        public string StatutAffiche { get; }

        public CommandeVue(Commande c)
        {
            Id                  = c.Id;
            UtilisateurId       = c.UtilisateurId;
            VehiculeId          = c.VehiculeId;
            VehiculeMarqueModele = c.VehiculeMarqueModele;
            UtilisateurNom      = c.UtilisateurNom;
            DateCommande        = c.DateCommande;
            Statut              = c.Statut;

            // Normalisation pour comparaison
            StatutNormalise = c.Statut.ToLower() switch
            {
                "en_attente" or "en attente" => "en_attente",
                "confirmee" or "confirmée"   => "confirmee",
                "annulee" or "annulée"       => "annulee",
                _                            => c.Statut.ToLower()
            };

            // Affichage lisible
            StatutAffiche = StatutNormalise switch
            {
                "en_attente" => "⏳ En attente",
                "confirmee"  => "✅ Confirmée",
                "annulee"    => "❌ Annulée",
                _            => c.Statut
            };
        }
    }
}
