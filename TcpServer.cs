using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace stratum_server_sharp
{
    public class TcpServer : IDisposable
    {
        private readonly TcpClient client;
        private readonly NetworkStream stream;

        public TcpServer(TcpClient client, NetworkStream stream)
        {
            this.client = client;
            this.stream = stream;
        }

        public static TcpServer Create(int listenPort)
        {
            var server = new TcpListener(IPAddress.Parse("127.0.0.1"), listenPort);
            server.Start();
            Console.WriteLine(" You can connected with Putty on a (RAW session) to {0} to issue JsonRpc requests.",
                server.LocalEndpoint);
            while (true)
            {
                try
                {
                    var client = server.AcceptTcpClient();
                    var stream = client.GetStream();
                    Console.WriteLine("Client Connected..");
                    return new TcpServer(client, stream);
                }
                catch (Exception e)
                {
                    Console.WriteLine("RPCServer exception " + e);
                }
            }
        }

        public void Dispose()
        {
            client?.Dispose();
            stream?.Dispose();
        }

        public void Start(Action<StreamWriter, string> handleRequest)
        {
            var reader = new StreamReader(stream, Encoding.UTF8);
            var writer = new StreamWriter(stream, new UTF8Encoding(false));

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                handleRequest(writer, line);

                Console.WriteLine("REQUEST: {0}", line);
            }
        }

        public Task Notify(string message)
        {
            return client.Client.SendAsync(Encoding.UTF8.GetBytes(message), SocketFlags.None);
        }
    }
}