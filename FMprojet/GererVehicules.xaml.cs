using FMprojet.Database;
using FMprojet.Models;
using System;
using System.Windows;

namespace FMprojet.Views
{
    public partial class GererVehicules : Window
    {
        private readonly DatabaseManager _dbManager = new();

        public GererVehicules()
        {
            InitializeComponent();
            Loaded += (_, _) => ChargerVehicules();
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
            listVehicules.ItemsSource = _dbManager.ObtenirTousLesVehicules();
        }

        private void BtnModifierVehicule_Click(object sender, RoutedEventArgs e)
        {
            if (listVehicules.SelectedItem is not Vehicule vehiculeSelectionne)
            {
                MessageBox.Show("Veuillez sélectionner un véhicule à modifier.");
                return;
            }

            MessageBox.Show($"Fonctionnalité de modification à implémenter pour le véhicule {vehiculeSelectionne.Id}.");
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

            var success = _dbManager.SupprimerVehicule(vehiculeSelectionne.Id);
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
