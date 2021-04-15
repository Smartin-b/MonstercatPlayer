using System;
using System.Windows.Forms;
using System.IO;
using System.Threading;

using CefSharp;
using CefSharp.WinForms;
using DiscordRPC;
using DiscordRPC.Logging;
using System.Net;
using System.Threading.Tasks;

namespace Monstecat_Desktop_Player
{
    public partial class Form1 : Form
    {
        public ChromiumWebBrowser browser;
        public DiscordRpcClient discord;
        public string currentSong = "";
        public DateTime songStart = DateTime.UtcNow;


        public void InitSettings()
        {
            if (!Settings.readSettingsFile()){
                Settings.createSettingsFile();
                Settings.readSettingsFile();
            }
        }
        public void InitBrowser()
        {
            CefSettings settings = new CefSettings();
            settings.CachePath = Environment.CurrentDirectory + @"\CEF";
            Cef.Initialize(settings);
            browser = new ChromiumWebBrowser(Settings.URL);
            this.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;
        }
        public void InitDiscord()
        {
            discord = new DiscordRpcClient("679284045141770250");
            discord.Logger = new ConsoleLogger() { Level = LogLevel.Warning };
            discord.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
            };

            discord.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Received Update! {0}", e.Presence);
            };
            discord.Initialize();
        }
        public void InitButtons()
        {
            if (Settings.usePrevButton)
                browser.GetMainFrame().ExecuteJavaScriptAsync("navigator.mediaSession.setActionHandler('previoustrack', () => " + Settings.prevButton + ");");
            if (Settings.usePauseButton)
            {
                browser.GetMainFrame().ExecuteJavaScriptAsync("navigator.mediaSession.setActionHandler('pause', () => " + Settings.pauseButton + ");");
                browser.GetMainFrame().ExecuteJavaScriptAsync("navigator.mediaSession.setActionHandler('play', () => " + Settings.pauseButton + ");");
            }
            if (Settings.useNextButton)
                browser.GetMainFrame().ExecuteJavaScriptAsync("navigator.mediaSession.setActionHandler('nexttrack', () => " + Settings.nextButton + ");");

        }
        public async void browserUpdateThread()
        {
            while (true)
            {
                if (browser.IsBrowserInitialized)
                {
                    InitButtons();
                    InitSettings();
                    try
                    {
                        string songTitle = await getSongTitle();
                        if (currentSong != songTitle)
                        {
                            //only executes on SongUpdate
                            songStart = DateTime.UtcNow;
                            currentSong = songTitle;
                            String Artists = await getArtists();
                            #pragma warning disable CS4014
                            pullImage();//waiting on this call is not needed, it writes directly onto file if posible
                            #pragma warning restore CS4014
                            WriteFile("SongTitle.txt", songTitle + Settings.afterTitle);
                            WriteFile("Artists.txt", Artists + Settings.afterArtist);
                            WriteFile("songinfo.txt", songTitle + Settings.afterTitle + Artists + Settings.afterArtist);
                        }
                    }
                    catch
                    {
                        currentSong = "";
                        songStart = DateTime.UtcNow;
                        Console.WriteLine("Error grabbing song text. Probably hasn't loaded yet.");
                    }
                }

                Thread.Sleep(Settings.songUpdatePause);
            }
        }
        public void DiscordUpdateThread()
        {
            {
                while (true)
                {
                    if (discord.IsInitialized)
                    {
                        Assets discordAsset = new Assets()
                        {
                            LargeImageKey = "monstercat_logo_trans",
                            LargeImageText = "Monstercat"
                        };
                        if (currentSong != "")
                        {
                            discord.SetPresence(new RichPresence()
                            {
                                Details = "Now Playing",
                                State = currentSong,
                                Timestamps = new Timestamps()
                                {
                                    Start = songStart
                                },
                                Assets = discordAsset
                            });
                        }
                        else
                        {
                            discord.SetPresence(new RichPresence()
                            {
                                Details = "Nothing Playing",
                                Assets = discordAsset
                            });
                        }
                    }

                    Thread.Sleep(Settings.discordUpdatePause);
                }
            };
        }

        private async Task<string> getSongTitle()
        {
            //maybe at Code to prevent crahes if TitlePull fails
            var songTitleresult = await browser.GetMainFrame()
                .EvaluateScriptAsync("(function() { return " + Settings.songTitleScr + " })();", null);
            string songtitle = (string)songTitleresult.Result;
            return songtitle;
        }

        private async Task<string> getArtists()
        {
            string Artists = "";
            var ArtistCount = await browser.GetMainFrame()
                .EvaluateScriptAsync("(function() { return " + Settings.artistCountScr + " })();", null);
            string s = "" + ArtistCount.Result;
            if (int.TryParse(s, out int count))
            {
                for (int i = 0; i < count; i++)
                {
                    var currentArtist = await browser.GetMainFrame()
                        .EvaluateScriptAsync("(function() { return " + Settings.artistPreCountScr + i + Settings.artistPostCountScr + "})();", null);
                    Artists += currentArtist.Result + Settings.betweenArtist;
                }
                Artists = Artists.Substring(0, Artists.Length - Settings.betweenArtist.Length);
            }

            return Artists;
        }

        private async Task pullImage()
        {
            var songimagescr = await browser.GetMainFrame()
                .EvaluateScriptAsync("(function() { return " + Settings.imageScr + "})();", null);
            String imagescr = (string)songimagescr.Result;
            if (imagescr != null && imagescr.Contains("\""))
            {
                imagescr = imagescr.Split('"')[1];
                Console.WriteLine(imagescr);
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(new Uri(imagescr), Path.Combine(Settings.DataFilePath(), "SongImage.png"));
                }
            }
        }

        private void WriteFile(string filename,string infile)
        {
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(Settings.DataFilePath(), filename)))
            {
                outputFile.Write(infile);
            }
        }

        public Form1()
        {
            InitializeComponent();
            InitBrowser();
            InitDiscord();
            InitSettings();

            Thread songThread = new Thread(browserUpdateThread);
            Thread discordThread = new Thread(DiscordUpdateThread);

            songThread.IsBackground = true;
            songThread.Start();
            discordThread.IsBackground = true;
            discordThread.Start();
        }
    }
}
