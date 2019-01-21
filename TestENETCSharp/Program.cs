using System;
using ENETCSharp;
using Event = ENETCSharp.Event;
using EventType = ENETCSharp.EventType;
using NetStack.Compression;
using NetStack.Serialization;
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

        /// <summary>
        /// Creates the request packet login.
        /// </summary>
        /// <returns>The request packet login.</returns>
        /// <param name="Message">Message.</param>
        /// <param name="_fromPeerID">To peer identifier.</param>
        public static Packet CreateRequestPacketLogin(String Message, uint _fromPeerID)
        {
            byte[] data = new byte[1024];
            BitBuffer buffer = new BitBuffer(128);
            buffer.AddInt((int)OpCodes.PlayerLogin)
                .AddUInt(_fromPeerID)
                .AddString(Message)
                .ToArray(data);

            Packet packet = default(Packet);
            packet.Create(data);
            return packet;
        }

        /// <summary>
        /// Sends to server.
        /// </summary>
        /// <param name="_packet">Packet.</param>
        /// <param name="toPeerServer">To peer server.</param>
        private static bool SendToServer(Packet _packet, Peer toPeerServer)
        {
            Console.WriteLine($"Client {toPeerServer.State.ToString()}");
            //return toPeerServer.Send((byte)ChannelTypes.SENDTOSERVER, ref _packet);                                                      
            return toPeerServer.Send((Byte)ChannelTypes.SENDTOSERVER, ref _packet);
        }

        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        static void Main(string[] args)
        {
            Library.Initialize();
            using (Host client = new Host())
            {
                Address address = new Address();

                address.SetHost("127.0.0.1");
                address.Port = 9900;
                //client.Create(1,10); 
                client.Create();

                //Connect to HOST with 10 Channels limit
                //If you NOT declare Channels limit you had to use only one channel (0)
                //If you try to send packet 1 (e.g.), packet will not been sent!
                Peer peer = client.Connect(address, 10);
                //Send Request once
                bool SendRequestPacketLogin = true;

                Event netEvent;
                while (!Console.KeyAvailable)
                {
                    client.Service(0, out netEvent);

                    switch (netEvent.Type)
                    {
                        case EventType.None:
                            break;

                        case EventType.Connect:
                            Console.WriteLine("Client connected to server - ID: " + peer.ID);
                            if (SendRequestPacketLogin)
                            {
                                //Start login to server
                                Console.WriteLine("Try to send Login Packet to Server.");
                                if (SendToServer(CreateRequestPacketLogin("Login;Password", peer.ID), peer))
                                {
                                    Console.WriteLine("Message sent!");
                                }
                                else
                                {
                                    Console.WriteLine("Message NOT sent!");
                                }
                                client.Flush();
                                SendRequestPacketLogin = false;
                            }
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
