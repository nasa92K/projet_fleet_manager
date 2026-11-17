using System.Windows;

namespace FMprojet
{
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += (_, args) =>
            {
                MessageBox.Show(
                    $"Une erreur inattendue s'est produite :\n\n{args.Exception.Message}",
                    "Erreur Fleet Manager",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}


