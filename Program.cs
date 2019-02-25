using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AustinHarris.JsonRpc;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.RPC;
using Newtonsoft.Json;
using stratum_server_sharp.Bitcoin;

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

            using (var tcpServer = TcpServer.Create(33333))
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
                        for (string line = Console.ReadLine(); !string.IsNullOrEmpty(line); line = Console.ReadLine())
                        {
                            var difficulty = 2;
                            Console.WriteLine("n: notify, i: increase difficulty, d: decrease difficulty");
                            switch (line)
                            {
                                case "n":
                                    await tcpServer.Notify(
                                        "{\"params\": [\"bf\", \"4d16b6f85af6e2198f44ae2a6de67f78487ae5611b77c6c0440b921e00000000\", \"01000000010000000000000000000000000000000000000000000000000000000000000000ffffffff20020862062f503253482f04b8864e5008\", \"072f736c7573682f000000000100f2052a010000001976a914d23fcdf86f7e756a64a7a9688ef9903327048ed988ac00000000\", [], \"00000002\", \"1c2ac4af\", \"504e86b9\", false], \"id\": null, \"method\": \"mining.notify\"}");
                                    break;
                                case "i":
                                    difficulty *= 2;
                                    await tcpServer.Notify(
                                        "{ \"id\": null, \"method\": \"mining.set_difficulty\", \"params\": [" +
                                        difficulty + "]}");
                                    break;
                                case "d":
                                    difficulty /= 2;
                                    await tcpServer.Notify(
                                        "{ \"id\": null, \"method\": \"mining.set_difficulty\", \"params\": [" +
                                        difficulty + "]}");
                                    break;
                            }
                        }
                    })
                );
            }
        }
    }
}