using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace StockAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {

            //  Cancellation of Asynchronous Task
            CancellationTokenSource cts = new CancellationTokenSource();

            var stockAnalyzer = new StockAnalyzer();

            //Task.Factory.StartNew(async () => await stockAnalyzer.AnalyzeStockHistory(), cts.Token);
            //Task.Factory.StartNew(async () => await stockAnalyzer.ProcessStockHistoryParallel(), cts.Token);

            //Task.Factory.StartNew(async () => await stockAnalyzer.ProcessStockHistoryParallel_RequestGate(2), cts.Token);
            Task.Factory.StartNew(async () => await stockAnalyzer.ProcessStockHistoryParallel_ForEachAsync(3), cts.Token);
            //Task.Factory.StartNew(async () => await stockAnalyzer.ProcessStockHistoryParallel_DOP(2), cts.Token);
            //Task.Factory.StartNew(async () => await stockAnalyzer.ProcessStockHistoryParallel_ForEachAsyncStream(), cts.Token);
            
            //Task.Factory.StartNew(async () => await stockAnalyzer.ProcessStockHistoryAsComplete(), cts.Token);


            Console.ReadLine();
            Console.WriteLine("Press ENTER to terminate");
            
            cts.Cancel();
        }
    }
}