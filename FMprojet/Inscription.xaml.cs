using FMprojet.Database;
using FMprojet.Models;
using FMprojet.Views;
using System.Windows;

namespace FMprojet.Views
{
    public partial class Inscription : Window
    {
        public Inscription()
        {
            InitializeComponent();
        }

        // Méthode pour gérer l'inscription
        private void BtnInscription_Click(object sender, RoutedEventArgs e)
        {
            // Vérifie que tous les champs sont remplis
            if (string.IsNullOrEmpty(txtNom.Text) ||
                string.IsNullOrEmpty(txtPrenom.Text) ||
                string.IsNullOrEmpty(txtEmail.Text) ||
                string.IsNullOrEmpty(txtMotDePasse.Password))
            {
                MessageBox.Show("Veuillez remplir tous les champs.");
                return;
            }

            // Crée un nouvel utilisateur
            var utilisateur = new Utilisateur
            {
                Nom = txtNom.Text,
                Prenom = txtPrenom.Text,
                Email = txtEmail.Text,
                MotDePasse = txtMotDePasse.Password,
                Role = "utilisateur"
            };

            // Enregistre l'utilisateur dans la base de données
            var dbManager = new DatabaseManager();
            bool success = dbManager.InscrireUtilisateur(utilisateur);

            if (success)
            {
                MessageBox.Show("Inscription réussie ! Vous allez être redirigé vers la page de connexion.");

                // Redirige vers la page de connexion
                var connexion = new Connexion();
                connexion.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Erreur lors de l'inscription. Veuillez réessayer.");
            }
        }
    }
}