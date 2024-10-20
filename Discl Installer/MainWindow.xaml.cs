using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Newtonsoft.Json;
using System.Net;
using System.Windows.Documents;
using System.Reflection;

class Config {
    public string version;
}

class Asset {
    public string name;
    public string browser_download_url;
}
class VersionInfo {
    public string zipball_url;
    public string tag_name;
    public string published_at;
    public string body;
    public List<Asset> assets;
}

namespace Discl_Installer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : FluentWindow {
        string GITHUB_URL = "https://github.com/titushm/discl";
        static string LOCAL_APPDATA = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string DISCORD_PATH = LOCAL_APPDATA + "\\discord";
        string DISCL_PATH = Utils.GetConfig().install_location;
        string DISCL_DOWNLOAD_URL = "https://api.github.com/repos/titushm/discl/releases/latest";
        string OPENASAR_DOWNLOAD_URL = "https://api.github.com/repos/GooseMod/OpenAsar/releases/latest";
        string INTERCEPTOR_DOWNLOAD_URL = "https://api.github.com/repos/titushm/discl-interceptor/releases/latest";
        string PIP_DOWNLOAD_URL = "https://bootstrap.pypa.io/get-pip.py";
        Dictionary<string, string> ARCHITECHURE_MAP = new Dictionary<string, string> {
            { "X64", "https://www.python.org/ftp/python/3.12.4/python-3.12.4-embed-amd64.zip" }
        };
        string ARCHITECTURE = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
        Dictionary<string, Color> themeIconData = new Dictionary<string, Color> {
            { "Light", Colors.Black },
            { "Dark", Colors.White }
        };


        private string? GetInstalledVersion() {
            string configPath = DISCL_PATH + "\\discl-main\\src\\config.json";
            if (!File.Exists(configPath)) {
                return null;
            }
            Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
            return config.version;
        }

        private void LogToFile(string message) {
            string logPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log.txt";
            File.WriteAllText(logPath, message);
        }

