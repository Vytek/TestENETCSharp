using System;
using ENETCSharp;
using Event = ENETCSharp.Event;
using EventType = ENETCSharp.EventType;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestENETCSharpServer
{
    class Program
    {

        /// <summary>
        /// Base client.
        /// </summary>
        public class BaseClient
        {
            public ENETCSharp.Peer PeerClient { get; set; }
            public uint PeerID { get; set; }
        }

        private List<BaseClient> m_entities = new List<BaseClient>();

        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        static void Main(string[] args)
        {
            Library.Initialize();
            using (Host server = new Host())
            {
                Address address = new Address();

                address.Port = 9900;
                // 10 Channels
                // 10 Peers
                server.Create(address, 10, 10);

                Event netEvent;
                while (!Console.KeyAvailable)
                {
                    server.Service(15, out netEvent);

                    switch (netEvent.Type)
                    {
                        case EventType.None:
                            break;

                        case EventType.Connect:
                            Console.WriteLine("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            break;

                        case EventType.Disconnect:
                            Console.WriteLine("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            break;

                        case EventType.Timeout:
                            Console.WriteLine("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            break;

                        case EventType.Receive:
                            Console.WriteLine("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                            netEvent.Packet.Dispose();
                            break;
                    }
                }

                server.Flush();
                Library.Deinitialize();
            }
        }
    }
}
