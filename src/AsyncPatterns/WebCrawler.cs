﻿using AsyncHelpers;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncPatterns
{
    public class WebCrawlerAsync
    {
        // Web crawler execution using memoization
        public static Func<string, IEnumerable<string>> WebCrawlerMemoized =
                    Memoization.Memoize<string, IEnumerable<string>>(WebCrawler);


        //  Thread-safe memoization function
        public static Func<string, IEnumerable<string>> WebCrawlerMemoizedThreadSafe =
            Memoization.MemoizeThreadSafe<string, IEnumerable<string>>(WebCrawler);

        public static IEnumerable<string> WebCrawler(string url)
        {
            string content = GetWebContent(url);
            yield return content;

            foreach (string item in AnalyzeHtmlContent(content))
                yield return GetWebContent(item);
        }
        private static string GetWebContent(string url)
        {
            using (var wc = new WebClient())
                return wc.DownloadString(new Uri(url));
        }

        private static readonly Regex regexLink = new Regex(@"(?<=href=('|""))https?://.*?(?=\1)");
        private static IEnumerable<string> AnalyzeHtmlContent(string text)
        {
            foreach (var url in regexLink.Matches(text))
                yield return url.ToString();
        }

        private static readonly Regex regexTitle = new Regex("<title>(?<title>.*?)<\\/title>", RegexOptions.Compiled);
        public static string ExtractWebPageTitle(string textPage)
        {
            if (regexTitle.IsMatch(textPage))
                return regexTitle.Match(textPage).Groups["title"].Value;
            return "No Page Title Found!";
        }

        public static void RunDemo(List<string> urls)
        {

            BenchPerformance.Time("Web crawler execution", () =>
            {
                var webPageTitles = from url in urls
                                    from pageContent in WebCrawler(url)
                                    select ExtractWebPageTitle(pageContent);

                Console.WriteLine($"Crawled {webPageTitles.Count()} page titles");
            });

            BenchPerformance.Time("Web crawler execution using memoization", () =>
            {
                var webPageTitles = from url in urls
                                    from pageContent in WebCrawlerMemoized(url)
                                    select ExtractWebPageTitle(pageContent);

                Console.WriteLine($"Crawled {webPageTitles.Count()} page titles");
            });


            BenchPerformance.Time("Thread-safe memoization function", () =>
            {
                var webPageTitles = from url in urls.AsParallel()
                                    from pageContent in WebCrawlerMemoizedThreadSafe(url)
                                    select ExtractWebPageTitle(pageContent);

                Console.WriteLine($"Crawled {webPageTitles.Count()} page titles");
            });
        }
    }

}
