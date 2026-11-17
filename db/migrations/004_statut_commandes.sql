-- ============================================================
-- FMprojet - Migration 004 : statuts des commandes
-- Base : projet_fleet_manager
--
-- Valeurs officielles (utilisées aussi par le code C#, voir
-- StatutCommande dans Commande.cs) : en_attente, confirmee, annulee
--
-- À exécuter UNE SEULE FOIS dans phpMyAdmin.
-- ============================================================

USE projet_fleet_manager;

-- 1. Les statuts vides ou NULL (valeurs invalides insérées par le passé) → en attente
UPDATE commandes SET statut = 'en_attente' WHERE statut IS NULL OR statut = '';

-- 2. Interdire NULL pour éviter que ça se reproduise
ALTER TABLE commandes
    MODIFY COLUMN statut ENUM('en_attente','confirmee','annulee') NOT NULL DEFAULT 'en_attente';

SELECT statut, COUNT(*) AS nb FROM commandes GROUP BY statut;
