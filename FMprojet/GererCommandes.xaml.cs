using FMprojet.Database;
using FMprojet.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FMprojet.Views
{
    public partial class GererCommandes : Window
    {
        private readonly DatabaseManager _dbManager = new();
        private List<Commande> _toutesLesCommandes = new();
        private bool _triAscendant = false;
        private string _colonneTri = "DateCommande";

        public GererCommandes()
        {
            InitializeComponent();
            Loaded += (_, _) => ChargerCommandes();
        }

        // ─── Chargement des données ───────────────────────────────────
        private void ChargerCommandes()
        {
            _toutesLesCommandes = _dbManager.ObtenirTousLesCommandes();
            AppliquerFiltres();
        }

        // ─── Filtrage + tri ───────────────────────────────────────────
        private void AppliquerFiltres()
        {
            IEnumerable<Commande> resultats = _toutesLesCommandes;

            // Filtre texte
            string recherche = txtRecherche.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(recherche))
            {
                resultats = resultats.Where(c =>
                    c.UtilisateurNom.ToLower().Contains(recherche) ||
                    c.VehiculeMarqueModele.ToLower().Contains(recherche) ||
                    c.Statut.ToLower().Contains(recherche));
            }

            // Filtre date
            if (dpFiltrDate.SelectedDate.HasValue)
            {
                var date = dpFiltrDate.SelectedDate.Value.Date;
                resultats = resultats.Where(c => c.DateCommande.Date == date);
            }

            // Tri
            resultats = _colonneTri switch
            {
                "Id"                   => _triAscendant ? resultats.OrderBy(c => c.Id)                   : resultats.OrderByDescending(c => c.Id),
                "UtilisateurNom"       => _triAscendant ? resultats.OrderBy(c => c.UtilisateurNom)       : resultats.OrderByDescending(c => c.UtilisateurNom),
                "VehiculeMarqueModele" => _triAscendant ? resultats.OrderBy(c => c.VehiculeMarqueModele) : resultats.OrderByDescending(c => c.VehiculeMarqueModele),
                _                      => _triAscendant ? resultats.OrderBy(c => c.DateCommande)         : resultats.OrderByDescending(c => c.DateCommande),
            };

            var liste = resultats.ToList();
            listCommandes.ItemsSource = liste;

            // État vide
            bool vide = liste.Count == 0;
            panneauVide.Visibility   = vide ? Visibility.Visible : Visibility.Collapsed;
            listCommandes.Visibility = vide ? Visibility.Collapsed : Visibility.Visible;

            // Barre de statut
            int total   = _toutesLesCommandes.Count;
            int affiches = liste.Count;
            txtStatutBarre.Text = vide
                ? "Aucune commande ne correspond aux critères de recherche."
                : $"{affiches} commande(s) affichée(s) sur {total} au total  •  " +
                  $"En attente : {_toutesLesCommandes.Count(c => c.Statut == "En attente")}  " +
                  $"Approuvées : {_toutesLesCommandes.Count(c => c.Statut == "Approuvée")}  " +
                  $"Refusées : {_toutesLesCommandes.Count(c => c.Statut == "Refusée")}";

            txtCompteurCommandes.Text = $"{total} commande(s) au total";
        }

        // ─── Événements filtres ───────────────────────────────────────
        private void Filtre_Changed(object sender, EventArgs e) => AppliquerFiltres();

        private void BtnReinitFiltres_Click(object sender, RoutedEventArgs e)
        {
            txtRecherche.Text = "";
            dpFiltrDate.SelectedDate = null;
            AppliquerFiltres();
        }

        // ─── Tri par colonne ──────────────────────────────────────────
        private void ColonneEntete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is GridViewColumnHeader header && header.Tag is string tag)
            {
                if (_colonneTri == tag)
                    _triAscendant = !_triAscendant;
                else
                {
                    _colonneTri = tag;
                    _triAscendant = true;
                }
                AppliquerFiltres();
            }
        }

        // ─── Actions Approuver / Refuser ──────────────────────────────
        private void BtnApprouver_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                bool ok = _dbManager.MettreAJourStatutCommande(id, "Approuvée");
                if (ok)
                {
                    MessageBox.Show("Commande approuvée avec succès.", "Succès",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    ChargerCommandes();
                }
            }
        }

        private void BtnRefuser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var confirm = MessageBox.Show(
                    "Êtes-vous sûr de vouloir refuser cette commande ?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    bool ok = _dbManager.MettreAJourStatutCommande(id, "Refusée");
                    if (ok)
                    {
                        MessageBox.Show("Commande refusée.", "Information",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                        ChargerCommandes();
                    }
                }
            }
        }

        // ─── Navigation ───────────────────────────────────────────────
        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            var accueil = new Accueil();
            accueil.Show();
            Close();
        }
    }
}
