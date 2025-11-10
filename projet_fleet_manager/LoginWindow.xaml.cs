using System;
using System.Windows;
using System.Windows.Input;
using MySql.Data.MySqlClient;

namespace projet_fleet_manager
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            // Permettre la connexion avec la touche Entrée
            txtPassword.KeyDown += TxtPassword_KeyDown;
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(btnLogin, null);
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Réinitialiser le message d'erreur
            txtError.Visibility = Visibility.Collapsed;
            txtError.Text = "";

            // Validation des champs
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username))
            {
                ShowError("Veuillez saisir un nom d'utilisateur.");
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Veuillez saisir un mot de passe.");
                txtPassword.Focus();
                return;
            }

            // Désactiver le bouton pendant la connexion
            btnLogin.IsEnabled = false;
            btnLogin.Content = "Connexion...";

            try
            {
                // Vérifier les identifiants dans la base de données
                if (VerifyCredentials(username, password))
                {
                    // Connexion réussie - Ouvrir la fenêtre principale
                    MainWindow mainWindow = new MainWindow(username);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    ShowError("Nom d'utilisateur ou mot de passe incorrect.");
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowError("Erreur de connexion : " + ex.Message);
            }
            finally
            {
                // Réactiver le bouton
                btnLogin.IsEnabled = true;
                btnLogin.Content = "Se connecter";
            }
        }

        private bool VerifyCredentials(string username, string password)
        {
            using (MySqlConnection connection = DatabaseConnection.GetConnection())
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM utilisateurs WHERE nom_utilisateur = @username AND mot_de_passe = @password";
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}
