using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace Nocturn_AutoUpdater
{
    public partial class Nocturn : Form
    {
        private const string GITHUB_API_URL = "https://api.github.com/repos/NiordFresh/Nocturn/releases/latest";
        private const string VERSION_FILE = "ver.ini";
        private const string TARGET_FOLDER = @"C:\Program Files\Nocturn";

        public Nocturn()
        {
            InitializeComponent();
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.MarqueeAnimationSpeed = 30;
            this.FormBorderStyle = FormBorderStyle.None;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.DoubleBuffer, true);
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Paint += Nocturn_Paint;
            this.Load += Nocturn_Load;
            this.Shown += Nocturn_Shown;

            CheckForUpdates();
        }

        private void Nocturn_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.Black, 1))
            {
                Rectangle rect = new Rectangle(0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private void CenterFormWithTaskbar()
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(
                workingArea.Left + (workingArea.Width - this.Width) / 2,
                workingArea.Top + (workingArea.Height - this.Height) / 2
            );
        }

        private void Nocturn_Shown(object sender, EventArgs e)
        {
            CenterFormWithTaskbar();
        }

        private async void CheckForUpdates()
        {
            try
            {
                await Task.Delay(1000);

                label1.Text = "Checking for updates...";

                string jsonResponse = await DownloadString(GITHUB_API_URL);

                var serializer = new JavaScriptSerializer();
                var release = serializer.Deserialize<dynamic>(jsonResponse);

                string latestVersion = release["tag_name"];

                string downloadUrl = "";
                string assetName = "";

                foreach (var asset in release["assets"])
                {
                    string name = asset["name"].ToString().ToLower();
                    if (name.EndsWith(".zip") && !name.Contains("source"))
                    {
                        downloadUrl = asset["browser_download_url"];
                        assetName = asset["name"];
                        break;
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    throw new Exception("No .zip file found to download");
                }

                string currentVersion = GetCurrentVersion();

                if (currentVersion != latestVersion)
                {
                    KillNocturnProcess();

                    label1.Text = "Downloading...";

                    string zipPath = Path.Combine(Path.GetTempPath(), assetName);
                    await DownloadFile(downloadUrl, zipPath);

                    label1.Text = "Installing...";

                    if (!Directory.Exists(TARGET_FOLDER))
                    {
                        Directory.CreateDirectory(TARGET_FOLDER);
                    }

                    ZipFile.ExtractToDirectory(zipPath, TARGET_FOLDER);

                    SaveCurrentVersion(latestVersion);

                    File.Delete(zipPath);

                    label1.Text = "Update completed!";
                    progressBar1.Style = ProgressBarStyle.Blocks;
                    progressBar1.Value = 100;

                    await Task.Delay(2500);
                    Application.Exit();
                }
                else
                {
                    label1.Text = "Already up to date!";
                    progressBar1.Style = ProgressBarStyle.Blocks;
                    progressBar1.Value = 100;

                    await Task.Delay(2500);
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                label1.Text = "Error occurred!";
                progressBar1.Style = ProgressBarStyle.Blocks;

                await Task.Delay(2500);
                Application.Exit();
            }
        }

        private Task<string> DownloadString(string url)
        {
            return Task.Run(() =>
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Nocturn-AutoUpdater");
                    return client.DownloadString(url);
                }
            });
        }

        private Task DownloadFile(string url, string destinationPath)
        {
            return Task.Run(() =>
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Nocturn-AutoUpdater");
                    client.DownloadFile(url, destinationPath);
                }
            });
        }

        private string GetCurrentVersion()
        {
            string versionFilePath = Path.Combine(TARGET_FOLDER, VERSION_FILE);

            if (File.Exists(versionFilePath))
            {
                return File.ReadAllText(versionFilePath).Trim();
            }

            return "0.0.0";
        }

        private void SaveCurrentVersion(string version)
        {
            string versionFilePath = Path.Combine(TARGET_FOLDER, VERSION_FILE);
            File.WriteAllText(versionFilePath, version);
        }

        private void KillNocturnProcess()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("nocturn"))
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch
            {
            }
        }

        private void Nocturn_Load(object sender, EventArgs e)
        {
            CenterFormWithTaskbar();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}