-- =====================================================
-- Script d'insertion de données réalistes
-- Fleet Manager — 10 utilisateurs + véhicules + suivi
-- =====================================================

USE projet_fleet_manager;

-- ── 1. CATÉGORIE par défaut ────────────────────────
INSERT IGNORE INTO categories (id, nom) VALUES (1, 'Standard');

-- ── 2. UTILISATEURS (10 nouveaux) ─────────────────
-- Mot de passe = "password123" pour tous (simplification)
INSERT IGNORE INTO utilisateurs (nom, prenom, email, mot_de_passe, role) VALUES
  ('Martin',    'Lucas',    'lucas.martin@fleetmanager.fr',    'password123', 'utilisateur'),
  ('Bernard',   'Emma',     'emma.bernard@fleetmanager.fr',    'password123', 'utilisateur'),
  ('Dupont',    'Thomas',   'thomas.dupont@fleetmanager.fr',   'password123', 'utilisateur'),
  ('Leroy',     'Chloé',    'chloe.leroy@fleetmanager.fr',     'password123', 'utilisateur'),
  ('Moreau',    'Nicolas',  'nicolas.moreau@fleetmanager.fr',  'password123', 'utilisateur'),
  ('Simon',     'Julie',    'julie.simon@fleetmanager.fr',     'password123', 'utilisateur'),
  ('Laurent',   'Maxime',   'maxime.laurent@fleetmanager.fr',  'password123', 'admin'),
  ('Michel',    'Sarah',    'sarah.michel@fleetmanager.fr',    'password123', 'utilisateur'),
  ('Garcia',    'Antoine',  'antoine.garcia@fleetmanager.fr',  'password123', 'utilisateur'),
  ('Roux',      'Camille',  'camille.roux@fleetmanager.fr',    'password123', 'utilisateur');

-- ── 3. VÉHICULES (10 nouveaux) ────────────────────
-- On s'assure que la colonne immatriculation existe
-- (DatabaseManager le fait auto, mais on force ici)
ALTER TABLE vehicules MODIFY COLUMN immatriculation VARCHAR(50) NULL;

INSERT INTO vehicules (marque, modele, immatriculation, annee, prix, kilometrage, carburant, transmission, description, image, categorie_id, created_at) VALUES
  ('Toyota',    'Yaris',      'AA-123-BB', 2022, 18500.00,  12000, 'Essence',  'Manuelle',    'Compacte économique',     NULL, 1, NOW()),
  ('Volkswagen','Golf',       'BB-456-CC', 2021, 24900.00,  28000, 'Diesel',   'Automatique', 'Berline familiale',        NULL, 1, NOW()),
  ('Renault',   'Mégane',     'CC-789-DD', 2020, 19800.00,  41000, 'Essence',  'Manuelle',    'Compacte pratique',        NULL, 1, NOW()),
  ('Peugeot',   '3008',       'DD-012-EE', 2023, 34500.00,   8500, 'Hybride',  'Automatique', 'SUV hybride premium',      NULL, 1, NOW()),
  ('BMW',       'Série 1',    'EE-345-FF', 2022, 31000.00,  19000, 'Essence',  'Automatique', 'Berline premium sportive', NULL, 1, NOW()),
  ('Mercedes',  'Classe A',   'FF-678-GG', 2021, 29500.00,  32000, 'Diesel',   'Automatique', 'Compacte luxe',            NULL, 1, NOW()),
  ('Citroën',   'C3',         'GG-901-HH', 2022, 15900.00,  17000, 'Essence',  'Manuelle',    'Citadine légère',          NULL, 1, NOW()),
  ('Ford',      'Puma',       'HH-234-II', 2023, 26800.00,   5500, 'Hybride',  'Automatique', 'SUV crossover moderne',    NULL, 1, NOW()),
  ('Nissan',    'Qashqai',    'II-567-JJ', 2020, 22000.00,  55000, 'Diesel',   'Manuelle',    'SUV compact robuste',      NULL, 1, NOW()),
  ('Seat',      'Leon',       'JJ-890-KK', 2021, 21500.00,  38000, 'Essence',  'Manuelle',    'Compacte sportive',        NULL, 1, NOW());

-- ── 4. COMMANDES (en attente + validées) ──────────
-- On récupère les IDs des utilisateurs et véhicules nouvellement insérés
-- via sous-requêtes sur email / immatriculation

INSERT INTO commandes (utilisateur_id, vehicule_id, statut, date_commande)
SELECT u.id, v.id, 'en_attente', DATE_SUB(NOW(), INTERVAL 1 DAY)
FROM utilisateurs u, vehicules v
WHERE u.email = 'lucas.martin@fleetmanager.fr'
  AND v.immatriculation = 'AA-123-BB';

INSERT INTO commandes (utilisateur_id, vehicule_id, statut, date_commande)
SELECT u.id, v.id, 'en_attente', DATE_SUB(NOW(), INTERVAL 2 DAY)
FROM utilisateurs u, vehicules v
WHERE u.email = 'emma.bernard@fleetmanager.fr'
  AND v.immatriculation = 'BB-456-CC';

