using FMprojet.Database;
using FMprojet.Models;
using System.Collections.Generic;
using System.Windows;

namespace FMprojet.Views
{
    public partial class Vehicules : Window
    {
        private readonly DatabaseManager _dbManager = new();

        public Vehicules()
        {
            InitializeComponent();
            Loaded += (_, _) => ChargerVehicules();
        }

        private void BtnRechercher_Click(object sender, RoutedEventArgs e)
        {
            var reference = txtReference.Text?.Trim();
            var vehicules = string.IsNullOrWhiteSpace(reference)
                ? _dbManager.ObtenirTousLesVehicules()
                : _dbManager.RechercherVehiculesParReference(reference);

            ActualiserListe(vehicules);
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            var accueil = new Accueil();
            accueil.Show();
            Close();
        }

        private void ChargerVehicules()
        {
            var vehicules = _dbManager.ObtenirTousLesVehicules();
            ActualiserListe(vehicules);
        }

        private void ActualiserListe(List<Vehicule> vehicules)
        {
            listVehicules.ItemsSource = vehicules;
        }
    }
}
