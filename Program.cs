using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using MimeKit;
using File = Google.Apis.Drive.v3.Data.File;
using System.Diagnostics;

namespace OTP_Test
{
    class Program
    {
        //Nazwa programu ktora sie wyswietla jak pyta o zalogowanie do google
        static string[] Scopes = { DriveService.Scope.DriveReadonly, DriveService.Scope.Drive, DriveService.Scope.DriveFile, };
        static string ApplicationName = "OTP Test";

        static void Main(string[] args)
        {
            //Zdobycie autoryzacji do uzywania google drive
            DriveService _service = Autoryzacja();

            //zapytanie, czy to jest pierwsze uruchomienie
            PierwszeUruchomienie();

            //Glowna petla zeby mozna bylo wyslac i odczytac bez ponownego uruchomienia programu
            int i = 0;
            while (i != 1)
            {
                //Wybor zastosowania programu
                Console.Clear();
                Console.WriteLine("Wpisz 1 aby wysłać wiadomość, 2 aby ją odebrać.");
                String input = "";
                String wiadomosc = "";

                input = Console.ReadLine(); //czytanie inputu

                if (input == "1")
                {
                    // wprowadzanie wiadomosci
                    wiadomosc = WpiszWiadomosc();

                    //Zamiana stringu na ciag binarny
                    String binarny = StringToBinary(wiadomosc);

                    //generowanie szyfru
                    String szyfr = Klucz(binarny);

                    //szyfrowanie wiadomosci
                    String zaszyfrowana = XOR(binarny, szyfr);

                    //zapis do pliku
                    String nazwaPliku = Zapis(zaszyfrowana);

                    //wysylanie pliku
                    Wysylanie(_service, nazwaPliku);
                }
                else
                {
                    if (input == "2")
                    {
                        //szukanie dostepnych plikow
                        String fileId = SzukajPlikow(_service);

                        //pobieranie pliku tekstowego
                        String zaszyfrowana = SciaganiePliku();

                        //wprowadzenie klucza
                        String klucz = CzytanieKlucza();

                        //Odszyfrowanie wiadomości
                        String odszyfrowana = XOR(zaszyfrowana, klucz);

                        //Zamiana z powrotem na tekst
                        BinaryToString(odszyfrowana);
                    }
                }

                //Sprawdzanie czy użytkownik chce wyjść
                i = Wyjscie();
            }
        }

