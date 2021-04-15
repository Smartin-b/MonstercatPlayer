using System;
using System.Windows.Forms;
using System.IO;
using System.Threading;

using CefSharp;
using CefSharp.WinForms;
using DiscordRPC;
using DiscordRPC.Logging;
using System.Net;


namespace Monstecat_Desktop_Player
{
    public partial class Form1 : Form
    {
        public ChromiumWebBrowser browser;
        public DiscordRpcClient discord;
        public string URL = "https://www.monstercat.com/player";
        public string pauseButton = "document.getElementsByClassName('buttons')[0].children[2].click()";
        public bool usePauseButton = false;
        public string prevButton = "document.getElementsByClassName('buttons')[0].children[1].click()";
        public bool usePrevButton = true;
        public string nextButton = "document.getElementsByClassName('buttons')[0].children[3].click()";
        public bool useNextButton = true;
        public string imageScr = "document.getElementsByClassName('active-song')[0].firstChild.style.backgroundImage;";
        public string artistCountScr = "document.getElementsByClassName('active-song')[0].childNodes[1].childNodes[1].childElementCount;";
        public string artistPreCountScr = "document.getElementsByClassName('active-song')[0].childNodes[1].childNodes[1].childNodes[";
        public string artistPostCountScr = "].innerText;";
        public string songTitleScr = "document.getElementsByClassName('active-song')[0].children[1].firstChild.innerText;";
        public int songUpdatePause = 2500;
        public int discordUpdatePause = 15000;



        public string currentSong = "";
        public string currentSongTime = "0";
        public DateTime songStart = DateTime.UtcNow;

        public string betweenArtist = "";
        public string afterArtist = "";
        public string afterTitle = "";

