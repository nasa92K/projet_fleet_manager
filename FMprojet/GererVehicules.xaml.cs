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

        private void BtnAjouterVehicule_Click(object sender, RoutedEventArgs e)
        {
            if (!ValiderChampsObligatoires(out var vehicule))
            {
                return;
            }

            var success = _dbManager.AjouterVehicule(vehicule);

            if (success)
            {
                MessageBox.Show("Véhicule ajouté avec succès.");
                ChargerVehicules();
                ReinitialiserChamps();
                return;
            }

            MessageBox.Show("Erreur lors de l'ajout du véhicule.");
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
                MessageBox.Show("Année invalide.");
                return false;
            }

            if (!decimal.TryParse(txtPrix.Text, out var prix))
            {
                MessageBox.Show("Prix invalide.");
                return false;
            }

            if (!int.TryParse(txtKilometrage.Text, out var kilometrage))
            {
                MessageBox.Show("Kilométrage invalide.");
                return false;
            }

            var categorieId = 1;
            if (!string.IsNullOrWhiteSpace(txtCategorieId.Text) && !int.TryParse(txtCategorieId.Text, out categorieId))
            {
                MessageBox.Show("Catégorie invalide.");
                return false;
            }

            vehicule = new Vehicule
            {
                Marque = txtMarque.Text,
                Modele = txtModele.Text,
                Immatriculation = txtImmatriculation.Text,
                Annee = annee,
                Prix = prix,
                Kilometrage = kilometrage,
                Carburant = txtCarburant.Text,
                Transmission = txtTransmission.Text,
                Description = txtDescription.Text,
                Image = txtImage.Text,
                CategorieId = categorieId
            };

            return true;
        }

        private void ChargerVehicules()
        {
            try
            {
                var liste = _dbManager.ObtenirTousLesVehicules();
                listVehicules.ItemsSource = liste;
                txtVehiculeStatut.Text = $"{liste.Count} véhicule(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des véhicules : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MettreAJourPied()
        {
            if (txtVehiculesFooter != null)
                txtVehiculesFooter.Text = SessionManager.EstAdmin ? "Gestion administrateur • suivi des dépenses activé" : "Suivi des dépenses activé";
        }

        private void ListVehicules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _vehiculeSelectionne = listVehicules.SelectedItem as Vehicule;
            if (_vehiculeSelectionne == null)
            {
                txtVehiculeSelection.Text = "Sélectionne un véhicule…";
                chartVehiculeDepenses.Series = Array.Empty<ISeries>();
                return;
            }

            txtVehiculeSelection.Text = $"{_vehiculeSelectionne.Marque} {_vehiculeSelectionne.Modele} • {_vehiculeSelectionne.Immatriculation}".Trim();
            ChargerGraphiqueVehicule();
        }

        private void ChargerGraphiqueVehicule()
        {
            if (_vehiculeSelectionne == null || chartVehiculeDepenses == null) return;

            try
            {
                var points = _dbManager.ObtenirDepensesVehiculeParJour(_vehiculeSelectionne.Id, 30);
                txtVehiculeChartInfo.Text = points.Count == 0 ? "Aucune donnée de suivi sur 30 jours." : "";

                var labels = points.Select(p => p.Jour.ToString("dd/MM")).ToArray();
                var valeurs = points.Select(p => (double)p.Total).ToArray();

                chartVehiculeDepenses.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = valeurs,
                        Name = "Total / jour",
                        GeometrySize = 6,
                        Fill = null
                    }
                };

                chartVehiculeDepenses.XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = labels,
                        TextSize = 11,
                        SeparatorsPaint = new SolidColorPaint(new SKColor(230, 235, 240))
                    }
                };

                chartVehiculeDepenses.YAxes = new Axis[]
                {
                    new Axis
                    {
                        Labeler = value => value.ToString("0"),
                        TextSize = 11,
                        SeparatorsPaint = new SolidColorPaint(new SKColor(230, 235, 240))
                    }
                };
            }
            catch (Exception ex)
            {
                txtVehiculeChartInfo.Text = "Erreur lors du chargement du graphique.";
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEnregistrerSuivi_Click(object sender, RoutedEventArgs e)
        {
            if (_vehiculeSelectionne == null)
            {
                MessageBox.Show("Sélectionne d'abord un véhicule.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var date = (dpSuiviDate.SelectedDate ?? DateTime.Today).Date;

            int? km = null;
            if (!string.IsNullOrWhiteSpace(txtSuiviKm.Text))
            {
                if (int.TryParse(txtSuiviKm.Text.Trim(), out var kmVal)) km = kmVal;
                else { MessageBox.Show("Kilométrage invalide."); return; }
            }

            decimal? carbQte = ParseDecimalOrNull(txtSuiviCarbQte.Text);
            decimal? carbPrix = ParseDecimalOrNull(txtSuiviCarbPrix.Text);
            decimal? peage = ParseDecimalOrNull(txtSuiviPeage.Text);

            try
            {
                var suivi = new SuiviDepense
                {
                    VehiculeId = _vehiculeSelectionne.Id,
                    DateJour = date,
                    Kilometrage = km,
                    CarburantQte = carbQte,
                    CarburantPrix = carbPrix,
                    PeagesFrais = peage
                };

                var ok = _dbManager.UpsertSuiviDepense(suivi);
                if (!ok) return;

                txtVehiculesFooter.Text = $"Suivi enregistré pour {_vehiculeSelectionne.Marque} {_vehiculeSelectionne.Modele} ({date:dd/MM/yyyy}).";
                ChargerGraphiqueVehicule();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement du suivi : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void BtnModifierVehicule_Click(object sender, RoutedEventArgs e)
        {
            if (listVehicules.SelectedItem is not Vehicule vehiculeSelectionne)
            {
                MessageBox.Show("Veuillez sélectionner un véhicule à modifier.");
                return;
            }

            try
            {
                var suivi = new SuiviVehicule(vehiculeSelectionne)
                {
                    Owner = this
                };
                var changed = suivi.ShowDialog() == true;
                if (changed)
                {
                    ChargerVehicules();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du suivi : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSupprimerVehicule_Click(object sender, RoutedEventArgs e)
        {
            if (listVehicules.SelectedItem is not Vehicule vehiculeSelectionne)
            {
                MessageBox.Show("Veuillez sélectionner un véhicule à supprimer.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Supprimer le véhicule {vehiculeSelectionne.Marque} {vehiculeSelectionne.Modele} (ID {vehiculeSelectionne.Id}) ?",
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            bool success;
            try
            {
                success = _dbManager.SupprimerVehicule(vehiculeSelectionne.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (success)
            {
                MessageBox.Show("Véhicule supprimé.");
                ChargerVehicules();
            }
            else
            {
                MessageBox.Show("Échec de la suppression.");
            }
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            var accueil = new Accueil();
            accueil.Show();
            Close();
        }

        private void ReinitialiserChamps()
        {
            txtMarque.Clear();
            txtModele.Clear();
            txtImmatriculation.Clear();
            txtAnnee.Clear();
            txtPrix.Clear();
            txtKilometrage.Clear();
            txtCarburant.Clear();
            txtTransmission.Clear();
            txtDescription.Clear();
            txtImage.Clear();
            txtCategorieId.Text = "1";
        }
    }
}
