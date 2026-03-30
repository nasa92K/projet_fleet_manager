using FMprojet.Database;
using FMprojet.Models;
using System.Linq;
using System.Windows;

namespace FMprojet.Views
{
    public partial class GestionUtilisateurs : Window
    {
        private DatabaseManager _dbManager;

        public GestionUtilisateurs()
        {
            InitializeComponent();
            _dbManager = new DatabaseManager();
            ChargerUtilisateurs();
        }

        // Charge la liste des utilisateurs
        private void ChargerUtilisateurs()
        {
            var utilisateurs = _dbManager.ObtenirTousLesUtilisateurs();
            listUtilisateurs.ItemsSource = utilisateurs;
        }

        // Ajoute un nouvel utilisateur
        private void BtnAjouterUtilisateur_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtNouveauNom.Text) ||
                string.IsNullOrEmpty(txtNouveauPrenom.Text) ||
                string.IsNullOrEmpty(txtNouveauEmail.Text) ||
                string.IsNullOrEmpty(txtNouveauMotDePasse.Password) ||
                cmbNouveauRole.SelectedItem == null)
            {
                MessageBox.Show("Veuillez remplir tous les champs.");
                return;
            }

            var utilisateur = new Utilisateur
            {
                Nom = txtNouveauNom.Text,
                Prenom = txtNouveauPrenom.Text,
                Email = txtNouveauEmail.Text,
                MotDePasse = txtNouveauMotDePasse.Password,
                Role = (cmbNouveauRole.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "utilisateur"
            };

            bool success = _dbManager.InscrireUtilisateur(utilisateur);

            if (success)
            {
                MessageBox.Show("Utilisateur ajouté avec succès !");
                ChargerUtilisateurs();
                // Réinitialise les champs
                txtNouveauNom.Clear();
                txtNouveauPrenom.Clear();
                txtNouveauEmail.Clear();
                txtNouveauMotDePasse.Clear();
                cmbNouveauRole.SelectedIndex = -1;
            }
            else
            {
                MessageBox.Show("Erreur lors de l'ajout de l'utilisateur.");
            }
        }

        // Retour à l'accueil
        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            var accueil = new Accueil();
            accueil.Show();
            Close();
        }
    }
}