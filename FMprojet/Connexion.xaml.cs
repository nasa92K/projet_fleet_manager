using FMprojet.Database;
using FMprojet.Models;
using FMprojet;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FMprojet.Views
{
    public partial class Connexion : Window
    {
        private DatabaseManager dbManager;

        public Connexion()
        {
            InitializeComponent();
            dbManager = new DatabaseManager();
            txtEmail.Focus();
        }

        private void BtnConnexion_Click(object sender, RoutedEventArgs e)
        {
            // Validation des champs
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                AfficherErreur("Veuillez saisir votre email.");
                txtEmail.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMotDePasse.Password))
            {
                AfficherErreur("Veuillez saisir votre mot de passe.");
                txtMotDePasse.Focus();
                return;
            }

            // Désactiver le bouton pendant la vérification
            btnConnexion.IsEnabled = false;

            try
            {
                // Vérifier les identifiants
                Utilisateur? utilisateur = dbManager.VerifierConnexion(txtEmail.Text, txtMotDePasse.Password);

                if (utilisateur != null)
                {
                    // Stocker l'utilisateur dans la session
                    SessionManager.UtilisateurActuel = utilisateur;

                    // Mettre à jour la dernière connexion
                    dbManager.MettreAJourDerniereConnexion(utilisateur.Id);

                    // Masquer le message d'erreur
                    MasquerErreur();

                    // Ouvrir la page d'accueil
                    var accueil = new Accueil();
                    accueil.Show();
                    this.Close();
                }
                else
                {
                    AfficherErreur("Email ou mot de passe incorrect.");
                    txtMotDePasse.Clear();
                    txtMotDePasse.Focus();
                }
            }
            catch (Exception ex)
            {
                AfficherErreur($"Erreur lors de la connexion : {ex.Message}");
            }
            finally
            {
                btnConnexion.IsEnabled = true;
            }
        }

        private void LnkInscription_Click(object sender, RoutedEventArgs e)
        {
            var inscription = new Inscription();
            inscription.Show();
            this.Close();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnConnexion_Click(sender, e);
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(52, 152, 219));
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(189, 195, 199));
            }
        }

        private void AfficherErreur(string message)
        {
            txtMessageErreur.Text = message;
            txtMessageErreur.Visibility = Visibility.Visible;
        }

        private void MasquerErreur()
        {
            txtMessageErreur.Visibility = Visibility.Collapsed;
            txtMessageErreur.Text = "";
        }
    }
}
