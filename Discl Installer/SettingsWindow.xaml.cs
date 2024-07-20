using Microsoft.Win32;
using System.IO;
using Wpf.Ui.Controls;


namespace Discl_Installer {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : FluentWindow {
        public SettingsWindow() {
            InitializeComponent();
        }
        private void FluentWindow_Loaded(object sender, System.Windows.RoutedEventArgs e) {
            InstallLocationButton.Content = Utils.GetConfig().install_location;
            InstallerVersionText.Text = Utils.INSTALLER_VERSION;
        }

        private void InstallLocationButton_Click(object sender, System.Windows.RoutedEventArgs e) {
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.Title = "Select Install Location";
            dialog.InitialDirectory = Utils.GetConfig().install_location;
            if (dialog.ShowDialog() == true) {
                string[] files = Directory.GetFiles(dialog.FolderName);
                string[] directories = Directory.GetDirectories(dialog.FolderName);
                if (files.Length > 0 || directories.Length > 0) {
                    Utils.ShowMessageBox("Directory Not Empty", "The selected directory is not empty. Please select an empty directory.");
                    return;
                }
                Utils.InstallerConfig config = Utils.GetConfig();
                config.install_location = dialog.FolderName;
                Utils.SaveConfig(config);
                InstallLocationButton.Content = dialog.FolderName;
            }
        }
    }
}
