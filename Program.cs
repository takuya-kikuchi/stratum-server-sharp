using System;
using System.IO;
using System.Threading.Tasks;
using AustinHarris.JsonRpc;

namespace stratum_server_sharp
{
    class Program
    {
        private static object _svc;

        static void Main(string[] args)
        {
            _svc = new StratumService();

            var rpcResultHandler = new AsyncCallback(
                state =>
                {
                    var async = ((JsonRpcStateAsync) state);
                    var result = async.Result;
                    var writer = ((StreamWriter) async.AsyncState);

                    writer.WriteLine(result);
                    writer.FlushAsync();
                });

            using (var tcpServer = TcpServer.Create(3333))
            {
                Task.WaitAll(
                    // JSON RPC Server task
                    Task.Run(() =>
                        tcpServer.Start((writer, line) =>
                        {
                            var async = new JsonRpcStateAsync(rpcResultHandler, writer) {JsonRpc = line};
                            JsonRpcProcessor.Process(async, writer);
                        })),
                    // Bitcoin client task
                    Task.Run(async () =>
                    {
                        var line = "";
                        var difficulty = 2048;

                        ShowCommands();
                        while (!String.IsNullOrEmpty(line = Console.ReadLine()))
                        {
                            try
                            {
                                switch (line)
                                {
                                    case "n":
                                        Console.WriteLine($"Notify new mining job with difficulty {difficulty}.");
                                        // set difficulty
                                        await NotifyDifficultyAsync(tcpServer, difficulty);
                                        await NotifyNewJobAsync(tcpServer);
                                        break;
                                    case "i":
                                        Console.WriteLine($"Increase difficulty. to {difficulty * 2}");
                                        difficulty *= 2;
                                        await NotifyDifficultyAsync(tcpServer, difficulty);
                                        await NotifyNewJobAsync(tcpServer);
                                        break;
                                    case "d":
                                        Console.WriteLine($"Decrease difficulty. to {difficulty / 2}");
                                        difficulty /= 2;
                                        await NotifyDifficultyAsync(tcpServer, difficulty);
                                        await NotifyNewJobAsync(tcpServer);
                                        break;
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }

                            ShowCommands();
                        }

                        ;
                    })
                );
            }
        }

        private static void ShowCommands()
        {
            Console.WriteLine("n: notify, i: increase difficulty, d: decrease difficulty");
        }

        private static async Task NotifyDifficultyAsync(TcpServer tcpServer, int difficulty)
        {
            var msg = "{ \"id\": null, \"method\": \"mining.set_difficulty\", \"params\": [" +
                      difficulty + "]}";
            await tcpServer.Notify(msg);
            Console.WriteLine($"SEND: {msg}");
        }

        private static async Task NotifyNewJobAsync(TcpServer tcpServer)
        {
            var jobId = new Random().Next().ToString("x8");
            var nTimeStr = (DateTime.Now.Ticks / 1000).ToString("x8");

            var msg = "{\"params\": [\"" + jobId +
                      "\", \"4d16b6f85af6e2198f44ae2a6de67f78487ae5611b77c6c0440b921e00000000\", \"01000000010000000000000000000000000000000000000000000000000000000000000000ffffffff20020862062f503253482f04b8864e5008\", \"072f736c7573682f000000000100f2052a010000001976a914d23fcdf86f7e756a64a7a9688ef9903327048ed988ac00000000\", [], \"00000002\", \"1c2ac4af\", \"" +
                      nTimeStr + "\", true], \"id\": null, \"method\": \"mining.notify\"}";

            await tcpServer.Notify(msg);

            Console.WriteLine($"SEND: {msg}");
        }
    }
}