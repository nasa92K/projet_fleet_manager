-- Migration: création de la table d'historique véhicule
-- Base: MySQL / MariaDB

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
);

CREATE INDEX idx_hist_vehicule_date
  ON historique_vehicule(vehicule_id, date_event);

CREATE INDEX idx_hist_vehicule_type_date
  ON historique_vehicule(vehicule_id, type, date_event);

