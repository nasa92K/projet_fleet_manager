using FMprojet.Database;
using FMprojet.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace FMprojet.Views
{
    public partial class SuiviVehicule : Window
    {
        private readonly DatabaseManager _dbManager = new();
        private readonly Vehicule _vehicule;

        public bool ADesChangements { get; private set; }

        public SuiviVehicule(Vehicule vehicule)
        {
            // Assigner avant InitializeComponent : les ComboBox/DatePicker
            // déclenchent SelectionChanged/SelectedDateChanged à l'init et appellent
            // ChargerHistorique() qui utilise _vehicule.Id.
            _vehicule = vehicule ?? throw new ArgumentNullException(nameof(vehicule));
            InitializeComponent();
            Loaded += (_, _) =>
            {
                try
                {
                    Initialiser();
                }
                catch (Exception ex)
                {
                    txtStatut.Text = "Impossible d'initialiser le suivi.";
                    MessageBox.Show(
                        $"Erreur lors de l'ouverture du suivi :\n\n{ex.Message}",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            };
        }

        private void Initialiser()
        {
            txtTitreVehicule.Text = $"Suivi véhicule • ID {_vehicule.Id}";
            txtSousTitre.Text = $"{_vehicule.Marque} {_vehicule.Modele}  •  {_vehicule.Immatriculation}".Trim();

            txtKm.Text = _vehicule.Kilometrage.ToString(CultureInfo.InvariantCulture);
            txtCarb.Text = _vehicule.Carburant ?? "";

            dpDateEvent.SelectedDate = DateTime.Today;
            ChargerHistorique();
        }

        private void ChargerHistorique()
        {
            if (_vehicule == null) return;

            try
            {
                var type = LireFiltreType();
                var depuis = dpDepuis.SelectedDate?.Date;
                var items = _dbManager.ObtenirHistoriqueVehicule(_vehicule.Id, type, depuis);

                listHistorique.ItemsSource = items;
                txtCompteur.Text = $"{items.Count} événement(s)";

                txtStatut.Text = items.Count == 0
                    ? "Aucun événement pour ce véhicule (ou filtres trop restrictifs)."
                    : "Historique chargé.";
            }
            catch (Exception ex)
            {
                txtStatut.Text = "Erreur lors du chargement de l'historique.";
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string? LireFiltreType()
        {
            if (cbFiltreType.SelectedItem is not ComboBoxItem item) return null;
            var val = (item.Content?.ToString() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(val) || val == "(tous)") return null;
            return val;
        }

        private void BtnEnregistrerSnapshot_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtKm.Text.Trim(), out var km))
            {
                MessageBox.Show("Kilométrage invalide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var carb = txtCarb.Text.Trim();
            var notes = string.IsNullOrWhiteSpace(txtNotesSnapshot.Text) ? null : txtNotesSnapshot.Text.Trim();

            try
            {
                var ok = _dbManager.MettreAJourVehiculeAvecHistorique(_vehicule.Id, km, carb, notes);
                if (!ok)
                {
                    txtStatut.Text = "Aucune mise à jour effectuée.";
                    return;
                }

                _vehicule.Kilometrage = km;
                _vehicule.Carburant = carb;
                ADesChangements = true;

                txtNotesSnapshot.Text = "";
                txtStatut.Text = "Changements enregistrés (historique + snapshot).";
                ChargerHistorique();
            }
            catch (Exception ex)
            {
                txtStatut.Text = "Erreur lors de l'enregistrement.";
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAjouterEvent_Click(object sender, RoutedEventArgs e)
        {
            var type = (cbType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "entretien";
            var date = (dpDateEvent.SelectedDate ?? DateTime.Today).Date;

            decimal? valeurNum = null;
            if (!string.IsNullOrWhiteSpace(txtValeurNum.Text))
            {
                if (decimal.TryParse(txtValeurNum.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ||
                    decimal.TryParse(txtValeurNum.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out v))
                {
                    valeurNum = v;
                }
                else
                {
                    MessageBox.Show("Valeur numérique invalide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            decimal? cout = null;
            if (!string.IsNullOrWhiteSpace(txtCout.Text))
            {
                if (decimal.TryParse(txtCout.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var c) ||
                    decimal.TryParse(txtCout.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out c))
                {
                    cout = c;
                }
                else
                {
                    MessageBox.Show("Coût invalide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var notes = string.IsNullOrWhiteSpace(txtNotesEvent.Text) ? null : txtNotesEvent.Text.Trim();

            try
            {
                var entree = new HistoriqueVehicule
                {
                    VehiculeId = _vehicule.Id,
                    Type = type,
                    ValeurNum = valeurNum,
                    Cout = cout,
                    DateEvent = date,
                    Notes = notes
                };

                var ok = _dbManager.AjouterHistoriqueVehicule(entree);
                if (!ok)
                {
                    txtStatut.Text = "Ajout à l'historique échoué.";
                    return;
                }

                ADesChangements = true;
                txtValeurNum.Text = "";
                txtCout.Text = "";
                txtNotesEvent.Text = "";
                dpDateEvent.SelectedDate = DateTime.Today;

                txtStatut.Text = "Événement ajouté à l'historique.";
                ChargerHistorique();
            }
            catch (Exception ex)
            {
                txtStatut.Text = "Erreur lors de l'ajout de l'événement.";
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filtres_Changed(object sender, RoutedEventArgs e) => ChargerHistorique();

        private void BtnResetFiltres_Click(object sender, RoutedEventArgs e)
        {
            dpDepuis.SelectedDate = null;
            cbFiltreType.SelectedIndex = 0;
            ChargerHistorique();
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ADesChangements)
                    DialogResult = true;
                else
                    Close();
            }
            catch
            {
                Close();
            }
        }
    }
}

