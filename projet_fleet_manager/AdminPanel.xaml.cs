using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;
using projet_fleet_manager;

namespace projet_fleet_manager
{
    public partial class AdminPanel : Window
    {
        private string connectionString;

        public AdminPanel(string connStr)
        {
            InitializeComponent();
            connectionString = connStr;

            // Affiche le nom de l'admin connecté
            AdminNameText.Text = $"Connecté : {CurrentUser.Prenom} {CurrentUser.Nom}";

            LoadUsers();
        }

        // 🔹 Charge tous les utilisateurs
        private void LoadUsers()
        {
            try
            {
                var users = new List<UserInfo>();

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, nom, prenom, email, role, statut FROM utilisateurs ORDER BY id";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new UserInfo
                            {
                                Id = reader.GetInt32("id"),
                                Nom = reader.GetString("nom"),
                                Prenom = reader.GetString("prenom"),
                                Email = reader.GetString("email"),
                                Role = reader.GetString("role"),
                                Statut = reader.GetString("statut")
                            });
                        }
                    }
                }

                UsersDataGrid.ItemsSource = users;
                UpdateStatistics(users);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}", "Erreur");
            }
        }

        // 🔹 Met à jour les statistiques
        private void UpdateStatistics(List<UserInfo> users)
        {
            TotalUsersText.Text = users.Count.ToString();

            int activeClients = 0;
            int blockedUsers = 0;

            foreach (var user in users)
            {
                if (user.Role.ToLower() == "client" && user.Statut == "actif")
                    activeClients++;

                if (user.Statut == "bloque")
                    blockedUsers++;
            }

            ActiveClientsText.Text = activeClients.ToString();
            BlockedUsersText.Text = blockedUsers.ToString();
        }

        // 🔹 Promouvoir un utilisateur en admin
        private void PromoteToAdmin_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            int userId = (int)button.Tag;

            var result = MessageBox.Show(
                "Promouvoir cet utilisateur en administrateur ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                UpdateUserRole(userId, "admin");
            }
        }

        // 🔹 Rétrograder un admin en client
        private void DemoteToClient_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            int userId = (int)button.Tag;

            if (userId == CurrentUser.Id)
            {
                MessageBox.Show("Tu ne peux pas te rétrograder toi-même !", "Attention");
                return;
            }

            var result = MessageBox.Show(
                "Rétrograder cet administrateur en client ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                UpdateUserRole(userId, "client");
            }
        }

        // 🔹 Bloquer un utilisateur
        private void BlockUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            int userId = (int)button.Tag;

            if (userId == CurrentUser.Id)
            {
                MessageBox.Show("Tu ne peux pas te bloquer toi-même !", "Attention");
                return;
            }

            var result = MessageBox.Show(
                "Bloquer cet utilisateur ? Il ne pourra plus se connecter.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                UpdateUserStatus(userId, "bloque");
            }
        }

        // 🔹 Débloquer un utilisateur
        private void UnblockUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            int userId = (int)button.Tag;

            UpdateUserStatus(userId, "actif");
        }

        // 🔹 Met à jour le rôle d'un utilisateur
        private void UpdateUserRole(int userId, string newRole)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE utilisateurs SET role = @Role WHERE id = @Id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Role", newRole);
                        cmd.Parameters.AddWithValue("@Id", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"✅ Rôle modifié en '{newRole}' avec succès !", "Succès");
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur");
            }
        }

        // 🔹 Met à jour le statut d'un utilisateur
        private void UpdateUserStatus(int userId, string newStatus)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE utilisateurs SET statut = @Statut WHERE id = @Id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Statut", newStatus);
                        cmd.Parameters.AddWithValue("@Id", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                string message = newStatus == "bloque" ? "🔒 Utilisateur bloqué" : "✅ Utilisateur débloqué";
                MessageBox.Show(message, "Succès");
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur");
            }
        }

        // 🔹 Actualiser la liste
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        // 🔹 Voir la boutique (même interface que les clients)
        private void ViewShop_Click(object sender, RoutedEventArgs e)
        {
            ShopWindow shop = new ShopWindow(connectionString);
            shop.Show();
        }

        // 🔹 Déconnexion
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow login = new MainWindow();
            login.Show();
            this.Close();
        }
    }

    // 🔹 Classe pour représenter un utilisateur
    public class UserInfo
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Statut { get; set; }
    }
}