using System;
using ENETCSharp;
using Event = ENETCSharp.Event;
using EventType = ENETCSharp.EventType;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestENETCSharp
{
    class Program
    {
        /// <summary>
        /// Op codes.
        /// </summary>
        public enum OpCodes
        {
            PlayerLogin,
            PlayerJoin,
            PlayerSpawn,
            PlayerUnSpawn,
            ObjectSpawn,
            ObjectUnSpawn,
            PlayerPositionRotationUpdate,
            ObjectPositionRotationUpdate
        }

        /// <summary>
        /// Channel type.
        /// </summary>
        public enum ChannelTypes : byte
        {
            SENDTOALL = 0,
            SENDTOOTHER = 1,
            SENDTOSERVER = 2,
            SENDTOUID = 3 //NOT IMPLEMENTED
        }

        static void Main(string[] args)
        {
            Library.Initialize();
            using (Host client = new Host())
            {
                Address address = new Address();

                address.SetHost("127.0.0.1");
                address.Port = 9900;
                client.Create();

                Peer peer = client.Connect(address);

                //Start login to server
                //

                Event netEvent;
                while (!Console.KeyAvailable)
                {
                    client.Service(15, out netEvent);

                    switch (netEvent.Type)
                    {
                        case EventType.None:
                            break;

                        case EventType.Connect:
                            Console.WriteLine("Client connected to server - ID: " + peer.ID);
                            break;

                        case EventType.Disconnect:
                            Console.WriteLine("Client disconnected from server");
                            break;

                        case EventType.Timeout:
                            Console.WriteLine("Client connection timeout");
                            break;

                        case EventType.Receive:
                            Console.WriteLine("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                            netEvent.Packet.Dispose();
                            break;
                    }
                }

                client.Flush();
                client.Dispose();
                Library.Deinitialize();
                //Exit 0
                Environment.Exit(0);
            }
        }
    }
}