INSERT INTO commandes (utilisateur_id, vehicule_id, statut, date_commande)
SELECT u.id, v.id, 'en_attente', DATE_SUB(NOW(), INTERVAL 3 DAY)
FROM utilisateurs u, vehicules v
WHERE u.email = 'thomas.dupont@fleetmanager.fr'
  AND v.immatriculation = 'CC-789-DD';

INSERT INTO commandes (utilisateur_id, vehicule_id, statut, date_commande)
SELECT u.id, v.id, 'confirmee', DATE_SUB(NOW(), INTERVAL 5 DAY)
FROM utilisateurs u, vehicules v
WHERE u.email = 'chloe.leroy@fleetmanager.fr'
  AND v.immatriculation = 'DD-012-EE';

INSERT INTO commandes (utilisateur_id, vehicule_id, statut, date_commande)
SELECT u.id, v.id, 'confirmee', DATE_SUB(NOW(), INTERVAL 7 DAY)
FROM utilisateurs u, vehicules v
WHERE u.email = 'nicolas.moreau@fleetmanager.fr'
  AND v.immatriculation = 'EE-345-FF';

INSERT INTO commandes (utilisateur_id, vehicule_id, statut, date_commande)
SELECT u.id, v.id, 'en_attente', DATE_SUB(NOW(), INTERVAL 1 DAY)
FROM utilisateurs u, vehicules v
WHERE u.email = 'julie.simon@fleetmanager.fr'
  AND v.immatriculation = 'FF-678-GG';

INSERT INTO commandes (utilisateur_id, vehicule_id, statut, date_commande)
SELECT u.id, v.id, 'en_attente', DATE_SUB(NOW(), INTERVAL 4 DAY)
FROM utilisateurs u, vehicules v
WHERE u.email = 'sarah.michel@fleetmanager.fr'
  AND v.immatriculation = 'GG-901-HH';

INSERT INTO commandes (utilisateur_id, vehicule_id, statut, date_commande)
SELECT u.id, v.id, 'annulee', DATE_SUB(NOW(), INTERVAL 10 DAY)
FROM utilisateurs u, vehicules v
WHERE u.email = 'antoine.garcia@fleetmanager.fr'
  AND v.immatriculation = 'HH-234-II';

INSERT INTO commandes (utilisateur_id, vehicule_id, statut, date_commande)
SELECT u.id, v.id, 'en_attente', DATE_SUB(NOW(), INTERVAL 2 DAY)
FROM utilisateurs u, vehicules v
WHERE u.email = 'camille.roux@fleetmanager.fr'
  AND v.immatriculation = 'II-567-JJ';

INSERT INTO commandes (utilisateur_id, vehicule_id, statut, date_commande)
SELECT u.id, v.id, 'confirmee', DATE_SUB(NOW(), INTERVAL 6 DAY)
FROM utilisateurs u, vehicules v
WHERE u.email = 'lucas.martin@fleetmanager.fr'
  AND v.immatriculation = 'JJ-890-KK';

-- ── 5. SUIVI DES DÉPENSES JOURNALIÈRES ────────────
-- Pour chaque nouveau véhicule, on insère 10 jours de suivi
-- (les 10 derniers jours, avec des valeurs réalistes)

-- Toyota Yaris (AA-123-BB)
INSERT IGNORE INTO suivi_depenses (vehicule_id, date_jour, kilometrage, carburant_qte, carburant_prix, peages_frais, entretien_type, entretien_cout, notes)
SELECT v.id, DATE_SUB(CURDATE(), INTERVAL n DAY),
  12000 + (10 - n) * 80,
  ROUND(4.5 + RAND() * 3, 2),
  ROUND(7.0 + RAND() * 5, 2),
  IF(n % 3 = 0, ROUND(3.5 + RAND() * 2, 2), NULL),
  NULL, NULL,
  CONCAT('Trajet jour J-', n)
FROM vehicules v, (
  SELECT 1 AS n UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5
  UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9 UNION SELECT 10
) days
WHERE v.immatriculation = 'AA-123-BB';

-- Volkswagen Golf (BB-456-CC)
INSERT IGNORE INTO suivi_depenses (vehicule_id, date_jour, kilometrage, carburant_qte, carburant_prix, peages_frais, entretien_type, entretien_cout, notes)
SELECT v.id, DATE_SUB(CURDATE(), INTERVAL n DAY),
  28000 + (10 - n) * 95,
  ROUND(5.5 + RAND() * 3, 2),
  ROUND(9.0 + RAND() * 6, 2),
  IF(n % 4 = 0, ROUND(4.0 + RAND() * 3, 2), NULL),
  NULL, NULL,
  CONCAT('Trajet Golf J-', n)
FROM vehicules v, (
  SELECT 1 AS n UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5
  UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9 UNION SELECT 10
) days
WHERE v.immatriculation = 'BB-456-CC';

