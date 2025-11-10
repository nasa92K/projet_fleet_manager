using MySql.Data.MySqlClient;

namespace projet_fleet_manager
{
    // Classe statique pour la connexion à la base MySQL
    public static class DatabaseConnection
    {
        // Méthode pour obtenir la connexion
        public static MySqlConnection GetConnection()
        {
            // Remplace TON_MOT_DE_PASSE par ton mot de passe MySQL.
            // SslMode=none évite les soucis en local pour la dev.
            string connectionString = "server=localhost;user id=root;password=TON_MOT_DE_PASSE;database=projet_fleet_manager;SslMode=none;";
            return new MySqlConnection(connectionString);
        }
    }
}
