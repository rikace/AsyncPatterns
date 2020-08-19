using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using Functional.Async;
using static StockAnalyzer.StockUtils;
using static Functional.Async.AsyncEx;
using AsyncHelpers;

namespace StockAnalyzer
{
    public class StockAnalyzer
    {
        public static readonly string[] Stocks =
            new[] { "MSFT", "FB", "AAPL", "GOOG", "AMZN" };

        Func<string, string> alphavantageSourceUrl = (symbol) =>
            $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY_ADJUSTED&symbol={symbol}&outputsize=full&apikey=W3LUV5WID6C0PV5L&datatype=csv";

        Func<string, string> badSourceUrl = (symbol) => "";

        Func<string, string> stooqSourceUrl = (symbol) =>
            $"https://stooq.com/q/d/l/?s={symbol}.US&i=d";

        //  Stock prices history analysis
        async Task<StockData[]> ConvertStockHistory(string stockHistory)
        {
            return await Task.Run(() =>
            {
                string[] stockHistoryRows =
                    stockHistory.Split(Environment.NewLine.ToCharArray(),
                        StringSplitOptions.RemoveEmptyEntries);
                return (from row in stockHistoryRows.Skip(1)
                        let cells = row.Split(',')
                        let date = DateTime.Parse(cells[0])
                        let open = double.TryParse(cells[1], out _) ? double.Parse(cells[1]) : 0
                        let high = double.TryParse(cells[2], out _) ? double.Parse(cells[2]) : 0
                        let low = double.TryParse(cells[3], out _) ? double.Parse(cells[3]) : 0
                        let close = double.TryParse(cells[4], out _) ? double.Parse(cells[4]) : 0
                        select new StockData(date, open, high, low, close)
                    ).ToArray();
            });
        }

        private string GetDataTickerPath()
        {
            var tickers = "../Data/Tickers";
            while (!Directory.Exists(tickers))
                tickers = $"../{tickers}";
            return tickers;
        }

        async Task<string> DownloadStockHistory(string symbol, CancellationToken token)
        {
            string stockUrl = alphavantageSourceUrl(symbol);
            var request = await new HttpClient().GetAsync(stockUrl, token);
            return await request.Content.ReadAsStringAsync();
        }

