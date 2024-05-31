using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        Console.Write("Enter UserID: ");
        string userName = Console.ReadLine();

        Console.WriteLine("Do you want to fetch all liked tracks or a specific number? (all/number): ");
        string fetchChoice = Console.ReadLine().ToLower();

        int trackLimit = 0;
        if (fetchChoice == "number")
        {
            Console.Write("Enter the number of liked tracks to fetch: ");
            trackLimit = Convert.ToInt32(Console.ReadLine());
        }

        Console.WriteLine("Fetching liked tracks...");
        var stopwatch = Stopwatch.StartNew();
        List<string> likedTrackUrls = await GetLikedTrackUrlsAsync(userName, fetchChoice, trackLimit);
        stopwatch.Stop();
        TimeSpan elapsed = stopwatch.Elapsed;

        int count = 0;
        Console.WriteLine("Liked URLs:");
        foreach (var url in likedTrackUrls)
        {
            count++;
            Console.WriteLine($"[{count}] {url}");
        }

        Console.WriteLine($"Fetching completed in {elapsed.Hours} hours, {elapsed.Minutes} minutes, and {elapsed.Seconds} seconds.");

        Console.Write("Do you want to download these songs (y/n): ");
        string choice = Console.ReadLine().ToLower();
        if (choice == "y")
        {
            await DownloadTracksAsync(likedTrackUrls);
        }

        Console.ReadKey();
    }

    static async Task<List<string>> GetLikedTrackUrlsAsync(string userId, string fetchChoice, int trackLimit)
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

            int previousCount = 0;
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                var jsExecutor = (IJavaScriptExecutor)driver;
                jsExecutor.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
                await Task.Delay(1000); // Adjust delay as needed

                var trackElements = driver.FindElements(By.CssSelector(".soundList__item .soundTitle__title"));
                foreach (var trackElement in trackElements)
                {
                    var url = trackElement.GetAttribute("href");
                    if (!likedTrackUrls.Contains(url))
                    {
                        likedTrackUrls.Add(url);
                    }
                }

                if (likedTrackUrls.Count == previousCount)
                {
                    break; // Break if no new tracks are loaded
                }
                previousCount = likedTrackUrls.Count;

                if (fetchChoice == "number" && likedTrackUrls.Count >= trackLimit)
                {
                    break; // Break if the desired number of tracks is reached
                }

                TimeSpan elapsed = stopwatch.Elapsed;
                Console.Title = $"Fetching liked tracks... Elapsed time: {elapsed.Hours} hours, {elapsed.Minutes} minutes, {elapsed.Seconds} seconds";
            }
            stopwatch.Stop();
        }

        return likedTrackUrls;
    }

    static async Task DownloadTracksAsync(List<string> trackUrls)
    {
        try
        {
            var soundcloud = new SoundCloudClient();
            foreach (string trackUrl in trackUrls)
            {
                try
                {
                    var track = await soundcloud.Tracks.GetAsync(trackUrl);
                    var trackName = string.Join("_", track.Title.Split(Path.GetInvalidFileNameChars()));
                    var downloadDirectory = Path.Combine(Environment.CurrentDirectory, "Download");
                    var filePath = Path.Combine(downloadDirectory, $"{trackName}.mp3");

                    Directory.CreateDirectory(downloadDirectory);
                    await soundcloud.DownloadAsync(track, filePath);

                    Console.WriteLine($"Track downloaded successfully: {filePath}");
                }
                catch (Exception ex)
                {
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
}
