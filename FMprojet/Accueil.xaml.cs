using FMprojet.Database;
using FMprojet.Models;
using FMprojet;
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
            int nbVehicules = _dbManager.CompterVehiculesDisponibles();
            int nbAttente   = _dbManager.CompterCommandesEnAttente();

            txtNbVehicules.Text           = nbVehicules.ToString();
            txtNbCommandesEnAttente.Text  = nbAttente.ToString();
        }

        private void BtnDeconnexion_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Êtes-vous sûr de vouloir vous déconnecter ?",
                "Déconnexion",
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

        private void BtnMesCommandes_Click(object sender, RoutedEventArgs e)
        {
            var mesCommandes = new MesCommandes();
            mesCommandes.Show();
            this.Close();
        }
    }
}