-- ============================================================
-- FMprojet - Migration 003 : clés étrangères en CASCADE + InnoDB
-- Base : projet_fleet_manager
--
-- Objectif : supprimer un véhicule (ou un utilisateur) supprime
-- automatiquement toutes ses données liées, côté base.
--
-- À exécuter UNE SEULE FOIS dans phpMyAdmin.
-- ============================================================

USE projet_fleet_manager;

-- 1. Passer les tables MyISAM en InnoDB (MyISAM ne gère ni FK ni transactions)
ALTER TABLE historique_vehicule ENGINE = InnoDB;
ALTER TABLE suivi_depenses      ENGINE = InnoDB;
ALTER TABLE suivi_vehicules     ENGINE = InnoDB;

-- 2. Aligner les types des colonnes (une FK exige le même type, signé/non signé)
ALTER TABLE utilisateurs        MODIFY COLUMN id          INT UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE historique_vehicule MODIFY COLUMN vehicule_id INT UNSIGNED NOT NULL;
ALTER TABLE suivi_depenses      MODIFY COLUMN vehicule_id INT UNSIGNED NOT NULL;
ALTER TABLE suivi_vehicules     MODIFY COLUMN vehicule_id INT UNSIGNED NOT NULL;

-- 3. Supprimer les lignes orphelines (sinon l'ajout des FK échoue)
DELETE c FROM commandes c
  LEFT JOIN utilisateurs u ON u.id = c.utilisateur_id
 WHERE u.id IS NULL;

DELETE s FROM suivi_depenses s
  LEFT JOIN vehicules v ON v.id = s.vehicule_id
 WHERE v.id IS NULL;

DELETE h FROM historique_vehicule h
  LEFT JOIN vehicules v ON v.id = h.vehicule_id
 WHERE v.id IS NULL;

DELETE s FROM suivi_vehicules s
  LEFT JOIN vehicules v ON v.id = s.vehicule_id
 WHERE v.id IS NULL;

-- 4. Remplacer les anciennes FK (NO ACTION) par des FK en CASCADE
ALTER TABLE commandes DROP FOREIGN KEY commandes_ibfk_2;
ALTER TABLE images    DROP FOREIGN KEY images_ibfk_1;
ALTER TABLE paiements DROP FOREIGN KEY paiements_ibfk_1;

ALTER TABLE commandes
    ADD CONSTRAINT fk_commandes_vehicule
        FOREIGN KEY (vehicule_id) REFERENCES vehicules(id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    ADD CONSTRAINT fk_commandes_utilisateur
        FOREIGN KEY (utilisateur_id) REFERENCES utilisateurs(id)
        ON DELETE CASCADE ON UPDATE CASCADE;

ALTER TABLE paiements
    ADD CONSTRAINT fk_paiements_commande
        FOREIGN KEY (commande_id) REFERENCES commandes(id)
        ON DELETE CASCADE ON UPDATE CASCADE;

ALTER TABLE images
    ADD CONSTRAINT fk_images_vehicule
        FOREIGN KEY (vehicule_id) REFERENCES vehicules(id)
        ON DELETE CASCADE ON UPDATE CASCADE;

ALTER TABLE historique_vehicule
    ADD CONSTRAINT fk_historique_vehicule_vehicules
        FOREIGN KEY (vehicule_id) REFERENCES vehicules(id)
        ON DELETE CASCADE ON UPDATE CASCADE;

ALTER TABLE suivi_depenses
    ADD CONSTRAINT fk_suivi_depenses_vehicules
        FOREIGN KEY (vehicule_id) REFERENCES vehicules(id)
        ON DELETE CASCADE ON UPDATE CASCADE;

ALTER TABLE suivi_vehicules
    ADD CONSTRAINT fk_suivi_vehicules_vehicules
        FOREIGN KEY (vehicule_id) REFERENCES vehicules(id)
        ON DELETE CASCADE ON UPDATE CASCADE;

SELECT 'Migration 003 terminée avec succès.' AS resultat;
