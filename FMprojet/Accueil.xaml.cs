using FMprojet.Database;
using FMprojet.Models;
using FMprojet;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Linq;
using System.Windows;

namespace FMprojet.Views
{
    public partial class Accueil : Window
    {
        private readonly DatabaseManager _dbManager = new();

        public Accueil()
        {
            InitializeComponent();
            ChargerInformationsUtilisateur();
        }

        private void ChargerInformationsUtilisateur()
        {
            if (SessionManager.UtilisateurActuel != null)
            {
                Utilisateur utilisateur = SessionManager.UtilisateurActuel;

                txtNomUtilisateur.Text = $"{utilisateur.Prenom} {utilisateur.Nom}";

                string role = utilisateur.Role?.ToLower() ?? "utilisateur";
                string roleAffiche;

                switch (role)
                {
                    case "super_admin":
                        roleAffiche = "Super Administrateur";
                        txtRole.Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(142, 68, 173));
                        break;
                    case "admin":
                        roleAffiche = "Administrateur";
                        txtRole.Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(52, 152, 219));
                        break;
                    case "utilisateur":
                    case "client":
                        roleAffiche = "Utilisateur";
                        txtRole.Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(39, 174, 96));
                        break;
                    default:
                        roleAffiche = "Utilisateur";
                        break;
                }

                txtRole.Text = roleAffiche;
                AfficherMenusSelonRole(role);
            }
            else
            {
                var connexion = new Connexion();
                connexion.Show();
                this.Close();
            }
        }

        private void AfficherMenusSelonRole(string role)
        {
            grpSuperAdmin.Visibility = Visibility.Collapsed;
            grpAdmin.Visibility = Visibility.Collapsed;
            grpUtilisateur.Visibility = Visibility.Collapsed;

            switch (role)
            {
                case "super_admin":
                    grpSuperAdmin.Visibility = Visibility.Visible;
                    grpAdmin.Visibility = Visibility.Visible;
                    grpUtilisateur.Visibility = Visibility.Visible;
                    ChargerCompteursDashboard();
                    break;
                case "admin":
                    grpAdmin.Visibility = Visibility.Visible;
                    grpUtilisateur.Visibility = Visibility.Visible;
                    ChargerCompteursDashboard();
                    break;
                case "utilisateur":
                case "client":
                    grpUtilisateur.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void ChargerCompteursDashboard()
        {
            int nbVehicules = 0;
            int nbAttente = 0;

            try
            {
                nbVehicules = _dbManager.CompterVehiculesDisponibles();
                nbAttente   = _dbManager.CompterCommandesEnAttente();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement du dashboard : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            txtNbVehicules.Text           = nbVehicules.ToString();
            txtNbCommandesEnAttente.Text  = nbAttente.ToString();

            ChargerGraphiqueCouts();
            ChargerGraphiqueCategories();
        }

        private void ChargerGraphiqueCouts()
        {
            if (chartCouts == null) return;

            try
            {
                var points = _dbManager.ObtenirCoutsParJourDerniersJours(30);
                if (txtDashChartInfo != null)
                    txtDashChartInfo.Text = points.Count == 0 ? "Aucune donnée de coût (ajoute des événements avec un coût dans le suivi véhicule)." : "";

                var labels = points.Select(p => p.Jour.ToString("dd/MM")).ToArray();
                var valeurs = points.Select(p => (double)p.CoutTotal).ToArray();

                chartCouts.Series = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = valeurs,
                        Name = "Coûts"
                    }
                };

                chartCouts.XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = labels,
                        LabelsRotation = 0,
                        TextSize = 11,
                        SeparatorsPaint = new SolidColorPaint(new SKColor(230, 235, 240))
                    }
                };

                chartCouts.YAxes = new Axis[]
                {
                    new Axis
                    {
                        Labeler = value => value.ToString("0"),
                        TextSize = 11,
                        SeparatorsPaint = new SolidColorPaint(new SKColor(230, 235, 240))
                    }
                };
            }
            catch (System.Exception ex)
            {
                if (txtDashChartInfo != null) txtDashChartInfo.Text = "Erreur lors du chargement du graphique.";
                MessageBox.Show($"Erreur graphique : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerGraphiqueCategories()
        {
            if (chartCategories == null) return;

            try
            {
                var cats = _dbManager.ObtenirDepensesFlotteParCategorie(30);
                if (txtDashCatInfo != null)
                    txtDashCatInfo.Text = cats.All(c => c.Total <= 0) ? "Aucune dépense saisie sur 30 jours (via le suivi journalier véhicule)." : "";

                chartCategories.Series = cats
                    .Where(c => c.Total > 0)
                    .Select(c => new PieSeries<decimal>
                    {
                        Values = new[] { c.Total },
                        Name = c.Categorie,
                        DataLabelsSize = 12,
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle
                    })
                    .Cast<ISeries>()
                    .ToArray();
            }
            catch (System.Exception ex)
            {
                if (txtDashCatInfo != null) txtDashCatInfo.Text = "Erreur lors du chargement des catégories.";
                MessageBox.Show($"Erreur graphique catégories : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeconnexion_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Changer de compte et revenir à l'écran de connexion ?",
                "Changer de compte",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SessionManager.Deconnexion();
                var connexion = new Connexion();
                connexion.Show();
                this.Close();
            }
        }

        // ─── Super Admin ──────────────────────────────────────────────
        private void BtnGererAdministrateurs_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.EstSuperAdmin)
            {
                MessageBox.Show("Accès refusé. Réservé au Super Administrateur.", "Accès refusé",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            MessageBox.Show("Fonctionnalité 'Gérer les administrateurs' — à implémenter.",
                            "Super Admin", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnStatistiques_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.EstSuperAdmin)
            {
                MessageBox.Show("Accès refusé. Réservé au Super Administrateur.", "Accès refusé",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            MessageBox.Show("Fonctionnalité 'Statistiques et rapports' — à implémenter.",
                            "Super Admin", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ─── Admin ────────────────────────────────────────────────────
        private void BtnGererUtilisateurs_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.EstAdmin)
            {
                MessageBox.Show("Accès refusé. Réservé aux administrateurs.", "Accès refusé",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var gestionUtilisateurs = new GestionUtilisateurs();
            gestionUtilisateurs.Show();
            this.Close();
        }

        private void BtnGererVehicules_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.EstAdmin)
            {
                MessageBox.Show("Accès refusé. Réservé aux administrateurs.", "Accès refusé",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var gererVehicules = new GererVehicules();
            gererVehicules.Show();
            this.Close();
        }

        private void BtnGererCommandes_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.EstAdmin)
            {
                MessageBox.Show("Accès refusé. Réservé aux administrateurs.", "Accès refusé",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var gererCommandes = new GererCommandes();
            gererCommandes.Show();
            this.Close();
        }

        // ─── Utilisateur ──────────────────────────────────────────────
        private void BtnVoirVehicules_Click(object sender, RoutedEventArgs e)
        {
            var vehicules = new Vehicules();
            vehicules.Show();
            this.Close();
        }
    }
}