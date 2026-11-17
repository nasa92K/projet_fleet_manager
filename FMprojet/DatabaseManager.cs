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
                    string query = "SELECT * FROM utilisateurs ORDER BY nom, prenom";
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

        public bool SupprimerUtilisateur(int utilisateurId)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    using var tx = connection.BeginTransaction();

                    // Supprimer les paiements puis les commandes liées d'abord (FK en NO ACTION)
                    ExecuterSiTableExiste(connection, tx, "paiements",
                        "DELETE FROM paiements WHERE commande_id IN (SELECT id FROM commandes WHERE utilisateur_id = @id)", utilisateurId);
                    ExecuterSiTableExiste(connection, tx, "commandes",
                        "DELETE FROM commandes WHERE utilisateur_id = @id", utilisateurId);

                    // Supprimer l'utilisateur
                    using (var cmd = new MySqlCommand(
                        "DELETE FROM utilisateurs WHERE id = @id", connection, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", utilisateurId);
                        var ok = cmd.ExecuteNonQuery() > 0;
                        tx.Commit();
                        return ok;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la suppression de l'utilisateur : {ex.Message}");
                    return false;
                }
            }
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
                                vehicule.Immatriculation = reader.GetString("immatriculation");

                            // Lecture de la date de mise en circulation
                            if (HasColumn(reader, "circulation") && !reader.IsDBNull(reader.GetOrdinal("circulation")))
                                vehicule.Circulation = reader.GetDateTime("circulation");

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
                                vehicule.Immatriculation = reader.GetString("immatriculation");

                            if (HasColumn(reader, "circulation") && !reader.IsDBNull(reader.GetOrdinal("circulation")))
                                vehicule.Circulation = reader.GetDateTime("circulation");

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

        private static void EnsureCirculationColumn(MySqlConnection connection)
        {
            const string checkSql = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                                      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'vehicules' AND COLUMN_NAME = 'circulation'";
            using var checkCmd = new MySqlCommand(checkSql, connection);
            var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            if (exists) return;

            const string alterSql = @"ALTER TABLE vehicules ADD COLUMN circulation DATE NULL AFTER created_at";
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
                    EnsureCirculationColumn(connection);
                    EnsureCategorieExists(connection, vehicule.CategorieId);
                    string query = @"
                        INSERT INTO vehicules
                        (marque, modele, immatriculation, annee, prix, kilometrage, carburant, transmission, description, image, categorie_id, created_at, circulation)
                        VALUES
                        (@marque, @modele, @immatriculation, @annee, @prix, @kilometrage, @carburant, @transmission, @description, @image, @categorieId, @createdAt, @circulation)";
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
                    cmd.Parameters.AddWithValue("@image",       vehicule.Image       ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@categorieId", vehicule.CategorieId);
                    cmd.Parameters.AddWithValue("@createdAt",   DateTime.Now);
                    // Date de mise en circulation (nullable)
                    cmd.Parameters.AddWithValue("@circulation",
                        vehicule.Circulation.HasValue ? (object)vehicule.Circulation.Value.Date : DBNull.Value);
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
                    using var tx = connection.BeginTransaction();

                    // Les FK en base sont en NO ACTION (et certaines tables sont en MyISAM, sans FK) :
                    // on supprime donc explicitement toutes les lignes dépendantes, enfants d'abord.
                    ExecuterSiTableExiste(connection, tx, "paiements",
                        "DELETE FROM paiements WHERE commande_id IN (SELECT id FROM commandes WHERE vehicule_id = @id)", vehiculeId);
                    ExecuterSiTableExiste(connection, tx, "commandes",
                        "DELETE FROM commandes WHERE vehicule_id = @id", vehiculeId);
                    ExecuterSiTableExiste(connection, tx, "images",
                        "DELETE FROM images WHERE vehicule_id = @id", vehiculeId);
                    ExecuterSiTableExiste(connection, tx, "historique_vehicule",
                        "DELETE FROM historique_vehicule WHERE vehicule_id = @id", vehiculeId);
                    ExecuterSiTableExiste(connection, tx, "suivi_depenses",
                        "DELETE FROM suivi_depenses WHERE vehicule_id = @id", vehiculeId);
                    ExecuterSiTableExiste(connection, tx, "suivi_vehicules",
                        "DELETE FROM suivi_vehicules WHERE vehicule_id = @id", vehiculeId);

                    using var cmd = new MySqlCommand("DELETE FROM vehicules WHERE id = @id", connection, tx);
                    cmd.Parameters.AddWithValue("@id", vehiculeId);
                    var result = cmd.ExecuteNonQuery();

                    tx.Commit();
                    return result > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la suppression du véhicule : {ex.Message}");
                    return false;
                }
            }
        }

        private static void ExecuterSiTableExiste(MySqlConnection connection, MySqlTransaction tx, string table, string sql, int id)
        {
            const string checkTable = @"SELECT COUNT(*) FROM information_schema.tables
                                        WHERE table_schema = DATABASE() AND table_name = @table";
            using (var checkCmd = new MySqlCommand(checkTable, connection, tx))
            {
                checkCmd.Parameters.AddWithValue("@table", table);
                if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0) return;
            }

            using var cmd = new MySqlCommand(sql, connection, tx);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // ======================
        // Modifier un véhicule existant
        // ======================
        /// <summary>
        /// Met à jour toutes les colonnes éditables du véhicule dont l'Id correspond.
        /// Utilise des paramètres SQL pour prévenir toute injection.
        /// </summary>
        /// <param name="vehicule">Objet Vehicule contenant les nouvelles valeurs.</param>
        /// <returns>true si au moins une ligne a été modifiée, false sinon.</returns>
        public bool ModifierVehicule(Vehicule vehicule)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();

                    // S'assurer que les colonnes optionnelles existent (migration transparente)
                    EnsureImmatriculationColumn(connection);
                    EnsureCirculationColumn(connection);

                    const string query = @"
                        UPDATE vehicules SET
                            marque          = @marque,
                            modele          = @modele,
                            immatriculation = @immatriculation,
                            annee           = @annee,
                            prix            = @prix,
                            kilometrage     = @kilometrage,
                            carburant       = @carburant,
                            transmission    = @transmission,
                            description     = @description,
                            image           = @image,
                            categorie_id    = @categorieId,
                            circulation     = @circulation
                        WHERE id = @id";

                    using var cmd = new MySqlCommand(query, connection);

                    // Colonnes texte obligatoires
                    cmd.Parameters.AddWithValue("@marque",          vehicule.Marque);
                    cmd.Parameters.AddWithValue("@modele",          vehicule.Modele);
                    cmd.Parameters.AddWithValue("@immatriculation",  vehicule.Immatriculation);
                    cmd.Parameters.AddWithValue("@carburant",        vehicule.Carburant);
                    cmd.Parameters.AddWithValue("@transmission",     vehicule.Transmission);

                    // Colonnes numériques
                    cmd.Parameters.AddWithValue("@annee",       vehicule.Annee);
                    cmd.Parameters.AddWithValue("@prix",        vehicule.Prix);
                    cmd.Parameters.AddWithValue("@kilometrage",  vehicule.Kilometrage);
                    cmd.Parameters.AddWithValue("@categorieId",  vehicule.CategorieId);

                    // Colonnes nullables
                    cmd.Parameters.AddWithValue("@description", vehicule.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@image",       vehicule.Image       ?? (object)DBNull.Value);

                    // Date de mise en circulation (nullable)
                    cmd.Parameters.AddWithValue("@circulation",
                        vehicule.Circulation.HasValue ? (object)vehicule.Circulation.Value.Date : DBNull.Value);

                    // Clé primaire — identifie la ligne à mettre à jour
                    cmd.Parameters.AddWithValue("@id", vehicule.Id);

                    int lignesAffectees = cmd.ExecuteNonQuery();
                    return lignesAffectees > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Erreur lors de la modification du véhicule : {ex.Message}",
                        "Erreur base de données",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }
            }
        }

        private static void AfficherErreurDb(string contexte, Exception ex)
        {
            MessageBox.Show($"{contexte} : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // ======================
        // Véhicules à remplacer (âge ≥ 4 ans ET km ≥ 80 000)
        // ======================
        /// <summary>
        /// Retourne la liste des véhicules dont la date de circulation indique
        /// un âge ≥ 4 ans ET dont le kilométrage est ≥ 80 000 km.
        /// Si circulation est NULL, on utilise l'année de fabrication comme fallback.
        /// </summary>
        public List<Vehicule> ObtenirVehiculesARemplacer()
        {
            var vehicules = new List<Vehicule>();
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();

                    // YEAR(NOW()) - YEAR(circulation) >= 4  → âge via date de circulation
                    // En cas de NULL on se rabat sur (YEAR(NOW()) - annee >= 4)
                    const string query = @"
                        SELECT * FROM vehicules
                        WHERE kilometrage >= 80000
                          AND (
                                (circulation IS NOT NULL AND TIMESTAMPDIFF(YEAR, circulation, CURDATE()) >= 4)
                             OR (circulation IS NULL     AND YEAR(CURDATE()) - annee >= 4)
                              )
                        ORDER BY kilometrage DESC";

                    using var cmd = new MySqlCommand(query, connection);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var v = new Vehicule
                        {
                            Id           = reader.GetInt32("id"),
                            Marque       = reader.GetString("marque"),
                            Modele       = reader.GetString("modele"),
                            Annee        = reader.GetInt32("annee"),
                            Prix         = reader.GetDecimal("prix"),
                            Kilometrage  = reader.GetInt32("kilometrage"),
                            Carburant    = reader.GetString("carburant"),
                            Transmission = reader.GetString("transmission"),
                            Description  = !reader.IsDBNull(reader.GetOrdinal("description")) ? reader.GetString("description") : null,
                            Image        = !reader.IsDBNull(reader.GetOrdinal("image"))       ? reader.GetString("image")       : null,
                            CategorieId  = reader.GetInt32("categorie_id"),
                            CreatedAt    = reader.GetDateTime("created_at")
                        };

                        if (HasColumn(reader, "immatriculation") && !reader.IsDBNull(reader.GetOrdinal("immatriculation")))
                            v.Immatriculation = reader.GetString("immatriculation");

                        if (HasColumn(reader, "circulation") && !reader.IsDBNull(reader.GetOrdinal("circulation")))
                            v.Circulation = reader.GetDateTime("circulation");

                        vehicules.Add(v);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Erreur lors de la récupération des véhicules à remplacer : {ex.Message}",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            return vehicules;
        }

        /// <summary>
        /// Compte le nombre de véhicules à remplacer (pour le KPI du dashboard).
        /// </summary>
        public int CompterVehiculesARemplacer()
        {
            using var connection = GetConnection();
            try
            {
                connection.Open();
                const string sql = @"
                    SELECT COUNT(*) FROM vehicules
                    WHERE kilometrage >= 80000
                      AND (
                            (circulation IS NOT NULL AND TIMESTAMPDIFF(YEAR, circulation, CURDATE()) >= 4)
                         OR (circulation IS NULL     AND YEAR(CURDATE()) - annee >= 4)
                          )";
                using var cmd = new MySqlCommand(sql, connection);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch { return 0; }
        }

        private static void EnsureHistoriqueVehiculeTable(MySqlConnection connection)
        {
            // Vérification : la table existe-t-elle déjà ?
            const string checkTable = @"SELECT COUNT(*) FROM information_schema.tables
                                        WHERE table_schema = DATABASE() AND table_name = 'historique_vehicule'";
            using var checkCmd = new MySqlCommand(checkTable, connection);
            var tableExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            if (tableExists) return;

            // ── Commande 1 : CREATE TABLE ──────────────────────────────────────
            // MySqlConnector n'accepte pas plusieurs instructions dans une seule
            // commande sans AllowBatch=true. On les sépare donc explicitement.
            const string createTable = @"
                CREATE TABLE IF NOT EXISTS historique_vehicule (
                    id            INT AUTO_INCREMENT PRIMARY KEY,
                    vehicule_id   INT UNSIGNED NOT NULL,
                    type          VARCHAR(32) NOT NULL,
                    valeur_texte  VARCHAR(255) NULL,
                    valeur_num    DECIMAL(12,2) NULL,
                    cout          DECIMAL(12,2) NULL,
                    date_event    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    notes         TEXT NULL,
                    created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT fk_historique_vehicule_vehicules
                      FOREIGN KEY (vehicule_id) REFERENCES vehicules(id)
                      ON DELETE CASCADE
                )";
            try
            {
                using var cmdTable = new MySqlCommand(createTable, connection);
                cmdTable.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Ne pas faire crasher l'application si la table ne peut pas être créée
                System.Diagnostics.Debug.WriteLine($"EnsureHistoriqueVehiculeTable: {ex.Message}");
                return;
            }

            // ── Commande 2 : INDEX sur (vehicule_id, date_event) ──────────────
            const string idx1 = @"
                CREATE INDEX IF NOT EXISTS idx_hist_vehicule_date
                ON historique_vehicule(vehicule_id, date_event)";
            using var cmdIdx1 = new MySqlCommand(idx1, connection);
            try { cmdIdx1.ExecuteNonQuery(); } catch { /* index déjà présent */ }

            // ── Commande 3 : INDEX sur (vehicule_id, type, date_event) ─────────
            const string idx2 = @"
                CREATE INDEX IF NOT EXISTS idx_hist_vehicule_type_date
                ON historique_vehicule(vehicule_id, type, date_event)";
            using var cmdIdx2 = new MySqlCommand(idx2, connection);
            try { cmdIdx2.ExecuteNonQuery(); } catch { /* index déjà présent */ }
        }

        private static void EnsureSuiviDepensesTable(MySqlConnection connection)
        {
            const string checkTable = @"SELECT COUNT(*) FROM information_schema.tables
                                        WHERE table_schema = DATABASE() AND table_name = 'suivi_depenses'";
            using var checkCmd = new MySqlCommand(checkTable, connection);
            var tableExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            if (tableExists) return;

            // ── Commande 1 : CREATE TABLE ──────────────────────────────────────
            const string createTable = @"
                CREATE TABLE IF NOT EXISTS suivi_depenses (
                    id                 INT AUTO_INCREMENT PRIMARY KEY,
                    vehicule_id        INT UNSIGNED NOT NULL,
                    date_jour          DATE NOT NULL,
                    kilometrage        INT NULL,
                    carburant_qte      DECIMAL(10,2) NULL,
                    carburant_prix     DECIMAL(12,2) NULL,
                    peages_frais       DECIMAL(12,2) NULL,
                    entretien_type     VARCHAR(120) NULL,
                    entretien_cout     DECIMAL(12,2) NULL,
                    assurance_echeance DATE NULL,
                    assurance_montant  DECIMAL(12,2) NULL,
                    notes              TEXT NULL,
                    created_at         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT fk_suivi_depenses_vehicules
                      FOREIGN KEY (vehicule_id) REFERENCES vehicules(id)
                      ON DELETE CASCADE,
                    CONSTRAINT uq_suivi_depenses_vehicule_jour
                      UNIQUE (vehicule_id, date_jour)
                )";
            try
            {
                using var cmdTable = new MySqlCommand(createTable, connection);
                cmdTable.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureSuiviDepensesTable: {ex.Message}");
                return;
            }

            // ── Commande 2 : INDEX ─────────────────────────────────────────────
            const string idx = @"
                CREATE INDEX IF NOT EXISTS idx_suivi_depenses_vehicule_date
                ON suivi_depenses(vehicule_id, date_jour)";
            using var cmdIdx = new MySqlCommand(idx, connection);
            try { cmdIdx.ExecuteNonQuery(); } catch { /* index déjà présent */ }
        }

        public bool UpsertSuiviDepense(SuiviDepense suivi)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    EnsureSuiviDepensesTable(connection);

                    const string query = @"
                        INSERT INTO suivi_depenses
                          (vehicule_id, date_jour, kilometrage, carburant_qte, carburant_prix, peages_frais,
                           entretien_type, entretien_cout, assurance_echeance, assurance_montant, notes)
                        VALUES
                          (@vehiculeId, @dateJour, @kilometrage, @carburantQte, @carburantPrix, @peagesFrais,
                           @entretienType, @entretienCout, @assuranceEcheance, @assuranceMontant, @notes)
                        ON DUPLICATE KEY UPDATE
                          kilometrage = VALUES(kilometrage),
                          carburant_qte = VALUES(carburant_qte),
                          carburant_prix = VALUES(carburant_prix),
                          peages_frais = VALUES(peages_frais),
                          entretien_type = VALUES(entretien_type),
                          entretien_cout = VALUES(entretien_cout),
                          assurance_echeance = VALUES(assurance_echeance),
                          assurance_montant = VALUES(assurance_montant),
                          notes = VALUES(notes)";

                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@vehiculeId", suivi.VehiculeId);
                    cmd.Parameters.AddWithValue("@dateJour", suivi.DateJour.Date);
                    cmd.Parameters.AddWithValue("@kilometrage", (object?)suivi.Kilometrage ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@carburantQte", (object?)suivi.CarburantQte ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@carburantPrix", (object?)suivi.CarburantPrix ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@peagesFrais", (object?)suivi.PeagesFrais ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@entretienType", (object?)suivi.EntretienType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@entretienCout", (object?)suivi.EntretienCout ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@assuranceEcheance", (object?)suivi.AssuranceEcheance?.Date ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@assuranceMontant", (object?)suivi.AssuranceMontant ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", (object?)suivi.Notes ?? DBNull.Value);

                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    AfficherErreurDb("Erreur lors de l'enregistrement du suivi des dépenses", ex);
                    return false;
                }
            }
        }

        public List<SuiviDepense> ObtenirSuiviDepensesVehicule(int vehiculeId)
        {
            var suivis = new List<SuiviDepense>();
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    EnsureSuiviDepensesTable(connection);

                    const string query = @"
                        SELECT id, vehicule_id, date_jour, kilometrage, carburant_qte, carburant_prix,
                               peages_frais, entretien_type, entretien_cout, assurance_echeance, assurance_montant,
                               notes, created_at
                        FROM suivi_depenses
                        WHERE vehicule_id = @vehiculeId
                        ORDER BY date_jour ASC, id ASC";

                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@vehiculeId", vehiculeId);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        suivis.Add(new SuiviDepense
                        {
                            Id = reader.GetInt32("id"),
                            VehiculeId = reader.GetInt32("vehicule_id"),
                            DateJour = reader.GetDateTime("date_jour"),
                            Kilometrage = reader.IsDBNull(reader.GetOrdinal("kilometrage")) ? null : reader.GetInt32("kilometrage"),
                            CarburantQte = reader.IsDBNull(reader.GetOrdinal("carburant_qte")) ? null : reader.GetDecimal("carburant_qte"),
                            CarburantPrix = reader.IsDBNull(reader.GetOrdinal("carburant_prix")) ? null : reader.GetDecimal("carburant_prix"),
                            PeagesFrais = reader.IsDBNull(reader.GetOrdinal("peages_frais")) ? null : reader.GetDecimal("peages_frais"),
                            EntretienType = reader.IsDBNull(reader.GetOrdinal("entretien_type")) ? null : reader.GetString("entretien_type"),
                            EntretienCout = reader.IsDBNull(reader.GetOrdinal("entretien_cout")) ? null : reader.GetDecimal("entretien_cout"),
                            AssuranceEcheance = reader.IsDBNull(reader.GetOrdinal("assurance_echeance")) ? null : reader.GetDateTime("assurance_echeance"),
                            AssuranceMontant = reader.IsDBNull(reader.GetOrdinal("assurance_montant")) ? null : reader.GetDecimal("assurance_montant"),
                            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                            CreatedAt = reader.GetDateTime("created_at")
                        });
                    }
                }
                catch (Exception ex)
                {
                    AfficherErreurDb("Erreur lors du chargement du suivi des dépenses", ex);
                }
            }
            return suivis;
        }

        public List<(DateTime Jour, decimal Total)> ObtenirDepensesVehiculeParJour(int vehiculeId, int jours = 30)
        {
            var resultats = new List<(DateTime, decimal)>();
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    EnsureSuiviDepensesTable(connection);

                    var depuis = DateTime.Today.AddDays(-(jours - 1));
                    const string query = @"
                        SELECT date_jour AS jour,
                               COALESCE(carburant_prix,0) + COALESCE(peages_frais,0) + COALESCE(entretien_cout,0) + COALESCE(assurance_montant,0) AS total
                        FROM suivi_depenses
                        WHERE vehicule_id = @vehiculeId AND date_jour >= @depuis
                        ORDER BY date_jour ASC";
                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@vehiculeId", vehiculeId);
                    cmd.Parameters.AddWithValue("@depuis", depuis);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        resultats.Add((reader.GetDateTime("jour"), reader.GetDecimal("total")));
                    }
                }
                catch (Exception ex)
                {
                    AfficherErreurDb("Erreur lors du chargement des dépenses par jour (véhicule)", ex);
                }
            }
            return resultats;
        }

        public List<(string Categorie, decimal Total)> ObtenirDepensesFlotteParCategorie(int jours = 30)
        {
            var resultats = new List<(string, decimal)>();
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    EnsureSuiviDepensesTable(connection);

                    var depuis = DateTime.Today.AddDays(-(jours - 1));
                    const string query = @"
                        SELECT
                            SUM(COALESCE(carburant_prix,0)) AS total_carburant,
                            SUM(COALESCE(peages_frais,0)) AS total_peage,
                            SUM(COALESCE(entretien_cout,0)) AS total_entretien,
                            SUM(COALESCE(assurance_montant,0)) AS total_assurance
                        FROM suivi_depenses
                        WHERE date_jour >= @depuis";
                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@depuis", depuis);
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        var carburant = reader.IsDBNull(reader.GetOrdinal("total_carburant")) ? 0m : reader.GetDecimal("total_carburant");
                        var peage = reader.IsDBNull(reader.GetOrdinal("total_peage")) ? 0m : reader.GetDecimal("total_peage");
                        var entretien = reader.IsDBNull(reader.GetOrdinal("total_entretien")) ? 0m : reader.GetDecimal("total_entretien");
                        var assurance = reader.IsDBNull(reader.GetOrdinal("total_assurance")) ? 0m : reader.GetDecimal("total_assurance");

                        resultats.Add(("Carburant", carburant));
                        resultats.Add(("Péage", peage));
                        resultats.Add(("Entretien", entretien));
                        resultats.Add(("Assurance", assurance));
                    }
                }
                catch (Exception ex)
                {
                    AfficherErreurDb("Erreur lors du chargement des dépenses par catégorie (flotte)", ex);
                }
            }
            return resultats;
        }

        public bool AjouterHistoriqueVehicule(HistoriqueVehicule entree)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    EnsureHistoriqueVehiculeTable(connection);

                    const string query = @"
                        INSERT INTO historique_vehicule
                          (vehicule_id, type, valeur_texte, valeur_num, cout, date_event, notes, created_at)
                        VALUES
                          (@vehiculeId, @type, @valeurTexte, @valeurNum, @cout, @dateEvent, @notes, @createdAt)";
                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@vehiculeId", entree.VehiculeId);
                    cmd.Parameters.AddWithValue("@type", entree.Type);
                    cmd.Parameters.AddWithValue("@valeurTexte", (object?)entree.ValeurTexte ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@valeurNum", (object?)entree.ValeurNum ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cout", (object?)entree.Cout ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@dateEvent", entree.DateEvent == default ? DateTime.Now : entree.DateEvent);
                    cmd.Parameters.AddWithValue("@notes", (object?)entree.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    AfficherErreurDb("Erreur lors de l'ajout dans l'historique véhicule", ex);
                    return false;
                }
            }
        }

        public List<HistoriqueVehicule> ObtenirHistoriqueVehicule(int vehiculeId, string? type = null, DateTime? depuis = null)
        {
            var historiques = new List<HistoriqueVehicule>();
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    EnsureHistoriqueVehiculeTable(connection);

                    var query = @"
                        SELECT id, vehicule_id, type, valeur_texte, valeur_num, cout, date_event, notes, created_at
                        FROM historique_vehicule
                        WHERE vehicule_id = @vehiculeId";

                    if (!string.IsNullOrWhiteSpace(type))
                        query += " AND type = @type";
                    if (depuis != null)
                        query += " AND date_event >= @depuis";

                    query += " ORDER BY date_event DESC, id DESC";

                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@vehiculeId", vehiculeId);
                    if (!string.IsNullOrWhiteSpace(type)) cmd.Parameters.AddWithValue("@type", type);
                    if (depuis != null) cmd.Parameters.AddWithValue("@depuis", depuis.Value);

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        historiques.Add(new HistoriqueVehicule
                        {
                            Id = reader.GetInt32("id"),
                            VehiculeId = reader.GetInt32("vehicule_id"),
                            Type = reader.GetString("type"),
                            ValeurTexte = reader.IsDBNull(reader.GetOrdinal("valeur_texte")) ? null : reader.GetString("valeur_texte"),
                            ValeurNum = reader.IsDBNull(reader.GetOrdinal("valeur_num")) ? null : reader.GetDecimal("valeur_num"),
                            Cout = reader.IsDBNull(reader.GetOrdinal("cout")) ? null : reader.GetDecimal("cout"),
                            DateEvent = reader.GetDateTime("date_event"),
                            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                            CreatedAt = reader.GetDateTime("created_at")
                        });
                    }
                }
                catch (Exception ex)
                {
                    AfficherErreurDb("Erreur lors du chargement de l'historique véhicule", ex);
                }
            }
            return historiques;
        }

        public HistoriqueVehicule? ObtenirDerniereEntreeHistorique(int vehiculeId, string type)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    EnsureHistoriqueVehiculeTable(connection);

                    const string query = @"
                        SELECT id, vehicule_id, type, valeur_texte, valeur_num, cout, date_event, notes, created_at
                        FROM historique_vehicule
                        WHERE vehicule_id = @vehiculeId AND type = @type
                        ORDER BY date_event DESC, id DESC
                        LIMIT 1";
                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@vehiculeId", vehiculeId);
                    cmd.Parameters.AddWithValue("@type", type);
                    using var reader = cmd.ExecuteReader();
                    if (!reader.Read()) return null;

                    return new HistoriqueVehicule
                    {
                        Id = reader.GetInt32("id"),
                        VehiculeId = reader.GetInt32("vehicule_id"),
                        Type = reader.GetString("type"),
                        ValeurTexte = reader.IsDBNull(reader.GetOrdinal("valeur_texte")) ? null : reader.GetString("valeur_texte"),
                        ValeurNum = reader.IsDBNull(reader.GetOrdinal("valeur_num")) ? null : reader.GetDecimal("valeur_num"),
                        Cout = reader.IsDBNull(reader.GetOrdinal("cout")) ? null : reader.GetDecimal("cout"),
                        DateEvent = reader.GetDateTime("date_event"),
                        Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                        CreatedAt = reader.GetDateTime("created_at")
                    };
                }
                catch (Exception ex)
                {
                    AfficherErreurDb("Erreur lors du chargement de la dernière entrée d'historique", ex);
                    return null;
                }
            }
        }

        public bool MettreAJourVehiculeAvecHistorique(int vehiculeId, int nouveauKilometrage, string nouveauCarburant, string? notes = null)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    EnsureHistoriqueVehiculeTable(connection);

                    // Lecture snapshot actuel
                    const string readQuery = "SELECT kilometrage, carburant FROM vehicules WHERE id = @id";
                    int kmActuel;
                    string carbActuel;
                    using (var readCmd = new MySqlCommand(readQuery, connection))
                    {
                        readCmd.Parameters.AddWithValue("@id", vehiculeId);
                        using var reader = readCmd.ExecuteReader();
                        if (!reader.Read()) return false;
                        kmActuel = reader.GetInt32("kilometrage");
                        carbActuel = reader.IsDBNull(reader.GetOrdinal("carburant")) ? "" : reader.GetString("carburant");
                    }

                    using var tx = connection.BeginTransaction();

                    if (nouveauKilometrage != kmActuel)
                    {
                        using var insertKm = new MySqlCommand(@"
                            INSERT INTO historique_vehicule (vehicule_id, type, valeur_num, date_event, notes, created_at)
                            VALUES (@vehiculeId, 'kilometrage', @valeurNum, @dateEvent, @notes, @createdAt)", connection, tx);
                        insertKm.Parameters.AddWithValue("@vehiculeId", vehiculeId);
                        insertKm.Parameters.AddWithValue("@valeurNum", (decimal)nouveauKilometrage);
                        insertKm.Parameters.AddWithValue("@dateEvent", DateTime.Now);
                        insertKm.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
                        insertKm.Parameters.AddWithValue("@createdAt", DateTime.Now);
                        insertKm.ExecuteNonQuery();
                    }

                    if (!string.Equals(nouveauCarburant ?? "", carbActuel ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        using var insertCarb = new MySqlCommand(@"
                            INSERT INTO historique_vehicule (vehicule_id, type, valeur_texte, date_event, notes, created_at)
                            VALUES (@vehiculeId, 'carburant', @valeurTexte, @dateEvent, @notes, @createdAt)", connection, tx);
                        insertCarb.Parameters.AddWithValue("@vehiculeId", vehiculeId);
                        insertCarb.Parameters.AddWithValue("@valeurTexte", nouveauCarburant ?? "");
                        insertCarb.Parameters.AddWithValue("@dateEvent", DateTime.Now);
                        insertCarb.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
                        insertCarb.Parameters.AddWithValue("@createdAt", DateTime.Now);
                        insertCarb.ExecuteNonQuery();
                    }

                    using var update = new MySqlCommand(
                        "UPDATE vehicules SET kilometrage = @km, carburant = @carb WHERE id = @id",
                        connection,
                        tx);
                    update.Parameters.AddWithValue("@km", nouveauKilometrage);
                    update.Parameters.AddWithValue("@carb", nouveauCarburant ?? "");
                    update.Parameters.AddWithValue("@id", vehiculeId);

                    var ok = update.ExecuteNonQuery() > 0;
                    tx.Commit();
                    return ok;
                }
                catch (Exception ex)
                {
                    AfficherErreurDb("Erreur lors de la mise à jour du véhicule", ex);
                    return false;
                }
            }
        }

        public List<(DateTime Jour, decimal CoutTotal)> ObtenirCoutsParJourDerniersJours(int jours = 30)
        {
            var resultats = new List<(DateTime, decimal)>();
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    EnsureHistoriqueVehiculeTable(connection);

                    var depuis = DateTime.Today.AddDays(-(jours - 1));
                    const string query = @"
                        SELECT DATE(date_event) AS jour, COALESCE(SUM(cout), 0) AS cout_total
                        FROM historique_vehicule
                        WHERE date_event >= @depuis
                          AND cout IS NOT NULL
                        GROUP BY DATE(date_event)
                        ORDER BY DATE(date_event) ASC";
                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@depuis", depuis);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var jour = reader.GetDateTime("jour");
                        var coutTotal = reader.GetDecimal("cout_total");
                        resultats.Add((jour, coutTotal));
                    }
                }
                catch (Exception ex)
                {
                    AfficherErreurDb("Erreur lors du chargement des coûts (dashboard)", ex);
                }
            }
            return resultats;
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
                    string query = "SELECT COUNT(*) FROM commandes WHERE statut = @statut";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@statut", StatutCommande.EnAttente);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch
                {
                    return 0;
                }
            }
        }

        public int CompterToutesCommandes()
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM commandes";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch
                {
                    return 0;
                }
            }
        }

        public int CompterUtilisateurs()
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM utilisateurs";
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