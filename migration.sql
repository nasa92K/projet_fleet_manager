-- ============================================================
-- FMprojet - Script de migration
-- À exécuter UNE SEULE FOIS dans phpMyAdmin sur la base
-- projet_fleet_manager
-- ============================================================

USE projet_fleet_manager;

-- 1. Mettre à jour les valeurs de statut existantes vers les valeurs ENUM
--    (au cas où des données en anglais existent)
UPDATE commandes SET statut = 'En attente'  WHERE LOWER(statut) IN ('pending', 'en attente', '');
UPDATE commandes SET statut = 'Approuvée'   WHERE LOWER(statut) IN ('approved', 'approuvée', 'approuve', 'validé', 'validee');
UPDATE commandes SET statut = 'Refusée'     WHERE LOWER(statut) IN ('refused', 'refusée', 'refuse', 'rejeté');
UPDATE commandes SET statut = 'Terminée'    WHERE LOWER(statut) IN ('completed', 'terminée', 'terminee', 'finished');
-- Tout statut non reconnu → En attente par défaut
UPDATE commandes SET statut = 'En attente'
WHERE statut NOT IN ('En attente', 'Approuvée', 'Refusée', 'Terminée');

-- 2. Modifier la colonne statut en ENUM
ALTER TABLE commandes
    MODIFY COLUMN statut ENUM('En attente','Approuvée','Refusée','Terminée')
        NOT NULL DEFAULT 'En attente';

-- 3. Supprimer les contraintes FK existantes si elles existent déjà
--    (évite les erreurs si on ré-exécute le script)
ALTER TABLE commandes
    DROP FOREIGN KEY IF EXISTS fk_commandes_utilisateur;

ALTER TABLE commandes
    DROP FOREIGN KEY IF EXISTS fk_commandes_vehicule;

-- 4. Ajouter les clés étrangères
ALTER TABLE commandes
    ADD CONSTRAINT fk_commandes_utilisateur
        FOREIGN KEY (utilisateur_id) REFERENCES utilisateurs(id)
        ON DELETE CASCADE ON UPDATE CASCADE;

ALTER TABLE commandes
    ADD CONSTRAINT fk_commandes_vehicule
        FOREIGN KEY (vehicule_id) REFERENCES vehicules(id)
        ON DELETE CASCADE ON UPDATE CASCADE;

-- 5. Index utiles pour les performances
ALTER TABLE commandes
    ADD INDEX IF NOT EXISTS idx_commandes_statut (statut),
    ADD INDEX IF NOT EXISTS idx_commandes_date   (date_commande);

SELECT 'Migration terminée avec succès.' AS resultat;
