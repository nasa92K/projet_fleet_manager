using MySql.Data.MySqlClient;
using FMprojet.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using FMprojet.Views;

namespace FMprojet.Database
{
    public class DatabaseManager
    {
        private string connectionString = "Server=localhost;Database=projet_fleet_manager;Uid=anas;Pwd=anasdevelopper;";

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }

        // ======================
        // Méthodes pour les utilisateurs
        // ======================
        public bool InscrireUtilisateur(Utilisateur utilisateur)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "INSERT INTO utilisateurs (nom, prenom, email, mot_de_passe, role) VALUES (@nom, @prenom, @email, @motDePasse, @role)";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@nom", utilisateur.Nom);
                    cmd.Parameters.AddWithValue("@prenom", utilisateur.Prenom);
                    cmd.Parameters.AddWithValue("@email", utilisateur.Email);
                    cmd.Parameters.AddWithValue("@motDePasse", utilisateur.MotDePasse);
                    cmd.Parameters.AddWithValue("@role", utilisateur.Role);
                    int result = cmd.ExecuteNonQuery();
                    return result > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'inscription : {ex.Message}");
                    return false;
                }
            }
        }

        public Utilisateur? VerifierConnexion(string email, string motDePasse)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT * FROM utilisateurs WHERE email = @email AND mot_de_passe = @motDePasse";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@motDePasse", motDePasse);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Utilisateur
                            {
                                Id = reader.GetInt32("id"),
                                Nom = reader.GetString("nom"),
                                Prenom = reader.GetString("prenom"),
                                Email = reader.GetString("email"),
                                Role = reader.GetString("role")
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la vérification de la connexion : {ex.Message}");
                }
            }
            return null;
        }

        public bool MettreAJourDerniereConnexion(int utilisateurId)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "UPDATE utilisateurs SET derniere_connexion = @dateConnexion WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@dateConnexion", DateTime.Now);
                    cmd.Parameters.AddWithValue("@id", utilisateurId);
                    int result = cmd.ExecuteNonQuery();
                    return result > 0;
                }
                catch (Exception)
                {
                    // Ne pas afficher de message d'erreur pour cette opération non critique
                    return false;
                }
            }
        }

        public List<Utilisateur> ObtenirTousLesUtilisateurs()
        {
            var utilisateurs = new List<Utilisateur>();
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT * FROM utilisateurs";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            utilisateurs.Add(new Utilisateur
                            {
                                Id = reader.GetInt32("id"),
                                Nom = reader.GetString("nom"),
                                Prenom = reader.GetString("prenom"),
                                Email = reader.GetString("email"),
                                Role = reader.GetString("role")
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la récupération des utilisateurs : {ex.Message}");
                }
            }
            return utilisateurs;
        }

        // ======================
        // Méthodes pour les véhicules
        // ======================
        public List<Vehicule> ObtenirTousLesVehicules()
        {
            var vehicules = new List<Vehicule>();
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT * FROM vehicules";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var vehicule = new Vehicule
                            {
                                Id = reader.GetInt32("id"),
                                Marque = reader.GetString("marque"),
                                Modele = reader.GetString("modele"),
                                Annee = reader.GetInt32("annee"),
                                Prix = reader.GetDecimal("prix"),
                                Kilometrage = reader.GetInt32("kilometrage"),
                                Carburant = reader.GetString("carburant"),
                                Transmission = reader.GetString("transmission"),
                                Description = !reader.IsDBNull(reader.GetOrdinal("description")) ? reader.GetString("description") : null,
                                Image = !reader.IsDBNull(reader.GetOrdinal("image")) ? reader.GetString("image") : null,
                                CategorieId = reader.GetInt32("categorie_id"),
                                CreatedAt = reader.GetDateTime("created_at")
                            };

                            if (HasColumn(reader, "immatriculation") && !reader.IsDBNull(reader.GetOrdinal("immatriculation")))
                            {
                                vehicule.Immatriculation = reader.GetString("immatriculation");
                            }

                            vehicules.Add(vehicule);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la récupération des véhicules : {ex.Message}");
                }
            }
            return vehicules;
        }

        public List<Vehicule> RechercherVehiculesParReference(string reference)
        {
            var vehicules = new List<Vehicule>();
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    bool isNumeric = int.TryParse(reference, out var km);

                    string query = @"SELECT * FROM vehicules 
                                     WHERE marque LIKE @reference 
                                        OR modele LIKE @reference
                                        OR immatriculation LIKE @reference
                                        OR carburant LIKE @reference
                                        OR transmission LIKE @reference";

                    // Si numérique, on ajoute des filtres kilométrage (max et exact)
                    if (isNumeric)
                    {
                        query += " OR kilometrage <= @kmMax OR kilometrage = @kmExact";
                    }

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@reference", $"%{reference}%");
                    if (isNumeric)
                    {
                        cmd.Parameters.AddWithValue("@kmMax", km);
                        cmd.Parameters.AddWithValue("@kmExact", km);
                    }
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var vehicule = new Vehicule
                            {
                                Id = reader.GetInt32("id"),
                                Marque = reader.GetString("marque"),
                                Modele = reader.GetString("modele"),
                                Annee = reader.GetInt32("annee"),
                                Prix = reader.GetDecimal("prix"),
                                Kilometrage = reader.GetInt32("kilometrage"),
                                Carburant = reader.GetString("carburant"),
                                Transmission = reader.GetString("transmission"),
                                Description = !reader.IsDBNull(reader.GetOrdinal("description")) ? reader.GetString("description") : null,
                                Image = !reader.IsDBNull(reader.GetOrdinal("image")) ? reader.GetString("image") : null,
                                CategorieId = reader.GetInt32("categorie_id"),
                                CreatedAt = reader.GetDateTime("created_at")
                            };

                            if (HasColumn(reader, "immatriculation") && !reader.IsDBNull(reader.GetOrdinal("immatriculation")))
                            {
                                vehicule.Immatriculation = reader.GetString("immatriculation");
                            }

                            vehicules.Add(vehicule);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la recherche des véhicules : {ex.Message}");
                }
            }
            return vehicules;
        }

        private static bool HasColumn(System.Data.IDataRecord reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static void EnsureImmatriculationColumn(MySqlConnection connection)
        {
            const string checkSql = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                                      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'vehicules' AND COLUMN_NAME = 'immatriculation'";
            using var checkCmd = new MySqlCommand(checkSql, connection);
            var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            if (exists) return;

            const string alterSql = @"ALTER TABLE vehicules ADD COLUMN immatriculation VARCHAR(50) NULL AFTER modele";
            using var alterCmd = new MySqlCommand(alterSql, connection);
            alterCmd.ExecuteNonQuery();
        }

        private static void EnsureCategorieExists(MySqlConnection connection, int categorieId)
        {
            var targetId = categorieId <= 0 ? 1 : categorieId;

            const string checkTable = @"SELECT COUNT(*) FROM information_schema.tables 
                                        WHERE table_schema = DATABASE() AND table_name = 'categories'";
            using var checkTableCmd = new MySqlCommand(checkTable, connection);
            var tableExists = Convert.ToInt32(checkTableCmd.ExecuteScalar()) > 0;
            if (!tableExists)
            {
                const string createTable = @"CREATE TABLE categories(
                        id INT PRIMARY KEY AUTO_INCREMENT,
                        nom VARCHAR(100) NOT NULL
                    )";
                using var createCmd = new MySqlCommand(createTable, connection);
                createCmd.ExecuteNonQuery();
            }

            const string checkCategory = @"SELECT COUNT(*) FROM categories WHERE id = @id";
            using var checkCatCmd = new MySqlCommand(checkCategory, connection);
            checkCatCmd.Parameters.AddWithValue("@id", targetId);
            var exists = Convert.ToInt32(checkCatCmd.ExecuteScalar()) > 0;

            if (!exists)
            {
                const string insertCategory = @"INSERT INTO categories(id, nom) VALUES (@id, 'Par défaut')";
                using var insertCmd = new MySqlCommand(insertCategory, connection);
                insertCmd.Parameters.AddWithValue("@id", targetId);
                insertCmd.ExecuteNonQuery();
            }
        }

        public bool AjouterVehicule(Vehicule vehicule)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    EnsureImmatriculationColumn(connection);
                    EnsureCategorieExists(connection, vehicule.CategorieId);
                    string query = @"
                        INSERT INTO vehicules
                        (marque, modele, immatriculation, annee, prix, kilometrage, carburant, transmission, description, image, categorie_id, created_at)
                        VALUES
                        (@marque, @modele, @immatriculation, @annee, @prix, @kilometrage, @carburant, @transmission, @description, @image, @categorieId, @createdAt)";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@marque", vehicule.Marque);
                    cmd.Parameters.AddWithValue("@modele", vehicule.Modele);
                    cmd.Parameters.AddWithValue("@immatriculation", vehicule.Immatriculation);
                    cmd.Parameters.AddWithValue("@annee", vehicule.Annee);
                    cmd.Parameters.AddWithValue("@prix", vehicule.Prix);
                    cmd.Parameters.AddWithValue("@kilometrage", vehicule.Kilometrage);
                    cmd.Parameters.AddWithValue("@carburant", vehicule.Carburant);
                    cmd.Parameters.AddWithValue("@transmission", vehicule.Transmission);
                    cmd.Parameters.AddWithValue("@description", vehicule.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@image", vehicule.Image ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@categorieId", vehicule.CategorieId);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                    int result = cmd.ExecuteNonQuery();
                    return result > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ajout du véhicule : {ex.Message}");
                    return false;
                }
            }
        }

        public bool SupprimerVehicule(int vehiculeId)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    const string query = "DELETE FROM vehicules WHERE id = @id";
                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", vehiculeId);
                    var result = cmd.ExecuteNonQuery();
                    return result > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la suppression du véhicule : {ex.Message}");
                    return false;
                }
            }
        }

        // ======================
        // Méthodes pour les commandes
        // ======================
        public List<Commande> ObtenirCommandesParUtilisateur(int utilisateurId)
        {
            var commandes = new List<Commande>();
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = @"
                        SELECT c.id, c.utilisateur_id, c.vehicule_id, c.statut, c.date_commande,
                               CONCAT(v.marque, ' ', v.modele) as vehicule_marque_modele,
                               CONCAT(u.prenom, ' ', u.nom) as utilisateur_nom
                        FROM commandes c
                        JOIN vehicules v ON c.vehicule_id = v.id
                        JOIN utilisateurs u ON c.utilisateur_id = u.id
                        WHERE c.utilisateur_id = @utilisateurId";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@utilisateurId", utilisateurId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            commandes.Add(new Commande
                            {
                                Id = reader.GetInt32("id"),
                                UtilisateurId = reader.GetInt32("utilisateur_id"),
                                VehiculeId = reader.GetInt32("vehicule_id"),
                                Statut = reader.GetString("statut"),
                                DateCommande = reader.GetDateTime("date_commande"),
                                VehiculeMarqueModele = reader.GetString("vehicule_marque_modele"),
                                UtilisateurNom = reader.GetString("utilisateur_nom")
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la récupération des commandes : {ex.Message}");
                }
            }
            return commandes;
        }

        public List<Commande> ObtenirTousLesCommandes()
        {
            var commandes = new List<Commande>();
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = @"
                        SELECT c.id, c.utilisateur_id, c.vehicule_id, c.statut, c.date_commande,
                               CONCAT(v.marque, ' ', v.modele) as vehicule_marque_modele,
                               CONCAT(u.prenom, ' ', u.nom) as utilisateur_nom
                        FROM commandes c
                        JOIN vehicules v ON c.vehicule_id = v.id
                        JOIN utilisateurs u ON c.utilisateur_id = u.id";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            commandes.Add(new Commande
                            {
                                Id = reader.GetInt32("id"),
                                UtilisateurId = reader.GetInt32("utilisateur_id"),
                                VehiculeId = reader.GetInt32("vehicule_id"),
                                Statut = reader.GetString("statut"),
                                DateCommande = reader.GetDateTime("date_commande"),
                                VehiculeMarqueModele = reader.GetString("vehicule_marque_modele"),
                                UtilisateurNom = reader.GetString("utilisateur_nom")
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la récupération des commandes : {ex.Message}");
                }
            }
            return commandes;
        }

        public bool AjouterCommande(Commande commande)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO commandes (utilisateur_id, vehicule_id, statut, date_commande)
                        VALUES (@utilisateurId, @vehiculeId, @statut, @dateCommande)";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@utilisateurId", commande.UtilisateurId);
                    cmd.Parameters.AddWithValue("@vehiculeId", commande.VehiculeId);
                    cmd.Parameters.AddWithValue("@statut", commande.Statut);
                    cmd.Parameters.AddWithValue("@dateCommande", DateTime.Now);
                    int result = cmd.ExecuteNonQuery();
                    return result > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ajout de la commande : {ex.Message}");
                    return false;
                }
            }
        }

        public bool MettreAJourStatutCommande(int commandeId, string nouveauStatut)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "UPDATE commandes SET statut = @statut WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@statut", nouveauStatut);
                    cmd.Parameters.AddWithValue("@id", commandeId);
                    int result = cmd.ExecuteNonQuery();
                    return result > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la mise à jour du statut : {ex.Message}");
                    return false;
                }
            }
        }

        public int CompterVehiculesDisponibles()
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM vehicules";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch
                {
                    return 0;
                }
            }
        }

        public int CompterCommandesEnAttente()
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM commandes WHERE statut = 'En attente'";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch
                {
                    return 0;
                }
            }
        }
    }
}