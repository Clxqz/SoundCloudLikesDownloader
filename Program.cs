using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SoundCloudExplode;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "SoundCloud Like Downloader By Clxqz";
        //This Code Searches For All Your Like Songs, Fetches The Urls using Selenium then returns the url links and downloads them into a folder
        Console.Write("Enter UserID: ");
        string userName = Console.ReadLine();

        Console.Write("Enter Like Count: ");
        int likeCount = Convert.ToInt32(Console.ReadLine());

        int likesC = likeCount / 10;

        List<string> likedTrackUrls = await GetLikedTrackUrlsAsync(userName, likesC);
        int count = 0;
        Console.WriteLine("Liked URLs:");
        foreach (var url in likedTrackUrls)
        {
            count++;
            Console.WriteLine($"[{count}]" + url);
        }

        Console.Write("Do you want to download these songs (y/n): ");
        string choice = Console.ReadLine().ToLower();
        if (choice == "y")
        {
            try
            {
                var soundcloud = new SoundCloudClient();

                foreach (string trackUrl in likedTrackUrls)
                {
                    var track = await soundcloud.Tracks.GetAsync(trackUrl);
                    var trackName = string.Join("_", track.Title.Split(Path.GetInvalidFileNameChars()));
                    var downloadDirectory = Path.Combine(Environment.CurrentDirectory, "Download");
                    var filePath = Path.Combine(downloadDirectory, $"{trackName}.mp3");

                    Directory.CreateDirectory(downloadDirectory);

                    // Attempt to download the track
                    try
                    {
                        await soundcloud.DownloadAsync(track, filePath);
                        Console.WriteLine($"Track downloaded successfully: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        // Log the error message and continue with the next track
                        Console.WriteLine($"Error downloading track: {ex.Message}");
                    }
                }

                Console.WriteLine("All tracks downloaded successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        else if (choice == "n")
        {
            Environment.Exit(0);
        }
        Console.ReadKey();
    }

    static async Task<List<string>> GetLikedTrackUrlsAsync(string userId, int likesCount)
    {
        var likedTrackUrls = new List<string>();
        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;
        var driverOptions = new ChromeOptions();
        driverOptions.AddArgument("--headless");
        driverOptions.SetLoggingPreference(LogType.Browser, LogLevel.All);

        using (var driver = new ChromeDriver(service, driverOptions))
        {
            driver.Navigate().GoToUrl($"https://soundcloud.com/{userId}/likes");


            await Task.Delay(2000);


            var jsExecutor = (IJavaScriptExecutor)driver;
            for (int i = 0; i < likesCount; i++)
            {
                jsExecutor.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
                await Task.Delay(1000);
            }

            await Task.Delay(2000);

            var trackElements = driver.FindElements(By.CssSelector(".soundList__item .soundTitle__title"));

            foreach (var trackElement in trackElements)
            {
                likedTrackUrls.Add(trackElement.GetAttribute("href"));
            }
        }

        return likedTrackUrls;
    }
}
