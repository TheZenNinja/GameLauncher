using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Forms;

namespace GameLauncher
{
    public enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }
    struct Version
    {
        public static Version zero = new Version(0, 0, 0);

        public short major, minor, patch;
        public Version(short major, short minor, short patch)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
        }
        public Version(string vers)
        {
            string[] parts = vers.Replace("v", "").Split('.');
            if (parts.Length != 3)
            {
                major = 0;
                minor = 0;
                patch = 0;
                Console.WriteLine("Version wasn't formatted correctly");
                return;
            }

            major = short.Parse(parts[0]);
            minor = short.Parse(parts[1]);
            patch = short.Parse(parts[2]);
        }
        public static bool operator ==(Version a, Version b)
        {
            if (a.major != b.major)
                return false;
            if (a.minor != b.minor)
                return false;
            if (a.patch != b.patch)
                return false;
            return true;
        }
        public static bool operator !=(Version a, Version b) => !(a == b);
        public override string ToString() => $"{major}.{minor}.{patch}";
    }
    public partial class GameLauncher : Form
    {
        //https://www.youtube.com/watch?v=JIjZQo03YdA&feature=youtu.be
        private string rootPath;
        private string versionFile;
        private string gameZipPath;
        private string gameExePath;
        private string gameName = "Game.exe";

        private string versionDL = "https://github.com/TheZenNinja/2DRoguelike/releases/download/ExtDL/Version.txt";
        private string gameDL = "https://github.com/TheZenNinja/2DRoguelike/releases/download/ExtDL/Build.zip";

        private LauncherStatus _status;
        public LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        btnPlay.Text = "Play";
                        break;
                    default:
                    case LauncherStatus.failed:
                        btnPlay.Text = "Update Failed - Retry";
                        break;
                    case LauncherStatus.downloadingGame:
                        btnPlay.Text = "Downloading Game";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        btnPlay.Text = "Downloading Update";
                        break;
                }
            }
        }

        public GameLauncher()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            Console.WriteLine(rootPath);
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZipPath = Path.Combine(rootPath, "Build.zip");
            gameExePath = Path.Combine(rootPath, "Build", gameName);

            CheckForUpdates();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (File.Exists(gameExePath) && Status == LauncherStatus.ready)
            {
                //ProcessStartInfo startInfo = new ProcessStartInfo(gameExePath);
                //startInfo.WorkingDirectory = Path.Combine(rootPath, "Build");
                //Process.Start(startInfo);
                MessageBox.Show("Launched Game");
                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
        }
        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                lblVersion.Text = "V" + localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString(versionDL));
                    Console.WriteLine(onlineVersion.ToString());
                    if (onlineVersion != localVersion)
                        InstallGameFiles(true, onlineVersion);
                    else
                        Status = LauncherStatus.ready;
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
                InstallGameFiles(false, Version.zero);
        }
        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString(versionDL));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri(gameDL), gameZipPath, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }
        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZipPath, rootPath, true);
                File.Delete(gameZipPath);

                File.WriteAllText(versionFile, onlineVersion);

                lblVersion.Text = "V" + onlineVersion;
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }

    }
}
