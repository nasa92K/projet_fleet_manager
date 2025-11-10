using System.Windows;

namespace projet_fleet_manager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Tester la connexion à la base de données au démarrage
            if (!DatabaseConnection.TestConnection())
            {
                MessageBox.Show(
                    "Impossible de se connecter à la base de données.\n\n" +
                    "Vérifiez que:\n" +
                    "- MySQL est démarré\n" +
                    "- Les paramètres de connexion sont corrects dans DatabaseConnection.cs\n" +
                    "- La base de données 'projet_fleet_manager' existe\n\n" +
                    DatabaseConnection.GetConnectionTestMessage(),
                    "Erreur de connexion",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Fermer l'application
                Shutdown();
                return;
            }

            // Ouvrir la fenêtre de connexion
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}