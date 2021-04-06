using System;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace WebsocketTestServer
{
    class Program
    {
        public class Echo : WebSocketBehavior
        {
            public Echo()
            {
                SendRandomBlocks();
            }
            private async Task SendRandomBlocks()
            {
                Random r = new Random();
                while (true)
                {
                    if (Sessions != null)
                    {
                        byte[] data = new byte[7];

                        short randomX = (short)r.Next(0, 1000);
                        short randomY = (short)r.Next(0, 1000);


                        byte[] xBytes = BitConverter.GetBytes(randomX);
                        byte[] yBytes = BitConverter.GetBytes(randomY);


                        byte randomR = (byte)r.Next(0, 256);
                        byte randomG = (byte)r.Next(0, 256);
                        byte randomB = (byte)r.Next(0, 256);


                        data[0] = xBytes[0];
                        data[1] = xBytes[1];
                        data[2] = yBytes[0];
                        data[3] = yBytes[1];

                        data[4] = randomR;
                        data[5] = randomG;
                        data[6] = randomB;


                        Console.WriteLine($"Send: {randomX}/{randomY} paint R:{randomR}|G:{randomG}|B:{randomB}");

                        Sessions.Broadcast(data);
                    }


                    await Task.Delay(250);
                }
            }
            

            protected override void OnMessage(MessageEventArgs e)
            {
                var msg = System.Text.Encoding.UTF8.GetString(e.RawData);
                //Console.WriteLine("Got Message: " + msg);
                Send(msg);
            }
        }
        static void Main(string[] args)
        {
            var wssv = new WebSocketServer(9000);
            wssv.AddWebSocketService<Echo>("/echo");
            wssv.Start();
           

            Console.ReadKey(true);
            wssv.Stop();


        }

    }
}
