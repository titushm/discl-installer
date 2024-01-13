using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace discl_installer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private Version InstallerVersion = new Version("1.0.0");
        private string DisclReleasesUrl = "https://api.github.com/repos/titushm/discl/releases";
        private string DisclInterceptorReleasesUrl = "https://api.github.com/repos/titushm/discl-interceptor/releases";
        private string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private string DiscordPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\discord";
        private string DisclPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\discl\discl-main";

        public MainWindow() {
            InitializeComponent();
        }

        private void Log(string msg) {
            LogTextBlock.Text = msg;
        }

        private void cleanUpDiscord() {
            foreach (var process in Process.GetProcessesByName("Discord")) {
                process.Kill();
            }
            foreach (var process in Process.GetProcessesByName("Update")) {
                process.Kill();
            }
        }

        private JObject GetLatestDisclRelease() {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "Discl Installer");
            HttpResponseMessage response = client.GetAsync(DisclReleasesUrl + "/latest").Result;
            JObject release = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            return release;
        }

        private JObject GetLatestInterceptorRelease() {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "Discl Installer");
            HttpResponseMessage response = client.GetAsync(DisclInterceptorReleasesUrl + "/latest").Result;
            JObject release = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            return release;
        }

        private JObject GetDisclConfig() {
            JObject config = JObject.Parse(File.ReadAllText(DisclPath + @"\src\config.json"));
            return config;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e) {
            InstallerVersionTextBlock.Text = InstallerVersion.ToString();
            if (!Directory.Exists(DisclPath)) {
                InstallButton.IsEnabled = true;
            }
            JObject release = GetLatestDisclRelease();
            string latestVersion = release["tag_name"].ToString();
            string releaseDate = release["published_at"].ToString();
            VersionTextBlock.Text = latestVersion;
            ReleasedTextBlock.Text = releaseDate;
            if (File.Exists(DisclPath + @"\src\config.json")) {
                JObject config = GetDisclConfig();
                string version = "Could not get version. Please reinstall discl.";
                if (config.ContainsKey("version")) {
                    version = config["version"].ToString();
                }
                InstalledVersionTextBlock.Text = version;
                if (new Version(version) < new Version(latestVersion)) {
                    InstallButton.IsEnabled = true;
                    InstallButton.Content = "Update";
                }
            }

        }

        private void UninstallButton_Click(object sender, RoutedEventArgs e) {
            Log("Starting uninstall");
            cleanUpDiscord();
            if (Directory.Exists(LocalAppData + @"\discl")) {
                Log("Deleting discl directory");
                Directory.Delete(LocalAppData + @"\discl", true);
            }
            if (Directory.Exists(DiscordPath) && File.Exists(DiscordPath + @"\Update.exe") && File.Exists(DiscordPath + @"\Update_Discord.exe")) {
                Log("Restoring Discord Update.exe");
                File.Delete(DiscordPath + @"\Update.exe");
                File.Move(DiscordPath + @"\Update_Discord.exe", DiscordPath + @"\Update.exe");
            }
            Log("Uninstall complete.");
            InstallButton.IsEnabled = true;
            InstallButton.Content = "Install";
            InstalledVersionTextBlock.Text = "";
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e) {
            Log("Starting install");
            cleanUpDiscord();
            if (!Directory.Exists(LocalAppData + @"\discl")) {
                Log("Creating discl directory");
                Directory.CreateDirectory(LocalAppData + @"\discl");
            }
            JObject latestRelease = GetLatestDisclRelease();
            string downloadUrl = latestRelease["zipball_url"].ToString();
            Log("Downloading discl");
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "Discl Installer");
            HttpResponseMessage response = client.GetAsync(downloadUrl).Result;
            string zipPath = LocalAppData + @"\discl\discl.zip";
            File.WriteAllBytes(zipPath, response.Content.ReadAsByteArrayAsync().Result);
            Log("Extracting discl");
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, LocalAppData + @"\discl\discl");
            File.Delete(zipPath);
            string innerDir = Directory.GetDirectories(LocalAppData + @"\discl\discl")[0];
            Directory.Move(innerDir, DisclPath);
            Directory.Delete(LocalAppData + @"\discl\discl");
            Log("Renaming Discord Update.exe");
            File.Move(DiscordPath + @"\Update.exe", DiscordPath + @"\Update_Discord.exe");
            JObject latestInterceptorRelease = GetLatestInterceptorRelease();
            string interceptorDownloadAsset = latestInterceptorRelease["assets"][0]["url"].ToString();
            response = client.GetAsync(interceptorDownloadAsset).Result;
            string interceptorDownloadUrl = JObject.Parse(response.Content.ReadAsStringAsync().Result)["browser_download_url"].ToString();
            Log("Downloading discl-interceptor");
            HttpResponseMessage interceptorResponse = client.GetAsync(interceptorDownloadUrl).Result;
            string interceptorPath = LocalAppData + @"\discl\Update.exe";
            File.WriteAllBytes(interceptorPath, interceptorResponse.Content.ReadAsByteArrayAsync().Result);
            Log("Copying discl-interceptor to Discord");
            File.Copy(LocalAppData + @"\discl\Update.exe", DiscordPath + @"\Update.exe");
            File.Delete(LocalAppData + @"\discl\Update.exe");
            Log("Installation complete.");
            InstallButton.IsEnabled = false;
            InstallButton.Content = "Installed";
            InstalledVersionTextBlock.Text = latestRelease["tag_name"].ToString();


        }
    }
}