        async Task<string> DownloadStockHistory(string symbol)
        { 

            //string url = alphavantageSourceUrl(symbol);

             string url = badSourceUrl(symbol);

            var request = WebRequest.Create(url);
            using (var response = await request.GetResponseAsync()
                .ConfigureAwait(false))
            using (var reader = new StreamReader(response.GetResponseStream()))
                return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        async Task<string> StreamReadStockHistory(string symbol)
        {
            var filePath = Path.Combine(GetDataTickerPath(), $"{symbol}.csv");
            
            // pretending some heavy work
            await Task.Delay(200);

            using (var reader = new StreamReader(filePath))
                return await reader.ReadToEndAsync().ConfigureAwait(false);
        }



        // Process Stock History
        async Task<Tuple<string, StockData[]>> ProcessStockHistory(string symbol)
        {
            string stockHistory = await DownloadStockHistory(symbol);
            StockData[] stockData = await ConvertStockHistory(stockHistory);
            return Tuple.Create(symbol, stockData);
        }


        async Task<Tuple<string, StockData[]>> ProcessStockHistoryComplete(string symbol)
        {
            var stockData = await AsyncEx.Retry(() =>
                            DownloadStockHistory(symbol)
                              .OrElse(() => StreamReadStockHistory(symbol))
                                 .Bind(stockHistory => ConvertStockHistory(stockHistory)), 3, TimeSpan.FromSeconds(1)); 
                                           

            return Tuple.Create(symbol, stockData);
        }

        async Task<Tuple<string, StockData[]>> ProcessStockHistoryLINQ(string symbol)
        {
            var stockData = await 
                                from stockHistory in DownloadStockHistory(symbol)
                                                      .OrElse(() => StreamReadStockHistory(symbol))
                                from _stockData in ConvertStockHistory(stockHistory)
                                select _stockData;

            return Tuple.Create(symbol, stockData);
        }


        // Example (1)
        public async Task AnalyzeStockHistory()
        {
            var sw = Stopwatch.StartNew();

            IEnumerable<Task<Tuple<string, StockData[]>>> stockHistoryTasks =
                Stocks.Select(stock => ProcessStockHistoryComplete(stock)); 

            var stockHistories = new List<Tuple<string, StockData[]>>();
            foreach (var stockTask in stockHistoryTasks)
                stockHistories.Add(await stockTask);

            DisplayStockInfo(stockHistories, sw.ElapsedMilliseconds);
            Console.WriteLine($"Completed in {sw.ElapsedMilliseconds}ms");

            //await Task.WhenAll(stockHistoryTasks)
            //   .ContinueWith(stockData =>
            //       DisplayStockInfo(stockData.Result, sw.ElapsedMilliseconds));
        }


        // Example (2)
        public async Task ProcessStockHistoryParallel()
        {
            var sw = Stopwatch.StartNew();
            
            List<Task<Tuple<string, StockData[]>>> stockHistoryTasks = 
                Stocks.Select(ProcessStockHistoryComplete).ToList();

            foreach (var stockHistoryTask in stockHistoryTasks)
            {
                var stockHistory = await stockHistoryTask;
                DisplayStockInfo(stockHistory, sw.ElapsedMilliseconds);
            }
            Console.WriteLine($"Completed in {sw.ElapsedMilliseconds}ms");
        }

        // Example (3)
        public async Task ProcessStockHistoryParallel_RequestGate(int dop)
        {
            var sw = Stopwatch.StartNew();

            // Process the stock analysis in parallel
            // When all the computation complete, then output the stock details
            // Than control the level of parallelism processing max DOP stocks at a given time

            List<Task<Tuple<string, StockData[]>>> stockHistoryTasks = Stocks.Select(ProcessStockHistoryComplete).ToList();

            var gate = new AsyncHelpers.RequestGate(dop);

            foreach (var stockHistoryTask in stockHistoryTasks)
            {
                using (var _ = await gate.AsyncAcquire(TimeSpan.FromSeconds(2)))
                {
                    var stockHistory = await stockHistoryTask;
                    DisplayStockInfo(stockHistory, sw.ElapsedMilliseconds);
                }
            }

            Console.WriteLine($"Completed in {sw.ElapsedMilliseconds}ms");
        }

        // Example (4)

        public async Task ProcessStockHistoryParallel_ForEachAsync(int dop)
        {
            var sw = Stopwatch.StartNew();

            await ForEachAsyncEx.ForEachAsync(Stocks, dop, async symbol =>
            {
                Tuple<string, StockData[]> stockHistoryTask = await ProcessStockHistoryComplete(symbol);
                DisplayStockInfo(stockHistoryTask, sw.ElapsedMilliseconds);
            });

            Console.WriteLine($"Completed in {sw.ElapsedMilliseconds}ms");
        }

        // Example (5)
        public async Task ProcessStockHistoryParallel_DOP(int dop)
        {
            var sw = Stopwatch.StartNew();

            // Process the stock analysis in parallel
            // When all the computation complete, then output the stock details
            // Than control the level of parallelism processing max DOP stocks at a given time

            Tuple<string, StockData[]>[] stockHistoryTasks = await Stocks.ExecuteInParallel(ProcessStockHistoryComplete, dop);

            foreach (var stockHistoryTask in stockHistoryTasks)
            {
                DisplayStockInfo(stockHistoryTask, sw.ElapsedMilliseconds);
            }

            Console.WriteLine($"Completed in {sw.ElapsedMilliseconds}ms");
        }

        // Example (6)

        async IAsyncEnumerable<Tuple<string, StockData[]>> stocksStream()
        {
            foreach (var stock in Stocks.Select(ProcessStockHistoryComplete))
                yield return await stock;
        }

        public async Task ProcessStockHistoryParallel_ForEachAsyncStream()
        {
            var sw = Stopwatch.StartNew();            

            await foreach(var stockHistory in stocksStream())
            {
                DisplayStockInfo(stockHistory, sw.ElapsedMilliseconds);
            }

            Console.WriteLine($"Completed in {sw.ElapsedMilliseconds}ms");
        }



        // Example (7)
        public async Task ProcessStockHistoryAsComplete()
        {
            var sw = Stopwatch.StartNew();

            List<Task<Tuple<string, StockData[]>>> stockHistoryTasks = Stocks.Select(ProcessStockHistoryComplete).ToList();

            while (stockHistoryTasks.Count > 0)
            {
                Task<Tuple<string, StockData[]>> stockHistoryTask = await Task.WhenAny(stockHistoryTasks);

                stockHistoryTasks.Remove(stockHistoryTask);
                
                Tuple<string, StockData[]> stockHistory = await stockHistoryTask;
                
                DisplayStockInfo(stockHistory, sw.ElapsedMilliseconds);
                 Thread.Sleep(250);
            }

            Console.WriteLine($"Completed in {sw.ElapsedMilliseconds}ms");
        }
    }
}