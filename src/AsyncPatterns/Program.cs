using SixLabors.ImageSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Functional.Async;


namespace AsyncPatterns
{
    class Program
    {
        static async Task ImageProcessingExample()
        {
            var sourceImages = "../Data/Images";
            while (!Directory.Exists(sourceImages))
                sourceImages = $"../{sourceImages}";


            var destination = "./Output";
            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);
            else
                foreach (FileInfo file in new DirectoryInfo(destination).GetFiles())
                    file.Delete();


            ImageProcessing imageProc = new ImageProcessing(sourceImages, destination);

            await imageProc.RunContinuation();
            //await imageProc.RunTransformer();
        }

        static void WebCrawlerExample()
        {
            List<string> urls = new List<string> {
                @"http://www.google.com",
                @"http://www.microsoft.com",
                @"http://www.bing.com",
                @"http://www.google.com"
            };

            WebCrawlerAsync.RunDemo(urls);
        }

        static async Task DownloadSiteIconAsyncExample()
        {
            var urls = new List<string>
            {
                "https://edition.cnn.com",
                "http://www.bbc.com",
                "https://www.microsoft.com",
                "https://www.apple.com",
                "https://www.amazon.com",
                "https://www.facebook.com"
            };

            var tasks = (from url in urls
                         select AsyncOperations.DownloadSiteIconAsync(url, $"./ Output/{Guid.NewGuid().ToString("N")}.jpg"))
                .ToArray(); // TO ARRAY IS IMPORTANT

            var images = await Task.WhenAll(tasks);
            foreach (var image in images)
            {
                // Do Something
            }
        }

        static async Task Main(string[] args)
        {
            await ImageProcessingExample();

            // WebCrawlerExample();
            // await DownloadSiteIconAsyncExample();

            Console.WriteLine("Completed");
            Console.ReadLine();







        }
    }
}
