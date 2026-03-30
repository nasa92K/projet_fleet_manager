using FMprojet;
using FMprojet.Database;
using FMprojet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FMprojet.Views
{
    public partial class MesCommandes : Window
    {
        private readonly DatabaseManager _dbManager = new();
        private List<Commande> _toutesLesCommandes = new();

        public MesCommandes()
        {
            InitializeComponent();
            Loaded += (_, _) => ChargerCommandes();
        }

        private void ChargerCommandes()
        {
            if (SessionManager.UtilisateurActuel == null)
            {
                MessageBox.Show("Session expirée, veuillez vous reconnecter.");
                var connexion = new Connexion();
                connexion.Show();
                Close();
                return;
            }

            _toutesLesCommandes = _dbManager.ObtenirCommandesParUtilisateur(SessionManager.UtilisateurActuel.Id);
            AppliquerFiltre();
        }

        private void AppliquerFiltre()
        {
            string recherche = txtRecherche.Text?.Trim().ToLower() ?? "";

            var resultats = string.IsNullOrEmpty(recherche)
                ? _toutesLesCommandes
                : _toutesLesCommandes
                    .Where(c =>
                        c.VehiculeMarqueModele.ToLower().Contains(recherche) ||
                        c.Statut.ToLower().Contains(recherche))
                    .ToList();

            // Tri : plus récent en premier
            resultats = resultats.OrderByDescending(c => c.DateCommande).ToList();

            listCommandes.ItemsSource = resultats;

            bool vide = resultats.Count == 0;
            panneauVide.Visibility   = vide ? Visibility.Visible : Visibility.Collapsed;
            listCommandes.Visibility = vide ? Visibility.Collapsed : Visibility.Visible;

            int total = _toutesLesCommandes.Count;
            txtStatutBarre.Text = vide
                ? "Aucune commande ne correspond à votre recherche."
                : $"{resultats.Count} commande(s) affichée(s) sur {total}  •  " +
                  $"En attente : {_toutesLesCommandes.Count(c => c.Statut == "En attente")}  " +
                  $"Approuvées : {_toutesLesCommandes.Count(c => c.Statut == "Approuvée")}";

            txtSoustitre.Text = $"{total} commande(s) au total";
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e) => AppliquerFiltre();

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            var accueil = new Accueil();
            accueil.Show();
            Close();
        }
    }
}
