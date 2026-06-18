using FMprojet.Database;
using FMprojet.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FMprojet.Views
{
    public partial class GestionUtilisateurs : Window
    {
        private readonly DatabaseManager _dbManager = new();
        private List<Utilisateur> _tousLesUtilisateurs = new();

        public GestionUtilisateurs()
        {
            InitializeComponent();
            Loaded += (_, _) => ChargerUtilisateurs();
        }

        // ─── Chargement ────────────────────────────────────────────
        private void ChargerUtilisateurs()
        {
            try
            {
                _tousLesUtilisateurs = _dbManager.ObtenirTousLesUtilisateurs();
                AfficherUtilisateurs(_tousLesUtilisateurs);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur chargement utilisateurs : {ex.Message}", "Erreur",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AfficherUtilisateurs(List<Utilisateur> liste)
        {
            listUtilisateurs.ItemsSource = null;
            listUtilisateurs.ItemsSource = liste;
            txtStatut.Text = $"{liste.Count} utilisateur(s) enregistré(s)";
            txtNbUtilisateurs.Text = $"{liste.Count} utilisateur(s) au total";
        }

        // ─── Recherche ──────────────────────────────────────────────
        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            var terme = txtRecherche.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(terme))
            {
                AfficherUtilisateurs(_tousLesUtilisateurs);
                return;
            }

            var filtrés = _tousLesUtilisateurs.Where(u =>
                (u.Nom?.ToLower().Contains(terme) ?? false) ||
                (u.Prenom?.ToLower().Contains(terme) ?? false) ||
                (u.Email?.ToLower().Contains(terme) ?? false) ||
                (u.Role?.ToLower().Contains(terme) ?? false)
            ).ToList();

            AfficherUtilisateurs(filtrés);
        }

        // ─── Sélection ──────────────────────────────────────────────
        private void ListUtilisateurs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listUtilisateurs.SelectedItem is Utilisateur u)
            {
                panneauInfoUser.Visibility = Visibility.Visible;
                txtInfoNom.Text = $"{u.Prenom} {u.Nom}";
                txtInfoEmail.Text = u.Email ?? "—";
                txtInfoRole.Text = u.Role ?? "—";
                btnSupprimerUtilisateur.IsEnabled = true;

                // Couleur badge selon rôle
                txtInfoRole.Foreground = u.Role?.ToLower() switch
                {
                    "super_admin" => new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(155, 114, 242)),
                    "admin" => new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(61, 142, 240)),
                    _ => new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(27, 197, 189))
                };
            }
            else
            {
                panneauInfoUser.Visibility = Visibility.Collapsed;
                btnSupprimerUtilisateur.IsEnabled = false;
            }
        }

        // ─── Ajout ──────────────────────────────────────────────────
        private void BtnAjouterUtilisateur_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNouveauNom.Text) ||
                string.IsNullOrWhiteSpace(txtNouveauPrenom.Text) ||
                string.IsNullOrWhiteSpace(txtNouveauEmail.Text) ||
                string.IsNullOrWhiteSpace(txtNouveauMotDePasse.Password) ||
                cmbNouveauRole.SelectedItem == null)
            {
                MessageBox.Show("Veuillez remplir tous les champs.", "Champs manquants",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var utilisateur = new Utilisateur
            {
                Nom        = txtNouveauNom.Text.Trim(),
                Prenom     = txtNouveauPrenom.Text.Trim(),
                Email      = txtNouveauEmail.Text.Trim(),
                MotDePasse = txtNouveauMotDePasse.Password,
                Role       = (cmbNouveauRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "utilisateur"
            };

            bool success = _dbManager.InscrireUtilisateur(utilisateur);

            if (success)
            {
                txtStatut.Text = $"✅ Utilisateur '{utilisateur.Prenom} {utilisateur.Nom}' ajouté.";
                ChargerUtilisateurs();
                ViderFormulaire();
            }
            else
            {
                MessageBox.Show("Erreur lors de l'ajout de l'utilisateur.", "Erreur",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── Suppression ────────────────────────────────────────────
        private void BtnSupprimerUtilisateur_Click(object sender, RoutedEventArgs e)
        {
            if (listUtilisateurs.SelectedItem is not Utilisateur u) return;

            // Protection sur le compte courant
            if (SessionManager.UtilisateurActuel?.Id == u.Id)
            {
                MessageBox.Show("Vous ne pouvez pas supprimer votre propre compte.", "Impossible",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Supprimer l'utilisateur {u.Prenom} {u.Nom} ({u.Email}) ?",
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                bool ok = _dbManager.SupprimerUtilisateur(u.Id);
                if (ok)
                {
                    txtStatut.Text = $"🗑 Utilisateur '{u.Prenom} {u.Nom}' supprimé.";
                    panneauInfoUser.Visibility = Visibility.Collapsed;
                    btnSupprimerUtilisateur.IsEnabled = false;
                    ChargerUtilisateurs();
                }
                else
                {
                    MessageBox.Show("Échec de la suppression.", "Erreur",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── Utilitaires ────────────────────────────────────────────
        private void ViderFormulaire()
        {
            txtNouveauNom.Clear();
            txtNouveauPrenom.Clear();
            txtNouveauEmail.Clear();
            txtNouveauMotDePasse.Clear();
            cmbNouveauRole.SelectedIndex = -1;
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            var accueil = new Accueil();
            accueil.Show();
            Close();
        }
    }
}