        public static string StringToBinary(string data) // funkcja na zamiane string->ciag binarny
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in data.ToCharArray())
            {
                sb.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
            }
            return sb.ToString();
        }


        public static void BinaryToString(string data) // funkcja na zamiane ciag biarny->string
        {
            List<Byte> byteList = new List<Byte>();

            for (int i = 0; i < data.Length; i += 8)
            {
                byteList.Add(Convert.ToByte(data.Substring(i, 8), 2));
            }

            //Wyswietlenie wiadomosci
            Console.Clear();
            Console.WriteLine("Odszyfrowana wiadomość:");
            Console.WriteLine("----------");
            Console.WriteLine("{0}", Encoding.ASCII.GetString(byteList.ToArray()));
            Console.WriteLine("----------");
            Console.WriteLine("Naciśnij enter.");
            Console.ReadLine();
        }


        public static string XOR(string wiadomosc, string klucz) // funkcja na szyfrowanie lub odszyfrowanie
        {
            int i = 0;
            int dlugosc = wiadomosc.Length;
            int dlugosc2 = klucz.Length;

            //sprawdzanie czy dlugosci klucza i wiadomosci sa rowne
            if (dlugosc != dlugosc2)
            {
                Console.WriteLine("Coś poszło nie tak (błędne długości klucza i wiadomości");
            }

            //petla na odszyfrowanie
            String nowytekst = "";
            while (i != dlugosc)
            {
                char x = wiadomosc[i];
                char y = klucz[i];
                int z = (x + y) % 2;
                nowytekst += z;
                i++;
            }

            //zwracanie odszyfrowanej lub zaszyfrowanej wiadomosci
            return nowytekst;
        }


        public static string WpiszWiadomosc() // funkcja na wpisywanie wiadomosci
        {
            int i = 0;
            String wiadomosc = "";
            while (i != 1)
            {
                //Wpisywanie tresci wiadomosci
                Console.Clear();
                Console.WriteLine("Wpisz wiadomość, którą chcesz wysłać.");
                wiadomosc = Console.ReadLine();
                Console.Clear();
                Console.WriteLine("Potwierdź, że to jest wiadomość do wysłania:");
                Console.WriteLine("----------");
                Console.WriteLine("{0}", wiadomosc);
                Console.WriteLine("----------");
                Console.WriteLine("Naciśnij enter aby kontynuować lub wpisz 'nie' aby ponownie wprowadzić wiadomość");
                String input = Console.ReadLine();

                // if na zakonczenie petli
                if (input != "nie")
                {
                    i++;
                }
            }
            return wiadomosc;
        }


        public static string Klucz(string wiadomosc) // funkcja na tworzenie klucza
        {
            Random rnd = new Random();
            String szyfr = "";
            int dlugosc = wiadomosc.Length; //dlugosc naszej wiadomosci przekonwertowanej na ciag binarny
            int i = 0;

            while (i != dlugosc) // petla tworzaca losowy klucz 0 i 1 o takiej samej dlugosci co wiadomosc
            {
                szyfr += rnd.Next(0, 2);
                i++;
            }

            //petla na znalezienie nieuzywanej nazwy pliku
            String lokacjaKlucza = "";
            int numerKlucza = 0;
            for(int l=0; l<1000; l++)
            {
                String nazwaKlucza = @"C:\Users\Public\TestFolder\klucz" + l + ".txt";
                if (!System.IO.File.Exists(nazwaKlucza))
                {
                    lokacjaKlucza = nazwaKlucza;
                    numerKlucza = l; 
                    break;
                }
            }

            //zapis klucza do pliku
            System.IO.File.WriteAllText(lokacjaKlucza, szyfr);

            //Wyswietlanie klucza
            numerKlucza.ToString();
            Console.Clear();
            Console.WriteLine("Klucz został zapisany do pliku\t klucz{0}.txt", numerKlucza);
            Console.WriteLine("Skopiuj go i wyślij do odbiorcy.");
            Console.WriteLine("----------");
            Console.WriteLine("Naciśnij enter.");
            Console.ReadLine();

            return szyfr;
        }


        public static string Zapis(string ZaszyfrowanaWiadomosc) // zapis wiadomosci do pliku
        {
            Console.Clear();
            Console.WriteLine("Proszę wpisać nazwę docelowego pliku.");
            String nazwa = Console.ReadLine();
            if (!nazwa.ToLower().EndsWith(".txt")) nazwa += ".txt";
            System.IO.File.WriteAllText(@"C:\Users\public\TestFolder\OTP" + nazwa, ZaszyfrowanaWiadomosc);

            Console.Clear();
            Console.WriteLine("Zaszyfrowana wiadomość została zapisana.");
            Console.WriteLine("Naciśnij enter.");
            Console.ReadLine();

            return @"C:\Users\public\TestFolder\OTP" + nazwa;
        }



        public static string CzytanieKlucza() // wprowadzanie klucza
        {
            Console.Clear();
            Console.WriteLine("Prosze podać klucz do odszyfrowania wiadomości.");
            String klucz = Console.ReadLine();
            return klucz;
        }


        public static void PierwszeUruchomienie() // instrukcja jak zrobić zeby dzialal program
        {
            Console.WriteLine("Witaj w najgorszym programie komunikacyjnym.");
            Console.WriteLine("Czy jest to Twoje pierwsze uruchomienie programu?");
            Console.WriteLine("Wpisz 'tak' aby wyświetlić instrukcję lub naciśnij enter aby kontynuować");
            string uruchomienie = Console.ReadLine();

            if (uruchomienie == "tak")
            {
                Console.Clear();
                Console.WriteLine("Aby rozpocząć działanie programu należy stworzyć nowy folder w określonym miejscu:");
                Console.WriteLine("Dysk C -> Users (Użytkownicy) -> Public (Publiczne) -> TestFolder");
                Console.WriteLine("Proszę pamiętać o dodawaniu '.txt' przy nazywaniu plików");
                Console.WriteLine("Proszę nacisnąć enter aby kontynuować");
                Console.ReadLine();
            }
        }


        public static int Wyjscie() // wychodzenie z programu
        {
            Console.Clear();
            Console.WriteLine("Czy chcesz wyłączyć program? Wpisz 'tak' aby zakończyć działanie lub naciśnij enter aby kontynuować");
            String input = Console.ReadLine();
            if (input == "tak")
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private static DriveService Autoryzacja() // autoryzacja aby uzywac google drive
        {
            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            Console.WriteLine("Naciśnij enter, aby kontynuować.");
            Console.ReadLine();
            Console.Clear();
            return service;
        }

        public static string SciaganiePliku() // sciaganie i czytanie pliku
        {
            //odpalamy drugi program zeby sciagnac z drive api v2
            Console.WriteLine("Uruchomiono drugi program");

            using (Process pProcess = new Process())
            {
                pProcess.StartInfo.FileName = @"C:\Users\Rudy\source\repos\OTP Downloader\bin\Debug\netcoreapp3.1\OTP Downloader.exe";
                pProcess.Start();
                pProcess.WaitForExit();
            }

            //odczyt zaszyfrowanej wiadomosci
            String zaszyfrowana = System.IO.File.ReadAllText(@"C:\Users\Public\TestFolder\Zaszyfrowana.txt");
            return zaszyfrowana;
        }


        public static string SzukajPlikow(DriveService _service) // szukanie plikow do sciagania
        {
            try
            {
                //czy folder istnieje
                String pageToken = null;

                var request = _service.Files.List();
                request.Q = "name contains 'OTP' and mimeType != 'application/vnd.google-apps.folder'";
                request.Fields = "nextPageToken, files(id, name,parents)";
                request.PageToken = pageToken;
                var result = request.Execute();

                if (result.Files.Count > 0)
                {
                    Console.WriteLine("Nazwa pliku: \t\t Id pliku:");
                    foreach (File file in result.Files)
                    {
                        Console.WriteLine("{0}\t\t {1}\t\t", file.Name, file.Id);
                    }
                }
                else
                {
                    Console.WriteLine("Nie znaleziono plików");
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }

            Console.WriteLine("Proszę podać Id pliku do pobrania.");
            String fileId = Console.ReadLine();
            Console.Clear();

            //sprawdzanie, czy plik ID juz istnieje
            if (System.IO.File.Exists(@"C:\Users\Public\TestFolder\ID.txt"))
            {
                // Use a try block to catch IOExceptions, to
                // handle the case of the file already being
                // opened by another process.
                try
                {
                    System.IO.File.Delete(@"C:\Users\Public\TestFolder\ID.txt");
                }
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            //zapis ID do pliku
            fileId = fileId.Trim();
            fileId = "OTP" + fileId + "OTP";
            System.IO.File.WriteAllText(@"C:\Users\Public\TestFolder\ID.txt", fileId);
            return fileId;
        }

        public static File Wysylanie(DriveService _service, string _uploadFile) // wstawianie plikow na google drive
        {
            if (System.IO.File.Exists(_uploadFile))
            {
                File body = new File();
                body.Name = Path.GetFileName(_uploadFile); // nazwa pliku
                body.MimeType = MimeTypes.GetMimeType(_uploadFile); //typ pliku
                body.Parents = new List<string> {"1X5050dfisuYvi-OCcdGAuB9mk3Iy1XrT"};
                byte[] byteArray = System.IO.File.ReadAllBytes(_uploadFile);
                MemoryStream stream = new MemoryStream(byteArray);
                try
                {
                    FilesResource.CreateMediaUpload request = _service.Files.Create(body, stream, MimeTypes.GetMimeType(_uploadFile));
                    request.SupportsTeamDrives = true;
                    request.Upload();
                    Console.WriteLine("Wstawianie zakończone pomyślnie.");
                    Console.WriteLine("Proszę nacisnąć enter aby kontynuować.");
                    Console.ReadLine();
                    return request.ResponseBody;
                }
                catch
                {
                    Console.WriteLine("Wystąpił błąd podczas wysyłania pliku.");
                    Console.WriteLine("Proszę nacisnąć enter aby kontynuować.");
                    Console.ReadLine();
                    return null;
                }
            }
            else
            {
                Console.WriteLine("Plik nie istnieje.", "404");
                Console.WriteLine("Proszę nacisnąć enter aby kontynuować.");
                Console.ReadLine();
                return null;
            }
        }
    }
}