-- Renault Mégane (CC-789-DD)
INSERT IGNORE INTO suivi_depenses (vehicule_id, date_jour, kilometrage, carburant_qte, carburant_prix, peages_frais, entretien_type, entretien_cout, notes)
SELECT v.id, DATE_SUB(CURDATE(), INTERVAL n DAY),
  41000 + (10 - n) * 70,
  ROUND(4.0 + RAND() * 2.5, 2),
  ROUND(6.5 + RAND() * 4, 2),
  NULL,
  IF(n = 5, 'Vidange', NULL),
  IF(n = 5, 89.00, NULL),
  CONCAT('Trajet Mégane J-', n)
FROM vehicules v, (
  SELECT 1 AS n UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5
  UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9 UNION SELECT 10
) days
WHERE v.immatriculation = 'CC-789-DD';

-- Peugeot 3008 (DD-012-EE)
INSERT IGNORE INTO suivi_depenses (vehicule_id, date_jour, kilometrage, carburant_qte, carburant_prix, peages_frais, entretien_type, entretien_cout, notes)
SELECT v.id, DATE_SUB(CURDATE(), INTERVAL n DAY),
  8500 + (10 - n) * 60,
  ROUND(3.0 + RAND() * 2, 2),
  ROUND(5.0 + RAND() * 3, 2),
  IF(n % 2 = 0, ROUND(5.0 + RAND() * 4, 2), NULL),
  NULL, NULL,
  CONCAT('Trajet 3008 J-', n)
FROM vehicules v, (
  SELECT 1 AS n UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5
  UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9 UNION SELECT 10
) days
WHERE v.immatriculation = 'DD-012-EE';

-- BMW Série 1 (EE-345-FF)
INSERT IGNORE INTO suivi_depenses (vehicule_id, date_jour, kilometrage, carburant_qte, carburant_prix, peages_frais, entretien_type, entretien_cout, notes)
SELECT v.id, DATE_SUB(CURDATE(), INTERVAL n DAY),
  19000 + (10 - n) * 110,
  ROUND(6.0 + RAND() * 4, 2),
  ROUND(11.0 + RAND() * 7, 2),
  IF(n % 3 = 0, ROUND(6.0 + RAND() * 5, 2), NULL),
  IF(n = 8, 'Révision complète', NULL),
  IF(n = 8, 320.00, NULL),
  CONCAT('Trajet BMW J-', n)
FROM vehicules v, (
  SELECT 1 AS n UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5
  UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9 UNION SELECT 10
) days
WHERE v.immatriculation = 'EE-345-FF';

-- Mercedes Classe A (FF-678-GG)
INSERT IGNORE INTO suivi_depenses (vehicule_id, date_jour, kilometrage, carburant_qte, carburant_prix, peages_frais, entretien_type, entretien_cout, notes)
SELECT v.id, DATE_SUB(CURDATE(), INTERVAL n DAY),
  32000 + (10 - n) * 85,
  ROUND(5.0 + RAND() * 3.5, 2),
  ROUND(8.5 + RAND() * 5, 2),
  IF(n % 5 = 0, ROUND(4.5 + RAND() * 3, 2), NULL),
  NULL, NULL,
  CONCAT('Trajet Mercedes J-', n)
FROM vehicules v, (
  SELECT 1 AS n UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5
  UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9 UNION SELECT 10
) days
WHERE v.immatriculation = 'FF-678-GG';

-- Citroën C3 (GG-901-HH)
INSERT IGNORE INTO suivi_depenses (vehicule_id, date_jour, kilometrage, carburant_qte, carburant_prix, peages_frais, entretien_type, entretien_cout, notes)
SELECT v.id, DATE_SUB(CURDATE(), INTERVAL n DAY),
  17000 + (10 - n) * 55,
  ROUND(3.5 + RAND() * 2, 2),
  ROUND(5.5 + RAND() * 3, 2),
  NULL, NULL, NULL,
  CONCAT('Trajet C3 J-', n)
FROM vehicules v, (
  SELECT 1 AS n UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5
  UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9 UNION SELECT 10
) days
WHERE v.immatriculation = 'GG-901-HH';

-- Ford Puma (HH-234-II)
INSERT IGNORE INTO suivi_depenses (vehicule_id, date_jour, kilometrage, carburant_qte, carburant_prix, peages_frais, entretien_type, entretien_cout, notes)
SELECT v.id, DATE_SUB(CURDATE(), INTERVAL n DAY),
  5500 + (10 - n) * 75,
  ROUND(4.0 + RAND() * 2.5, 2),
  ROUND(6.0 + RAND() * 4, 2),
  IF(n % 4 = 0, ROUND(3.0 + RAND() * 2, 2), NULL),
  NULL, NULL,
  CONCAT('Trajet Puma J-', n)
FROM vehicules v, (
  SELECT 1 AS n UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5
  UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9 UNION SELECT 10
) days
WHERE v.immatriculation = 'HH-234-II';

-- Résumé
SELECT 'Données insérées avec succès !' AS Résultat;
SELECT COUNT(*) AS nb_utilisateurs FROM utilisateurs;
SELECT COUNT(*) AS nb_vehicules FROM vehicules;
SELECT COUNT(*) AS nb_commandes FROM commandes;
SELECT COUNT(*) AS nb_suivis FROM suivi_depenses;