        public void initSettings()
        {
            if (!readSettingsFile())
            {
                createSettingsFile();
                readSettingsFile();
            }
        }
        public void createSettingsFile()
        {
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "settings.txt")))
            {
                outputFile.Write(
                    "Zwischen Artists :" + betweenArtist + "\n" +
                    "Nach Artists : " + afterArtist + "\n" +
                    "Nach Title : " + afterTitle + "\n" +
                    "SongUpdateDelay : " + songUpdatePause + "\n" +
                    "DiscordUpdateDelay : " + discordUpdatePause + "\n" +
                    "Javascript stuff falls Website sich ändert:" + "\n" +
                    "URL :" + URL + "\n" +
                    "Artist Count :" + artistCountScr + "\n" +
                    "Artist PreCount :" + artistPreCountScr + "\n" +
                    "Artist PostCount :" + artistPostCountScr + "\n" +
                    "SongTitle :" + songTitleScr + "\n" +
                    "SongImage :" + imageScr + "\n" +
                    "MediaKeys , use y in between :: to force enable" + "\n" +
                    "Play/Pause :" + "n" + ":" + pauseButton + "\n" +
                    "Next Track :" + "y" + ":" + nextButton + "\n" +
                    "Prev Track :" + "y" + ":" + prevButton + "\n"
                    );
            }
        }
        public bool readSettingsFile()
        {
            using (StreamReader file = new StreamReader(Path.Combine(Environment.CurrentDirectory, "settings.txt")))
            {
                var zeilensprung = "\n".ToCharArray();
                string[] data = file.ReadToEnd().Split(zeilensprung);
                if (data.Length < 16)
                {
                    return false;
                }
                betweenArtist = split(data[0]);
                afterArtist = split(data[1]);
                afterTitle = split(data[2]);
                songUpdatePause = splittoNumber(data[3]);
                discordUpdatePause = splittoNumber(data[4]);
                URL = split(data[6]);
                artistCountScr = split(data[7]);
                artistPreCountScr = split(data[8]);
                artistPostCountScr = split(data[9]);
                songTitleScr = split(data[10]);
                imageScr = split(data[11]);
                pauseButton = splitButton(data[13],out usePauseButton);
                nextButton = splitButton(data[14], out useNextButton);
                prevButton = splitButton(data[15], out usePrevButton);
            }
            return true;
        }
        public string split(string s)
        {
            if (s.Contains(":"))
            {
                return s.Split(':')[1];
            }
            return "";
        }
        public string splitButton(string s,out bool use)
        {
            if (s.Contains(":"))
            {
                use = s.Split(':')[1] == "y";
                return s.Split(':')[2];

            }
            use = false;
            return "";
        }
        public int splittoNumber(string s)
        {
            if (s.Contains(":") && int.TryParse(s.Split(':')[1], out int res))
            {
                return res;
            }
            return 2500;
        }
        public void InitBrowser()
        {
            CefSettings settings = new CefSettings();
            settings.CachePath = Environment.CurrentDirectory + @"\CEF";
            Cef.Initialize(settings);
            browser = new ChromiumWebBrowser(URL);
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
        public async void initButtons()
        {
            if (usePrevButton)
                browser.GetMainFrame().ExecuteJavaScriptAsync("navigator.mediaSession.setActionHandler('previoustrack', () => " + prevButton + ");");
            if (usePauseButton)
            {
                browser.GetMainFrame().ExecuteJavaScriptAsync("navigator.mediaSession.setActionHandler('pause', () => " + pauseButton + ");");
                browser.GetMainFrame().ExecuteJavaScriptAsync("navigator.mediaSession.setActionHandler('play', () => " + pauseButton + ");");
            }
            if (useNextButton)
                browser.GetMainFrame().ExecuteJavaScriptAsync("navigator.mediaSession.setActionHandler('nexttrack', () => " + nextButton + ");");

        }

        public Form1()
        {
            InitializeComponent();
            InitBrowser();
            InitDiscord();
            initSettings();

            Thread songThread = new Thread(async () =>
            {
            bool t = true;
            while (true)
            {
                if (browser.IsBrowserInitialized)
                {
                    if (t)
                    {
                            Console.WriteLine("starting buttonsetup");
                            t = false;
                        }
                        initButtons();
                        try
                        {
                            initSettings();
                            var songTitle = await browser.GetMainFrame()
                                .EvaluateScriptAsync("(function() { return "+songTitleScr+" })();", null);
                            if (currentSong != (string)songTitle.Result)
                            {
                                songStart = DateTime.UtcNow;
                                currentSong = (string)songTitle.Result;
                                //Get Artists
                                String Artists = "";
                                var ArtistCount = await browser.GetMainFrame()
                                    .EvaluateScriptAsync("(function() { return "+artistCountScr+" })();", null);
                                string s = "" + ArtistCount.Result;
                                int count = 0;
                                if (int.TryParse(s, out count))
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        var currentArtist = await browser.GetMainFrame()
                                            .EvaluateScriptAsync("(function() { return "+ artistPreCountScr + i + artistPostCountScr + "})();", null);
                                        Artists += currentArtist.Result + betweenArtist;
                                    }
                                    Artists = Artists.Substring(0, Artists.Length - betweenArtist.Length);
                                }
                                var songimagescr = await browser.GetMainFrame()
                                    .EvaluateScriptAsync("(function() { return "+ imageScr + "})();", null);
                                String imagescr = (string)songimagescr.Result;
                                if (imagescr != null && imagescr.Contains("\"")){
                                    imagescr = imagescr.Split('"')[1];
                                    Console.WriteLine(imagescr);
                                    using (WebClient client = new WebClient())
                                    {
                                        client.DownloadFile(new Uri(imagescr), Path.Combine(Environment.CurrentDirectory, "SongImage.png"));
                                    }
                                }
                                using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "SongTitle.txt")))
                                {
                                    outputFile.Write(songTitle.Result + afterTitle);
                                }
                                using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "Artists.txt")))
                                {
                                    outputFile.Write(Artists + afterArtist);
                                }
                                using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "songinfo.txt")))
                                {
                                    outputFile.Write(songTitle.Result + afterTitle + Artists + afterArtist);
                                }
                            }
                        }
                        catch
                        {
                            currentSong = "";
                            songStart = DateTime.UtcNow;
                            Console.WriteLine("Error grabbing song text. Probably hasn't loaded yet.");
                        }
                    }

                    Thread.Sleep(songUpdatePause);
                }
            });

            Thread discordThread = new Thread(async () =>
            {
                while (true)
                {
                    if (discord.IsInitialized)
                    {
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
                                Assets = new Assets()
                                {
                                    LargeImageKey = "monstercat_logo_trans",
                                    LargeImageText = "Monstercat"
                                }
                            });
                        }
                        else
                        {
                            discord.SetPresence(new RichPresence()
                            {
                                Details = "Nothing Playing",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "monstercat_logo_trans",
                                    LargeImageText = "Monstercat"
                                }
                            });
                        }
                    }

                    Thread.Sleep(discordUpdatePause);
                }
            });

            songThread.IsBackground = true;
            songThread.Start();
            discordThread.IsBackground = true;
            discordThread.Start();
        }
    }
}
