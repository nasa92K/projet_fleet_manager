using FMprojet.Database;
using FMprojet.Models;
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
                CategorieId     = categorieId
            };
            return true;
        }

        // ─── Modifier / Suivi ───────────────────────────────────────
        private void BtnModifierVehicule_Click(object sender, RoutedEventArgs e)
        {
            if (listVehicules.SelectedItem is not Vehicule v)
            {
                MessageBox.Show("Veuillez sélectionner un véhicule à modifier.");
                return;
            }

            try
            {
                var suivi = new SuiviVehicule(v) { Owner = this };
                if (suivi.ShowDialog() == true) ChargerVehicules();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du suivi : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
            txtCategorieId.Text = "1";
        }
    }
}
