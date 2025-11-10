using System;
using System.Windows;
using System.Windows.Input;

namespace projet_fleet_manager
{
    public partial class MainWindow : Window
    {
        private readonly string currentUser;

        public MainWindow(string username)
        {
            InitializeComponent();
            currentUser = username;
            txtWelcome.Text = $"Bienvenue, {username}";
            txtStatus.Text = $"Connecté en tant que {username} - {DateTime.Now:dd/MM/yyyy HH:mm}";
        }

        private void Vehicules_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Module Véhicules\n\nFonctionnalité en développement.", "Véhicules", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateStatus("Navigation vers Véhicules");
        }
        private void Utilisateurs_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Module Utilisateurs\n\nFonctionnalité en développement.", "Utilisateurs", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateStatus("Navigation vers Utilisateurs");
        }
        private void Commandes_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Module Commandes\n\nFonctionnalité en développement.", "Commandes", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateStatus("Navigation vers Commandes");
        }
        private void Paiements_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Module Paiements\n\nFonctionnalité en développement.", "Paiements", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateStatus("Navigation vers Paiements");
        }
        private void Categories_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Module Catégories\n\nFonctionnalité en développement.", "Catégories", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateStatus("Navigation vers Catégories");
        }
        private void Images_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Module Images\n\nFonctionnalité en développement.", "Images", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateStatus("Navigation vers Images");
        }
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Êtes-vous sûr de vouloir vous déconnecter?",
                "Déconnexion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
        private void UpdateStatus(string message)
        {
            txtStatus.Text = $"{message} - {DateTime.Now:HH:mm:ss}";
        }
    }
}
