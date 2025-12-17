using Microsoft.Win32;
using System.IO;
using System.Windows; // MessageBox

namespace MultiLogViewer.Services
{
    public class WpfUserDialogService : IUserDialogService
    {
        public string? OpenFileDialog()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Log files (*.log)|*.log|All files (*.*)|*.*",
                InitialDirectory = Directory.GetCurrentDirectory()
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return null;
        }

        public void ShowError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
