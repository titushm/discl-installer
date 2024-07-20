using Newtonsoft.Json;
using System.IO;

namespace Discl_Installer {
    public static class Utils {
        public static string INSTALLER_VERSION = "2.0.0";

        public class InstallerConfig {
            public string install_location = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\discl";
        }

        public static void ShowMessageBox(string title, string message) {
            Wpf.Ui.Controls.MessageBox msgbox = new Wpf.Ui.Controls.MessageBox();
            msgbox.Title = title;
            msgbox.CloseButtonText = "OK";
            msgbox.Content = message;
            msgbox.ShowDialogAsync();
        }

        public static InstallerConfig GetConfig() {
            if (!File.Exists(".\\config.json")) {
                File.CreateText(".\\config.json").Close();
            }
            string config = File.ReadAllText(".\\config.json");
            InstallerConfig ?parsed = JsonConvert.DeserializeObject<InstallerConfig>(config);
            if (parsed == null) {
                SaveConfig(new InstallerConfig());
                return new InstallerConfig();
            }
            return parsed;
        }

        public static void SaveConfig(InstallerConfig config) {
            File.WriteAllText(".\\config.json", JsonConvert.SerializeObject(config));
        }
    }
}
