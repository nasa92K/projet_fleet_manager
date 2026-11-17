-- ============================================================
--  FleetManager – Trigger de validation commande
--  Base : projet_fleet_manager
--  Objectif : bloquer toute insertion dans `commandes` si le
--             véhicule commandé ne respecte pas les règles métiers :
--               • Âge du véhicule  < 4 ans
--               • Kilométrage      < 80 000 km
--
--  À exécuter UNE SEULE FOIS dans phpMyAdmin.
-- ============================================================

USE projet_fleet_manager;

-- 1. Supprimer l'ancien trigger si on ré-exécute le script (idempotent)
DROP TRIGGER IF EXISTS trg_valider_commande_vehicule;

-- 2. Changer le délimiteur pour pouvoir écrire le corps du trigger
DELIMITER $$

CREATE TRIGGER trg_valider_commande_vehicule
-- Se déclenche AVANT chaque INSERT dans commandes
BEFORE INSERT ON commandes
FOR EACH ROW
BEGIN
    -- Variables locales pour stocker les données du véhicule concerné
    DECLARE v_annee       INT;
    DECLARE v_kilometrage INT;
    DECLARE v_age         INT;
    DECLARE v_marque      VARCHAR(100);
    DECLARE v_modele      VARCHAR(100);

    -- ────────────────────────────────────────────────────────────
    --  Lecture des données du véhicule commandé
    -- ────────────────────────────────────────────────────────────
    SELECT annee, kilometrage, marque, modele
    INTO   v_annee, v_kilometrage, v_marque, v_modele
    FROM   vehicules
    WHERE  id = NEW.vehicule_id;

    -- Calcul de l'âge du véhicule (année courante - année de fabrication)
    SET v_age = YEAR(NOW()) - v_annee;

    -- ────────────────────────────────────────────────────────────
    --  Vérification règle 1 : Âge < 4 ans
    -- ────────────────────────────────────────────────────────────
    IF v_age >= 4 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = CONCAT(
            '[FleetManager] Commande refusée – Règle métier non respectée : ',
            'Le véhicule "', v_marque, ' ', v_modele, '" ',
            'a ', v_age, ' an(s) (fabriqué en ', v_annee, '). ',
            'Maximum autorisé : 3 ans (année de fabrication minimum : ',
            YEAR(NOW()) - 3, ').'
        );
    END IF;

    -- ────────────────────────────────────────────────────────────
    --  Vérification règle 2 : Kilométrage < 80 000 km
    -- ────────────────────────────────────────────────────────────
    IF v_kilometrage >= 80000 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = CONCAT(
            '[FleetManager] Commande refusée – Règle métier non respectée : ',
            'Le véhicule "', v_marque, ' ', v_modele, '" ',
            'affiche ', v_kilometrage, ' km. ',
            'Maximum autorisé : 79 999 km.'
        );
    END IF;

    -- Si aucun SIGNAL n'a été déclenché → l'INSERT est autorisé et s'exécute normalement

END$$

-- 3. Remettre le délimiteur par défaut
DELIMITER ;

-- ────────────────────────────────────────────────────────────────────────
--  Vérification : lister les triggers actifs sur la base
-- ────────────────────────────────────────────────────────────────────────
SHOW TRIGGERS FROM projet_fleet_manager;
