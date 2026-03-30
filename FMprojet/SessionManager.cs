using FMprojet.Models;

namespace FMprojet
{
    public static class SessionManager
    {
        private static Utilisateur? _utilisateurActuel;

        public static Utilisateur? UtilisateurActuel
        {
            get => _utilisateurActuel;
            set => _utilisateurActuel = value;
        }

        public static bool EstConnecte => _utilisateurActuel != null;

        public static bool EstSuperAdmin => _utilisateurActuel?.Role?.ToLower() == "super_admin";

        public static bool EstAdmin => _utilisateurActuel?.Role?.ToLower() == "admin" || EstSuperAdmin;

        public static bool EstUtilisateur => _utilisateurActuel?.Role?.ToLower() == "utilisateur" || _utilisateurActuel?.Role?.ToLower() == "client";

        public static void Deconnexion()
        {
            _utilisateurActuel = null;
        }
    }
}


