using FMprojet.Database;
using FMprojet.Models;
using FMprojet;
using System;
using System.Globalization;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Windows;
using System.Windows.Controls;

namespace FMprojet.Views
{
    public partial class GererVehicules : Window
    {
        private readonly DatabaseManager _dbManager = new();
        private Vehicule? _vehiculeSelectionne;

        public GererVehicules()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                dpSuiviDate.SelectedDate = DateTime.Today;
                ChargerVehicules();
                MettreAJourPied();
            };
        }

        // ─── Chargement liste ───────────────────────────────────────
        private void ChargerVehicules()
        {
            try
            {
                var liste = _dbManager.ObtenirTousLesVehicules();
                listVehicules.ItemsSource = liste;
                txtVehiculeStatut.Text = $"{liste.Count} véhicule(s) dans le parc";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des véhicules : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MettreAJourPied()
        {
            if (txtVehiculesFooter != null)
                txtVehiculesFooter.Text = SessionManager.EstAdmin
                    ? "Gestion administrateur  •  suivi des dépenses activé"
                    : "Suivi des dépenses activé";
        }

        // ─── Sélection véhicule ─────────────────────────────────────
        private void ListVehicules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _vehiculeSelectionne = listVehicules.SelectedItem as Vehicule;
            if (_vehiculeSelectionne == null)
            {
                txtVehiculeSelection.Text = "Sélectionne un véhicule dans la liste…";
                chartVehiculeDepenses.Series = Array.Empty<ISeries>();
                return;
            }

            txtVehiculeSelection.Text =
                $"{_vehiculeSelectionne.Marque} {_vehiculeSelectionne.Modele}  •  {_vehiculeSelectionne.Immatriculation}  •  {_vehiculeSelectionne.Annee}".Trim();

            ChargerGraphiqueVehicule();
        }

        // ─── Graphique dépenses (thème sombre) ──────────────────────
        private void ChargerGraphiqueVehicule()
        {
            if (_vehiculeSelectionne == null || chartVehiculeDepenses == null) return;

            try
            {
                var points = _dbManager.ObtenirDepensesVehiculeParJour(_vehiculeSelectionne.Id, 30);
                txtVehiculeChartInfo.Text = points.Count == 0
                    ? "Aucune dépense enregistrée sur les 30 derniers jours."
                    : $"{points.Count} jour(s) avec des données";

                var labels  = points.Select(p => p.Jour.ToString("dd/MM")).ToArray();
                var valeurs = points.Select(p => (double)p.Total).ToArray();

                chartVehiculeDepenses.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values         = valeurs,
                        Name           = "Total / jour (€)",
                        GeometrySize   = 7,
                        Stroke         = new SolidColorPaint(new SKColor(61, 142, 240), 2),
                        GeometryStroke = new SolidColorPaint(new SKColor(61, 142, 240), 2),
                        GeometryFill   = new SolidColorPaint(new SKColor(28, 39, 64)),
                        Fill           = new SolidColorPaint(new SKColor(61, 142, 240, 40))
                    }
                };

                var sepPaint = new SolidColorPaint(new SKColor(31, 46, 69));
                var lblPaint = new SolidColorPaint(new SKColor(107, 127, 168));

                chartVehiculeDepenses.XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels          = labels,
                        TextSize        = 10,
                        LabelsPaint     = lblPaint,
                        SeparatorsPaint = sepPaint
                    }
                };

                chartVehiculeDepenses.YAxes = new Axis[]
                {
                    new Axis
                    {
                        Labeler         = value => value.ToString("0") + "€",
                        TextSize        = 10,
                        LabelsPaint     = lblPaint,
                        SeparatorsPaint = sepPaint
                    }
                };
            }
            catch (Exception ex)
            {
                txtVehiculeChartInfo.Text = "Erreur lors du chargement du graphique.";
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── Enregistrer suivi du jour ──────────────────────────────
        private void BtnEnregistrerSuivi_Click(object sender, RoutedEventArgs e)
        {
            if (_vehiculeSelectionne == null)
            {
                MessageBox.Show("Sélectionne d'abord un véhicule.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var date = (dpSuiviDate.SelectedDate ?? DateTime.Today).Date;

            int? km = null;
            if (!string.IsNullOrWhiteSpace(txtSuiviKm.Text))
            {
                if (int.TryParse(txtSuiviKm.Text.Trim(), out var kmVal)) km = kmVal;
                else { MessageBox.Show("Kilométrage invalide."); return; }
            }

            decimal? carbQte  = ParseDecimalOrNull(txtSuiviCarbQte.Text);
            decimal? carbPrix = ParseDecimalOrNull(txtSuiviCarbPrix.Text);
            decimal? peage    = ParseDecimalOrNull(txtSuiviPeage.Text);

            try
            {
                var suivi = new SuiviDepense
                {
                    VehiculeId    = _vehiculeSelectionne.Id,
                    DateJour      = date,
                    Kilometrage   = km,
                    CarburantQte  = carbQte,
                    CarburantPrix = carbPrix,
                    PeagesFrais   = peage
                };

                var ok = _dbManager.UpsertSuiviDepense(suivi);
                if (!ok) return;

                txtVehiculesFooter.Text =
                    $"✅  Suivi enregistré pour {_vehiculeSelectionne.Marque} {_vehiculeSelectionne.Modele} le {date:dd/MM/yyyy}";
                ChargerGraphiqueVehicule();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement du suivi : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static decimal? ParseDecimalOrNull(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ||
                decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out v))
            {
                return v;
            }
            return null;
        }

        // ─── Ajouter véhicule ───────────────────────────────────────
        private void BtnAjouterVehicule_Click(object sender, RoutedEventArgs e)
        {
            if (!ValiderChampsObligatoires(out var vehicule)) return;

            var success = _dbManager.AjouterVehicule(vehicule);
            if (success)
            {
                txtVehiculesFooter.Text = $"✅  Véhicule {vehicule.Marque} {vehicule.Modele} ajouté avec succès.";
                ChargerVehicules();
                ReinitialiserChamps();
                expanderAjout.IsExpanded = false;
            }
            else
            {
                MessageBox.Show("Erreur lors de l'ajout du véhicule.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValiderChampsObligatoires(out Vehicule vehicule)
        {
            vehicule = default!;

            if (string.IsNullOrWhiteSpace(txtMarque.Text) || string.IsNullOrWhiteSpace(txtModele.Text))
            {
                MessageBox.Show("Veuillez remplir au moins la marque et le modèle.");
                return false;
            }

            if (!int.TryParse(txtAnnee.Text, out var annee))
            {
                MessageBox.Show("Année invalide."); return false;
            }

            if (!decimal.TryParse(txtPrix.Text, NumberStyles.Number,
                CultureInfo.InvariantCulture, out var prix) &&
                !decimal.TryParse(txtPrix.Text, out prix))
            {
                MessageBox.Show("Prix invalide."); return false;
            }

            if (!int.TryParse(txtKilometrage.Text, out var kilometrage))
            {
                MessageBox.Show("Kilométrage invalide."); return false;
            }

            var categorieId = 1;
            if (!string.IsNullOrWhiteSpace(txtCategorieId.Text) &&
                !int.TryParse(txtCategorieId.Text, out categorieId))
            {
                MessageBox.Show("Catégorie invalide."); return false;
            }

            vehicule = new Vehicule
            {
                Marque          = txtMarque.Text.Trim(),
                Modele          = txtModele.Text.Trim(),
                Immatriculation = txtImmatriculation.Text.Trim(),
                Annee           = annee,
                Prix            = prix,
                Kilometrage     = kilometrage,
                Carburant       = txtCarburant.Text.Trim(),
                Transmission    = txtTransmission.Text.Trim(),
                Description     = txtDescription.Text.Trim(),
                Image           = txtImage.Text.Trim(),
                CategorieId     = categorieId,
                // Date de mise en circulation (optionnelle)
                Circulation     = dpCirculation.SelectedDate
            };
            return true;
        }

        private void BtnModifierVehicule_Click(object sender, RoutedEventArgs e)
        {
            if (listVehicules.SelectedItem is not Vehicule vehiculeSelectionne)
            {
                MessageBox.Show(
                    "⚠️  Aucun véhicule sélectionné.\n\n" +
                    "Veuillez cliquer sur un véhicule dans la liste avant d'utiliser\n" +
                    "le bouton « Modifier / Suivi ».",
                    "Sélection requise",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _vehiculeSelectionne = vehiculeSelectionne;

            try
            {
                expanderAjout.IsExpanded = true;
                txtMarque.Text = vehiculeSelectionne.Marque ?? "";
                txtModele.Text = vehiculeSelectionne.Modele ?? "";
                txtImmatriculation.Text = vehiculeSelectionne.Immatriculation ?? "";
                txtAnnee.Text = vehiculeSelectionne.Annee.ToString();
                txtPrix.Text = vehiculeSelectionne.Prix.ToString(CultureInfo.InvariantCulture);
                txtKilometrage.Text = vehiculeSelectionne.Kilometrage.ToString();
                txtCarburant.Text = vehiculeSelectionne.Carburant ?? "";
                txtTransmission.Text = vehiculeSelectionne.Transmission ?? "";
                txtDescription.Text = vehiculeSelectionne.Description ?? string.Empty;
                txtImage.Text = vehiculeSelectionne.Image ?? string.Empty;
                txtCategorieId.Text = vehiculeSelectionne.CategorieId.ToString();
                dpCirculation.SelectedDate = vehiculeSelectionne.Circulation;

                txtVehiculeSelection.Text =
                    $"{vehiculeSelectionne.Marque} {vehiculeSelectionne.Modele}  •  {vehiculeSelectionne.Immatriculation}  •  {vehiculeSelectionne.Annee}".Trim();

                txtVehiculesFooter.Text =
                    $"✏️  Champs pré-remplis pour : {vehiculeSelectionne.Marque} {vehiculeSelectionne.Modele} — " +
                    "modifiez puis cliquez sur « Modifier & Enregistrer ».";
            }
            catch (Exception exPreremplissage)
            {
                MessageBox.Show(
                    $"Erreur lors du pré-remplissage : {exPreremplissage.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            OuvrirFenetreSuivi(vehiculeSelectionne);
        }

        private void OuvrirFenetreSuivi(Vehicule vehicule)
        {
            try
            {
                var fenetreSuivi = new SuiviVehicule(vehicule)
                {
                    Owner = this
                };

                var resultat = fenetreSuivi.ShowDialog();

                if (resultat == true)
                {
                    ChargerVehicules();
                    txtVehiculesFooter.Text =
                        $"✅  Suivi mis à jour pour : {vehicule.Marque} {vehicule.Modele}.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Impossible d'ouvrir la fenêtre de suivi.\n\nDétail : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Enregistre les modifications saisies dans l'expander pour le véhicule
        /// actuellement sélectionné, en appelant _dbManager.ModifierVehicule().
        ///
        /// Ce bouton remplace l'ancien comportement "Ajouter" quand un véhicule
        /// est pré-sélectionné et que l'expander est pré-rempli.
        /// </summary>
        private void BtnModifierEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            // ── Garde : un véhicule doit être sélectionné ────────────────────
            if (_vehiculeSelectionne == null)
            {
                MessageBox.Show(
                    "⚠️  Aucun véhicule sélectionné.\n\n" +
                    "Sélectionnez un véhicule dans la liste, puis cliquez sur\n" +
                    "« Modifier / Suivi » pour pré-remplir les champs avant d'enregistrer.",
                    "Sélection requise",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                // ── Validation et parsing des champs numériques ──────────────
                if (string.IsNullOrWhiteSpace(txtMarque.Text) || string.IsNullOrWhiteSpace(txtModele.Text))
                {
                    MessageBox.Show("La marque et le modèle sont obligatoires.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtAnnee.Text, out var annee))
                {
                    MessageBox.Show("L'année saisie est invalide. Veuillez entrer un nombre entier (ex : 2021).",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtPrix.Text,
                        System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out var prix) &&
                    !decimal.TryParse(txtPrix.Text, out prix))
                {
                    MessageBox.Show("Le prix saisi est invalide. Utilisez un point ou une virgule comme séparateur décimal.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtKilometrage.Text, out var kilometrage))
                {
                    MessageBox.Show("Le kilométrage saisi est invalide. Veuillez entrer un nombre entier (ex : 45000).",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var categorieId = 1;
                if (!string.IsNullOrWhiteSpace(txtCategorieId.Text) &&
                    !int.TryParse(txtCategorieId.Text, out categorieId))
                {
                    MessageBox.Show("L'identifiant de catégorie est invalide.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ── Confirmation avant modification ──────────────────────────
                var confirmation = MessageBox.Show(
                    $"Confirmer la modification du véhicule ?\n\n" +
                    $"  Véhicule   : {_vehiculeSelectionne.Marque} {_vehiculeSelectionne.Modele}\n" +
                    $"  Ancien km  : {_vehiculeSelectionne.Kilometrage:N0} km\n" +
                    $"  Nouveau km : {kilometrage:N0} km\n" +
                    $"  Ancienne année : {_vehiculeSelectionne.Annee}\n" +
                    $"  Nouvelle année : {annee}",
                    "Confirmer la modification",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation != MessageBoxResult.Yes) return;

                // ── Mise à jour de l'objet avec les nouvelles valeurs ────────
                // On conserve l'Id pour que le WHERE id = @id soit correct
                _vehiculeSelectionne.Marque          = txtMarque.Text.Trim();
                _vehiculeSelectionne.Modele          = txtModele.Text.Trim();
                _vehiculeSelectionne.Immatriculation = txtImmatriculation.Text.Trim();
                _vehiculeSelectionne.Annee           = annee;
                _vehiculeSelectionne.Prix            = prix;
                _vehiculeSelectionne.Kilometrage     = kilometrage;
                _vehiculeSelectionne.Carburant       = txtCarburant.Text.Trim();
                _vehiculeSelectionne.Transmission    = txtTransmission.Text.Trim();
                _vehiculeSelectionne.Description     = string.IsNullOrWhiteSpace(txtDescription.Text)
                                                         ? null : txtDescription.Text.Trim();
                _vehiculeSelectionne.Image           = string.IsNullOrWhiteSpace(txtImage.Text)
                                                         ? null : txtImage.Text.Trim();
                _vehiculeSelectionne.CategorieId     = categorieId;
                // Mise à jour de la date de circulation
                _vehiculeSelectionne.Circulation     = dpCirculation.SelectedDate;

                // ── Appel à la nouvelle méthode ModifierVehicule() ───────────
                bool succes = _dbManager.ModifierVehicule(_vehiculeSelectionne);

                if (succes)
                {
                    txtVehiculesFooter.Text =
                        $"✅  Véhicule {_vehiculeSelectionne.Marque} {_vehiculeSelectionne.Modele} modifié avec succès.";
                    ChargerVehicules();         // Rafraîchit la liste
                    ReinitialiserChamps();      // Vide les champs de l'expander
                    expanderAjout.IsExpanded = false;
                }
                else
                {
                    // DatabaseManager a déjà affiché un MessageBox d'erreur SQL
                    txtVehiculesFooter.Text = "❌  La modification a échoué. Consultez le message d'erreur.";
                }
            }
            catch (Exception ex)
            {
                // Filet de sécurité global — l'application ne crashe jamais
                MessageBox.Show(
                    $"Erreur inattendue lors de la modification :\n\n{ex.Message}",
                    "Erreur critique",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ─── Supprimer ──────────────────────────────────────────────
        private void BtnSupprimerVehicule_Click(object sender, RoutedEventArgs e)
        {
            if (listVehicules.SelectedItem is not Vehicule v)
            {
                MessageBox.Show("Veuillez sélectionner un véhicule à supprimer.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Supprimer le véhicule {v.Marque} {v.Modele} (ID {v.Id}) ?",
                "Confirmer la suppression",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                bool success = _dbManager.SupprimerVehicule(v.Id);
                if (success)
                {
                    txtVehiculesFooter.Text = $"🗑  Véhicule {v.Marque} {v.Modele} supprimé.";
                    ChargerVehicules();
                }
                else
                {
                    MessageBox.Show("Échec de la suppression.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── Retour ─────────────────────────────────────────────────
        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            var accueil = new Accueil();
            accueil.Show();
            Close();
        }

        private void ReinitialiserChamps()
        {
            txtMarque.Clear(); txtModele.Clear(); txtImmatriculation.Clear();
            txtAnnee.Clear();  txtPrix.Clear();   txtKilometrage.Clear();
            txtCarburant.Clear(); txtTransmission.Clear();
            txtDescription.Clear(); txtImage.Clear();
            txtCategorieId.Text       = "1";
            dpCirculation.SelectedDate = null; // Vider la date de circulation
        }
    }
}
