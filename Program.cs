using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Threading;
using File = Google.Apis.Drive.v2.Data.File;

namespace OTP_Downloader
{
    class Program
    {
        //do autoryzacji
        static string[] Scopes = { DriveService.Scope.Drive, DriveService.Scope.DriveFile,};
        static string ApplicationName = "OTP Downloader";

        static void Main(string[] args)
        {
            //autoryzacja do uzycia google drive
            var service = Authorize();

            //zdobywanie URL pliku do pobrania
            DownloadURL(service);
        }

        private static DriveService Authorize() // robienie autoryzacji
        {
            //credentials
            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
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

            return service;
        }


        public static void DownloadURL(DriveService service) // zdobywanie URL pliku do pobrania
        {
            //zdobycie ID pliku z .txt
            String fileId = System.IO.File.ReadAllText(@"C:\Users\Public\TestFolder\ID.txt");
            int dlugosc = fileId.Length - 6;
            fileId = fileId.Substring(3, dlugosc);
            Console.WriteLine("Id pliku:\t {0}", fileId);

            //sciaganie
            var request = service.Files.Get(fileId);
            var stream = new MemoryStream();

            // Add a handler which will be notified on progress changes.
            // It will notify on each chunk download and when the
            // download is completed or failed.
            request.MediaDownloader.ProgressChanged +=
                (IDownloadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading:
                            {
                                Console.WriteLine(progress.BytesDownloaded);
                                break;
                            }
                        case DownloadStatus.Completed:
                            {
                                Console.WriteLine("Pobieranie zakończone.");
                                break;
                            }
                        case DownloadStatus.Failed:
                            {
                                Console.WriteLine("Coś poszło nie tak (pobieranie przewane).");
                                break;
                            }
                    }
                };
            request.Download(stream);
            

            //odczytanie wiadomosci
            stream.Position = 0;
            StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            reader.Close();

            //usuniecie poprzedniego zaszyfrowanego pliku
            if (System.IO.File.Exists(@"C:\Users\Public\TestFolder\Zaszyfrowana.txt"))
            {
                // Use a try block to catch IOExceptions, to
                // handle the case of the file already being
                // opened by another process.
                try
                {
                    System.IO.File.Delete(@"C:\Users\Public\TestFolder\Zaszyfrowana.txt");
                }
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
            }

            //zapisanie wiadomosci do pliku
            System.IO.File.WriteAllText(@"C:\Users\Public\TestFolder\Zaszyfrowana.txt", result);
            Console.WriteLine("Zapisano plik");
            Console.WriteLine("Naciśnij enter, aby kontynuować.");
            Console.ReadLine();
            Console.Clear();

            //Usuniecie pliku z ID
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
                    return;
                }
            }
        }
    }

}