        private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            LogToFile(e.ExceptionObject.ToString());
            Wpf.Ui.Controls.MessageBox msgbox = new Wpf.Ui.Controls.MessageBox();
            msgbox.Title = "Unhandled Exception";
            msgbox.PrimaryButtonText = "Open logs";
            msgbox.CloseButtonText = "OK";
            msgbox.Content = "An unhandled exception has occured";
            Task<Wpf.Ui.Controls.MessageBoxResult> result = msgbox.ShowDialogAsync();
            if (result.Result == Wpf.Ui.Controls.MessageBoxResult.Primary) {
                string logPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log.txt";
                Process.Start("notepad.exe ", logPath);
            }
        }

        private bool VolatileAction(Action action, int retries, float timeout, string message) {
            
            try {
                Dispatcher.Invoke(() => { StatusText.Text = message; });
                action();
                return true;
            } catch (Exception e) {
                if (retries == 0) {
                    Dispatcher.Invoke(() => { Utils.ShowMessageBox("Failed to complete action", e.Message); });
                    return false;
                }
                Thread.Sleep((int)(timeout));
                return VolatileAction(action, retries - 1, timeout, message);
            }
        }

        public MainWindow() {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomain_UnhandledException);
        }

        private void UpdateTheme() {
            ApplicationTheme appTheme = ApplicationThemeManager.GetAppTheme();
            Color colour = themeIconData[appTheme.ToString()];
            Resources["IconColour"] = new SolidColorBrush(colour);
            if (appTheme.ToString() == "Light") {
                DrawingImage icon = FindResource("Dark" + "Icon") as DrawingImage;
                ThemeIcon.Source = icon;
            }
            else {
                DrawingImage icon = FindResource("Light" + "Icon") as DrawingImage;
                ThemeIcon.Source = icon;
            }
        }

        private void UpdateInstallButton() {
            string? installedVersion = GetInstalledVersion();
            if (installedVersion != null) {
                bool updateAvailable = UpdateAvailable();
                if (updateAvailable) {
                    InstallButton.Content = "Update";
                    InstallButton.Tag = "update";
                    InstallButton.Background = (SolidColorBrush)Resources["Blue"];
                    InstallButton.MouseOverBackground = (SolidColorBrush)Resources["BlueHover"];
                    StatusText.Text = "To uninstall, right click the button";
                    return;
                }
            }
            if (installedVersion != null) {
                InstallButton.Content = "Uninstall";
                InstallButton.Tag = "uninstall";
                InstallButton.Background = (SolidColorBrush)Resources["Red"];
                InstallButton.MouseOverBackground = (SolidColorBrush)Resources["RedHover"];
            } else {
                InstallButton.Content = "Install";
                InstallButton.Tag = "install";
                InstallButton.Background = (SolidColorBrush)Resources["Blue"];
                InstallButton.MouseOverBackground = (SolidColorBrush)Resources["BlueHover"];
            }
        }

        private VersionInfo GetReleaseInfo(string url) {
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", "Discl-Installer");
            VersionInfo info;
            try {
                info = JsonConvert.DeserializeObject<VersionInfo>(client.DownloadString(url));
            }
            catch {
                Utils.ShowMessageBox("Failed to get latest version", "Failed to get latest version\nPlease try again later.");
                return null;
            }
            return info;
        }

        private bool UpdateAvailable() {
           string? installedVersion = GetInstalledVersion();
            if (installedVersion == null) {
                return false;
            }
            VersionInfo info = GetReleaseInfo(DISCL_DOWNLOAD_URL);
            Version latestVersion = new Version(info.tag_name);
            Version installed = new Version(installedVersion);
            return latestVersion > installed;
        }

        private async Task InstallDiscl(bool remove) {
            Dispatcher.Invoke(() => { InstallButton.IsEnabled = false; });
            bool result;
            await Task.Run(() =>
            {
                result = VolatileAction(() => {
                    foreach (var process in Process.GetProcessesByName("Discord")) {
                        process.Kill();
                    }
                }, 3, 500, "Killing discord processes");

                if (!result) return;
                result = VolatileAction(() => {
                    foreach (var process in Process.GetProcessesByName("Update")) {
                        process.Kill();
                    }
                }, 3, 500, "Killing discl processes");
                if (!result) return;

                if (remove) {
                    if (Directory.Exists(DISCL_PATH)) {
                        result = VolatileAction(() => {
                            Directory.Delete(DISCL_PATH, true);
                        }, 3, 500, "Removing data path");
                        if (!result) return;
                    }

                    if (File.Exists(DISCORD_PATH + "\\Update_Discord.exe") && File.Exists(DISCORD_PATH + "\\Update.exe")) {
                        result = VolatileAction(() => {
                            File.Delete(DISCORD_PATH + "\\Update.exe");
                        }, 3, 500, "Removing interceptor");
                        if (!result) return;

                        result = VolatileAction(() => {
                            File.Move(DISCORD_PATH + "\\Update_Discord.exe", DISCORD_PATH + "\\Update.exe");
                        }, 3, 500, "Renaming Update.exe");
                        if (!result) return;

                        result = VolatileAction(() => {
                            File.Delete(DISCORD_PATH + "\\discl_location.txt");
                        }, 3, 500, "Removing python_runtime.txt");
                        if (!result) return;
                    }
                    result = VolatileAction(() => {
                        DirectoryInfo appdata_discord = new DirectoryInfo(DISCORD_PATH);
                        foreach (DirectoryInfo dir in appdata_discord.GetDirectories()) {
                            if (dir.Name.StartsWith("app-")) {
                                File.Delete(DISCORD_PATH + "\\" + dir.Name + "\\Resources" + "\\app.asar");
                                File.Move(DISCORD_PATH + "\\" + dir.Name + "\\Resources" + "\\discord_app.asar", DISCORD_PATH + "\\" + dir.Name + "\\Resources" + "\\app.asar");
                            }
                        }
                    }, 3, 500, "Downloading OpenAsar");
                    if (!result) return;
                    Dispatcher.Invoke(() => { StatusText.Text = "Discl has been removed"; });
                }
                else {
                    if (!Directory.Exists(DISCORD_PATH)) {
                        Dispatcher.Invoke(() => { Utils.ShowMessageBox("Failed to install", "Could not find discord installation\nPlease restart the installer."); });
                        return;
                    }
                    WebClient client = new WebClient();
                    if (!ARCHITECHURE_MAP.ContainsKey(ARCHITECTURE)) {
                        Dispatcher.Invoke(() => { Utils.ShowMessageBox("Unsupported architechure", "Discl does not currently support your system architechure: " + ARCHITECTURE); });
                        return;
                    }
                    string PYTHON_DOWNLOAD_URL = ARCHITECHURE_MAP[ARCHITECTURE];
                    if (!Directory.Exists(DISCL_PATH)) {
                        result = VolatileAction(() => {
                            Directory.CreateDirectory(DISCL_PATH);
                        }, 3, 500, "Creating discl data directory");
                        if (!result) return;
                    }
                    else {
                        result = VolatileAction(() => {
                            Directory.Delete(DISCL_PATH, true);
                            Directory.CreateDirectory(DISCL_PATH);
                        }, 3, 500, "Refreshing discl data directory");
                        if (!result) {
                            Dispatcher.Invoke(() => { Utils.ShowMessageBox("Failed to access path", "Failed to access data path\nMake sure it is not in use."); });
                            return;
                        }
                    }
                    result = VolatileAction(() => {
                        client.DownloadFile(PYTHON_DOWNLOAD_URL, DISCL_PATH + "\\python.zip");
                        System.IO.Compression.ZipFile.ExtractToDirectory(DISCL_PATH + "\\python.zip", DISCL_PATH + "\\python");
                        File.Delete(DISCL_PATH + "\\python.zip");
                    }, 3, 500, "Downloading python");
                    if (!result) return;

                    result = VolatileAction(() => {
                        VersionInfo info = GetReleaseInfo(DISCL_DOWNLOAD_URL);
                        client.Headers.Add("User-Agent", "Discl-Installer");
                        client.DownloadFile(info.zipball_url, DISCL_PATH + "\\discl.zip");
                    }, 3, 500, "Downloading discl");
                    if (!result) return;

                    result = VolatileAction(() => {
                        System.IO.Compression.ZipFile.ExtractToDirectory(DISCL_PATH + "\\discl.zip", DISCL_PATH + "\\discl-temp");
                        string[] directories = Directory.GetDirectories(DISCL_PATH + "\\discl-temp");
                        if (directories.Length == 0) {
                            Dispatcher.Invoke(() => { Utils.ShowMessageBox("Failed to install", "Could not extract discl files\nPlease try again later."); });
                            return;
                        }
                        Directory.Move(directories[0], DISCL_PATH + "\\discl-main");
                        Directory.Delete(DISCL_PATH + "\\discl-temp", true);
                        File.Delete(DISCL_PATH + "\\discl.zip");
                    }, 3, 500, "Extracting discl");
                    if (!result) return;

                    result = VolatileAction(() => {
                        VersionInfo interceptorInfo = GetReleaseInfo(INTERCEPTOR_DOWNLOAD_URL);
                        if (interceptorInfo.assets.Count == 0) {
                            Dispatcher.Invoke(() => { Utils.ShowMessageBox("Failed to install", "Could not get the interceptor files\nPlease try again later."); });
                            return;
                        }
                        Asset asset = interceptorInfo.assets.Find(x => x.name == "Update.exe");
                        if (File.Exists(DISCORD_PATH + "\\Update_Discord.exe")) {
                            if (File.Exists(DISCORD_PATH + "\\Update.exe")) {
                                File.Delete(DISCORD_PATH + "\\Update.exe");
                            }
                            File.Move(DISCORD_PATH + "\\Update_Discord.exe", DISCORD_PATH + "\\Update.exe");
                        }
                        File.Move(DISCORD_PATH + "\\Update.exe", DISCORD_PATH + "\\Update_Discord.exe");
                        client.DownloadFile(asset.browser_download_url, DISCORD_PATH + "\\Update.exe");
                    }, 3, 500, "Downloading interceptor");
                    if (!result) return;

                    result = VolatileAction(() => {
                        DirectoryInfo dir = new DirectoryInfo(DISCL_PATH + "\\python");
                        foreach (FileInfo file in dir.GetFiles()) {
                            if (file.Name.EndsWith("._pth")) {
                                FileStream path = new FileStream(file.FullName, FileMode.Append);
                                StreamWriter writer = new StreamWriter(path);
                                writer.WriteLine("Lib");
                                writer.WriteLine("Lib\\site-packages");
                                writer.WriteLine(DISCL_PATH + "\\discl-main\\src");
                                writer.Close();
                                path.Close();
                            }
                        }
                    }, 3, 500, "Setting up python environment");
                    if (!result) return;

                    result = VolatileAction(() => {
                        client.DownloadFile(PIP_DOWNLOAD_URL, DISCL_PATH + "\\python\\get_pip.py");
                    }, 3, 500, "Downloading pip");
                    if (!result) return;

                    result = VolatileAction(() => {
                        Process process = new Process();
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.FileName = DISCL_PATH + "\\python\\python.exe";
                        process.StartInfo.Arguments = DISCL_PATH + "\\python\\get_pip.py";
                        process.Start();
                        string output = "";
                        while (!process.StandardOutput.EndOfStream) {
                            string line = process.StandardOutput.ReadLine();
                            Dispatcher.Invoke(() => { StatusText.Text = line; });
                            output += "\n" + line;
                        }
                        if (output.Contains("Successfully installed pip-")) {
                            Dispatcher.Invoke(() => { StatusText.Text = "Installed pip"; });
                        }
                    }, 3, 500, "Installing pip");
                    if (!result) return;

                    result = VolatileAction(() => {
                        File.Delete(DISCL_PATH + "\\python\\get_pip.py");
                    }, 3, 500, "Cleaning pip");
                    if (!result) return;

                    result = VolatileAction(() => {
                        Process process = new Process();
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.FileName = DISCL_PATH + "\\python\\python.exe";
                        process.StartInfo.Arguments = "-m pip install -r " + DISCL_PATH + "\\discl-main\\requirements.txt";
                        process.Start();
                        string output = "";
                        while (!process.StandardOutput.EndOfStream) {
                            string line = process.StandardOutput.ReadLine();
                            Dispatcher.Invoke(() => { StatusText.Text = line; });
                            output += "\n" + line;
                        }
                        Dispatcher.Invoke(() => { StatusText.Text = "Installed requirements"; });
                    }, 3, 500, "Installing requirements");
                    if (!result) return;

                    result = VolatileAction(() => {
                        StreamWriter writer = File.CreateText(DISCORD_PATH + "\\discl_location.txt");
                        writer.Write(DISCL_PATH);
                        writer.Close();
                    }, 3, 500, "Copying python_runtime.txt");
                    if (!result) return;

                    result = VolatileAction(() => {
                        VersionInfo info = GetReleaseInfo(OPENASAR_DOWNLOAD_URL);
                        Asset asset = info.assets[0];
                        DirectoryInfo appdata_discord = new DirectoryInfo(DISCORD_PATH);
                        foreach (DirectoryInfo dir in appdata_discord.GetDirectories()) {
                            if (dir.Name.StartsWith("app-")) {
                                File.Move(DISCORD_PATH + "\\" + dir.Name + "\\Resources" + "\\app.asar", DISCORD_PATH + "\\" + dir.Name + "\\Resources" + "\\discord_app.asar");
                                client.DownloadFile(asset.browser_download_url, DISCORD_PATH + "\\" + dir.Name + "\\Resources" + "\\app.asar");
                            }
                        }
                    }, 3, 500, "Downloading OpenAsar");
                    if (!result) return;
                    Dispatcher.Invoke(() => { StatusText.Text = "Discl has been installed"; });
                }
            });
            Dispatcher.Invoke(() => { UpdateInstallButton(); });
            Dispatcher.Invoke(() => { InstallButton.IsEnabled = true; });
        }

        private void UpdateLatestVersion() {
            VersionInfo info = GetReleaseInfo(DISCL_DOWNLOAD_URL);
            LatestVersionText.Text = info.tag_name;
            DateTime publishDate = DateTime.Parse(info.published_at, null, System.Globalization.DateTimeStyles.RoundtripKind);
            ReleaseDateText.Text = publishDate.ToLocalTime().ToString();
            ReleaseNotesText.Document.Blocks.Clear();
            ReleaseNotesText.Document.Blocks.Add(new Paragraph(new Run(info.body)));
        }
        private void FluentWindow_Loaded(object sender, RoutedEventArgs e) {
            ApplicationThemeManager.ApplySystemTheme();
            UpdateTheme(); 
            UpdateInstallButton();
            UpdateLatestVersion();
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e) {
            ApplicationTheme appTheme = ApplicationThemeManager.GetAppTheme();
            ApplicationTheme theme = appTheme == ApplicationTheme.Light ? ApplicationTheme.Dark : ApplicationTheme.Light;
            ApplicationThemeManager.Apply(theme);
            UpdateTheme();
        }

        private void Github_Click(object sender, RoutedEventArgs e) {
            Process.Start("explorer.exe", GITHUB_URL);
        }

        private void Info_Click(object sender, RoutedEventArgs e) {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private async void Install_Click(object sender, RoutedEventArgs e) {
            DISCL_PATH = Utils.GetConfig().install_location;
            bool remove = InstallButton.Tag.ToString() == "uninstall";
            bool update = InstallButton.Tag.ToString() == "update";
            if (update) {
                await InstallDiscl(true);
                await InstallDiscl(false);
                return;
            }
            InstallDiscl(remove);
        }

        private void Install_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (InstallButton.Tag.ToString() == "update") {
                Wpf.Ui.Controls.MessageBox msgbox = new Wpf.Ui.Controls.MessageBox();
                msgbox.Title = "Uninstall Discl";
                msgbox.PrimaryButtonText = "Uninstall";
                msgbox.CloseButtonText = "Cancel";
                msgbox.Content = "Are you sure you want to uninstall Discl?";
                Task<Wpf.Ui.Controls.MessageBoxResult> result = msgbox.ShowDialogAsync();
                if (result.Result == Wpf.Ui.Controls.MessageBoxResult.Primary) {
                    InstallDiscl(true);
                }
            }
        }
    }
}