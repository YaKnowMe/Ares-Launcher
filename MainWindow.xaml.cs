using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.ComponentModel;
using Ionic.Zip;

namespace Ares_Launcher
{

    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// 
        /// Ustawienia
        /// ====================
        public bool Konsola = true;
        public string AdresSerwera = "http://boatcraftgame.com/Launcher";
        public string NazwaPlikuWykonywalnego = "Game.exe";
        ///
        ///Zmienne prywatne
        ///=====================
        private Int32 WersjaLokalna;
        private Int32 WersjaServera;
        private int proba =1 ;
        ///
        /// 
        ///Funkcje
        ///====================
        
        public MainWindow()
        {
            InitializeComponent();
            UstawOkno();
            Inicjalizacja();
        }



        public void Log(String Wiadomosc)
        {

            // Write the string to a file.append mode is enabled so that the log
            // lines get appended to  test.txt than wiping content and writing the log

            StreamWriter file = new StreamWriter("log.txt", true);
            file.WriteLine(Wiadomosc);

            file.Close();

        }

        public void WczytajWersjeLocal()
        {
            PasekPostepu.Value = 5f;
            try
            {
                using (StreamReader fs = new StreamReader("version.data", true))
                {
                    int x;
                    Int32.TryParse(fs.ReadLine(),out x);
                    WersjaLokalna = x;
                    Log("Local version:" + WersjaLokalna.ToString());
                    Status.Content = "Local version:" + WersjaLokalna.ToString();
                    fs.Close();
                }
            }
            catch
            {
                Status.Content = "Version.data not found";
                StreamWriter file = new StreamWriter("version.data", true);
                file.WriteLine("0");
                file.Close();
                WczytajWersjeLocal();
            }

        }

        public void WczytajWersjeZSerwera()
        {
            PasekPostepu.Value = 10f;
            Status.Content = "Checking for updates...";
            try
            {
                WebClient client = new WebClient();
                Stream stream = client.OpenRead(AdresSerwera + "/version.data");
                StreamReader plik = new StreamReader(stream);
                String content = plik.ReadLine();
                int x;
                Int32.TryParse(content, out x);
                WersjaServera = x;
                Log("Server version" + WersjaServera.ToString());
            }
            catch
            {
                Status.Content = "Can't connect to update server (#" + proba + "try)";
                Log("Can't connect to update server");
                if (proba == 3)
                {
                    WersjaServera = 0;
                    return;
                }
                proba += 1;
                WczytajWersjeZSerwera();
                return;


            }
        }

        public bool PorównajWersje()
        {
            if(WersjaLokalna>= WersjaServera)
            {
                return false;
            }
            if (WersjaLokalna < WersjaServera)
            {
                return true;
            }
            //Gdy jakimś cudem oba warunki nie zostaną spełnione
            return false;
        }

        public static void UnZip(string zipFile)
        {
            if (!File.Exists(zipFile))
                throw new FileNotFoundException();

            string zipToUnpack = zipFile;
            string unpackDirectory = Directory.GetCurrentDirectory();
            using (ZipFile zip1 = ZipFile.Read(zipToUnpack))
            {
                // here, we extract every entry, but we could extract conditionally
                // based on entry name, size, date, checkbox status, etc.  
                foreach (ZipEntry e in zip1)
                {
                    e.Extract(unpackDirectory, ExtractExistingFileAction.OverwriteSilently);
                }
            }

        }

        public void Aktualizuj()
        {
            PasekPostepu.Value = 0f;
            Status.Content = "Downloading update...";
            try
            {
                Thread wątek = new Thread(() =>
                 {
                     Log("Creating async download");
                     WebClient klient = new WebClient();
                     klient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                     klient.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                     klient.DownloadFileAsync(new Uri(AdresSerwera + "/software.package"), "software.package");
                 }
                );
                wątek.Start();
                return;
            }
            catch
            {
                Log("Update failed");
                Status.Content = "Update Failed";
                return;
            }

        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            this.Dispatcher.Invoke((Action)(() =>
            {
                Status.Content = "Downloading update... (" + (e.BytesReceived/1000000).ToString("n0") + "mb/" + (e.TotalBytesToReceive / 1000000).ToString("n0") +"mb)";
                PasekPostepu.Value = int.Parse(Math.Truncate(percentage).ToString());
            }));
        }
        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                Status.Content = "Installing update package";
            }));
            UnZip("software.package");
            File.Delete("software.package");
            File.Delete("version.data");
            StreamWriter file = new StreamWriter("version.data", true);
            file.WriteLine(WersjaServera);
            file.Close();
            OdpalAplikacje();
        }

        public void OdpalAplikacje()
        {
            try
            {
                Process.Start(NazwaPlikuWykonywalnego);
                this.Dispatcher.Invoke((Action)(() =>
                {
                    System.Windows.Application.Current.Shutdown();
                }));
            }
            catch
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    Status.Content = "Game package is corrupted";
                }));
            }
        }

        public void UstawOkno()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }
       

        public void Inicjalizacja()
        {
            
            if (Konsola)
                Console.Write("Ares launcher by Jakub Tomana \n");
            //UstawOkno();
            WczytajWersjeLocal();
            WczytajWersjeZSerwera();
            bool Aktualizować = PorównajWersje();
            if(Aktualizować)
            {
                Aktualizuj();
            }
            else
            {
                OdpalAplikacje();
            }
        }

        private void Zamknij(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }


    }
}
