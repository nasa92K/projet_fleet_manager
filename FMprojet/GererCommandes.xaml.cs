using FMprojet.Database;
using FMprojet.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
            try
            {
                _toutesLesCommandes = _dbManager.ObtenirTousLesCommandes() ?? new List<Commande>();
                AppliquerFiltres();
            }
            catch (Exception ex)
            {
                _toutesLesCommandes = new List<Commande>();
                AppliquerFiltres();

                if (txtStatutBarre != null)
                    txtStatutBarre.Text = "Impossible de charger les commandes (base de données indisponible).";

                MessageBox.Show(
                    $"Erreur lors du chargement des commandes : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ─── Filtrage + tri ───────────────────────────────────────────
        private void AppliquerFiltres()
        {
            if (!IsLoaded) return;

            IEnumerable<Commande> resultats = _toutesLesCommandes;

            // Filtre texte
            string recherche = txtRecherche?.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(recherche))
            {
                resultats = resultats.Where(c =>
                    (c.UtilisateurNom?.Contains(recherche, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.VehiculeMarqueModele?.Contains(recherche, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    c.StatutLibelle.Contains(recherche, StringComparison.OrdinalIgnoreCase));
            }

            // Filtre date
            if (dpFiltrDate?.SelectedDate != null)
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
            if (listCommandes != null)
                listCommandes.ItemsSource = liste;

            // État vide
            bool vide = liste.Count == 0;
            if (panneauVide != null)
                panneauVide.Visibility = vide ? Visibility.Visible : Visibility.Collapsed;
            
            if (listCommandes != null)
                listCommandes.Visibility = vide ? Visibility.Collapsed : Visibility.Visible;

            // Barre de statut
            int total = _toutesLesCommandes.Count;
            int affiches = liste.Count;
            
            if (txtStatutBarre != null)
            {
                txtStatutBarre.Text = vide
                    ? "Aucune commande ne correspond aux critères de recherche."
                    : $"{affiches} commande(s) affichée(s) sur {total} au total  •  " +
                      $"En attente : {_toutesLesCommandes.Count(c => c.Statut == StatutCommande.EnAttente)}  " +
                      $"Confirmées : {_toutesLesCommandes.Count(c => c.Statut == StatutCommande.Confirmee)}  " +
                      $"Annulées : {_toutesLesCommandes.Count(c => c.Statut == StatutCommande.Annulee)}";
            }

            if (txtCompteurCommandes != null)
                txtCompteurCommandes.Text = $"{total} commande(s) au total";
        }

        // ─── Événements filtres ───────────────────────────────────────
        private void Filtre_Changed(object sender, RoutedEventArgs e) => AppliquerFiltres();

        private void BtnReinitFiltres_Click(object sender, RoutedEventArgs e)
        {
            if (txtRecherche != null) txtRecherche.Text = "";
            if (dpFiltrDate != null) dpFiltrDate.SelectedDate = null;
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
                try
                {
                    bool ok = _dbManager.MettreAJourStatutCommande(id, StatutCommande.Confirmee);
                    if (ok)
                    {
                        MessageBox.Show("Commande approuvée avec succès.", "Succès",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                        ChargerCommandes();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'approbation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    try
                    {
                        bool ok = _dbManager.MettreAJourStatutCommande(id, StatutCommande.Annulee);
                        if (ok)
                        {
                            MessageBox.Show("Commande refusée.", "Information",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                            ChargerCommandes();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors du refus : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
