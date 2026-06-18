-- Migration: création de la table de suivi des dépenses par véhicule (journalier)
-- Base: MySQL / MariaDB

CREATE TABLE IF NOT EXISTS suivi_depenses (
    id                 INT AUTO_INCREMENT PRIMARY KEY,
    vehicule_id        INT NOT NULL,
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
);

CREATE INDEX idx_suivi_depenses_vehicule_date
  ON suivi_depenses(vehicule_id, date_jour);

