using System;
using System.IO;

namespace Monstecat_Desktop_Player
{
    class Settings
    {
        public static string URL = "https://www.monstercat.com/player";
        public static string pauseButton = "document.getElementsByClassName('buttons')[0].children[2].click()";
        public static bool usePauseButton = false;
        public static string prevButton = "document.getElementsByClassName('buttons')[0].children[1].click()";
        public static bool usePrevButton = true;
        public static string nextButton = "document.getElementsByClassName('buttons')[0].children[3].click()";
        public static bool useNextButton = true;
        public static string imageScr = "document.getElementsByClassName('active-song')[0].firstChild.style.backgroundImage;";
        public static string artistCountScr = "document.getElementsByClassName('active-song')[0].childNodes[1].childNodes[1].childElementCount;";
        public static string artistPreCountScr = "document.getElementsByClassName('active-song')[0].childNodes[1].childNodes[1].childNodes[";
        public static string artistPostCountScr = "].innerText;";
        public static string songTitleScr = "document.getElementsByClassName('active-song')[0].children[1].firstChild.innerText;";
        public static int songUpdatePause = 2500;
        public static int discordUpdatePause = 15000;
        public static string betweenArtist = " / ";
        public static string afterArtist = "-";
        public static string afterTitle = " by ";
        public static void createSettingsFile()
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
        public static bool readSettingsFile()
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
                pauseButton = splitButton(data[13], out usePauseButton);
                nextButton = splitButton(data[14], out useNextButton);
                prevButton = splitButton(data[15], out usePrevButton);
            }
            return true;
        }
        public static string split(string s)
        {
            if (s.Contains(":"))
            {
                return s.Split(':')[1];
            }
            return "";
        }
        public static string splitButton(string s, out bool use)
        {
            if (s.Contains(":"))
            {
                use = s.Split(':')[1] == "y";
                return s.Split(':')[2];

            }
            use = false;
            return "";
        }
        public static int splittoNumber(string s)
        {
            if (s.Contains(":") && int.TryParse(s.Split(':')[1], out int res))
            {
                return res;
            }
            return 2500;
        }
    }
}
