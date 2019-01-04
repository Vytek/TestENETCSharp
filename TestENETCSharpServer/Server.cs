using System;
using ENETCSharp;
using Event = ENETCSharp.Event;
using EventType = ENETCSharp.EventType;
using System.Collections.Generic;
using NetStack.Compression;
using NetStack.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestENETCSharpServer
{
    class Server
    {
        public static ushort portNumber = 9900;
        public static bool DEBUG = true;
        public bool Running { get; private set; }

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
        /// Base client.
        /// </summary>
        public class BaseClient
        {
            public ENETCSharp.Peer PeerClient { get; set; }
            public uint PeerID { get; set; }
        }

        public float RandomRange = 50f;

        public BoundedRange[] Range = new BoundedRange[3];

        private List<BaseClient> m_entities = new List<BaseClient>();

        //Add Peer connected to General List
        /// <summary>
        /// Adds the client peer.
        /// </summary>
        /// <param name="ClientPeer">Client peer.</param>
        /// <param name="vPeerID">Peer identifier.</param>
        private void AddClientPeer(Peer ClientPeer, uint vPeerID)
        {
            m_entities.Add(new BaseClient()
            {
                PeerClient = ClientPeer,
                PeerID = vPeerID
            });
        }

        //Remove Peer disconnected/Timeouted to General List
        /// <summary>
        /// Removes the client peer.
        /// </summary>
        /// <param name="ClientPeer">Client peer.</param>
        /// <param name="vPeerID">Peer identifier.</param>
        private void RemoveClientPeer(Peer ClientPeer, uint vPeerID)
        {
            m_entities.RemoveAll(item => item.PeerID == vPeerID);
        }

        /// <summary>
        /// Start this instance.
        /// </summary>
        public void Start()
        {

            //Set range for compression
            Range[0] = new BoundedRange(-RandomRange, RandomRange, 0.05f);
            Range[1] = new BoundedRange(-RandomRange, RandomRange, 0.05f);
            Range[2] = new BoundedRange(-RandomRange, RandomRange, 0.05f);

            Library.Initialize();
            using (Host server = new Host())
            {
                Address address = new Address();
                Console.WriteLine("Server started!");
                address.Port = portNumber;
                // 10 Channels
                // 10 Peers
                server.Create(address, 10, 10);

                Event netEvent;
                while (!Console.KeyAvailable)
                {
                    server.Service(0, out netEvent);

                    switch (netEvent.Type)
                    {
                        case EventType.None:
                            break;

                        case EventType.Connect:
                            Console.WriteLine("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            //Add Peer connected to General List
                            AddClientPeer(netEvent.Peer, netEvent.Peer.ID);
                            Console.WriteLine($"Numbers Peer(s): {server.PeersCount.ToString()}");
                            SendToSingleClient(CreateAnswerPacketLogin("OK", netEvent.Peer.ID), netEvent.Peer.ID);
                            break;

                        case EventType.Disconnect:
                            Console.WriteLine("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            //Remove Peer disconnected / Timeouted to General List
                            RemoveClientPeer(netEvent.Peer, netEvent.Peer.ID);
                            break;

                        case EventType.Timeout:
                            Console.WriteLine("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            //Remove Peer disconnected / Timeouted to General List
                            RemoveClientPeer(netEvent.Peer, netEvent.Peer.ID);
                            break;

                        case EventType.Receive:
                            //Process packet received!
                            ProcessPacket(netEvent);
                            Console.WriteLine("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                            netEvent.Packet.Dispose();
                            break;
                    }
                }

                server.Flush();
                server.Dispose();
                Library.Deinitialize();
                //Exit 0
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Processes the packet.
        /// </summary>
        /// <param name="evt">Evt.</param>
        private void ProcessPacket(Event evt)
        {
            byte[] data = new byte[1024];
            evt.Packet.CopyTo(data);

            BitBuffer buffer = new BitBuffer(128);
            buffer.FromArray(data, evt.Packet.Length);

            OpCodes op = (OpCodes)buffer.ReadInt();
            uint id = buffer.ReadUInt();
            if (DEBUG)
            {
                Console.WriteLine($"OPCODE: {op.ToString()}");
                Console.WriteLine($"PEERID: {id.ToString()}");
            }

            if (evt.Peer.ID != id)
            {
                Console.WriteLine($"ID Mismatch! {evt.Peer.ID} vs. {id}");
                return;
            }

            Packet packet = evt.Packet;

            switch (op)
            {
                case OpCodes.PlayerLogin:
                    //Answer OK
                    SendToSingleClient(CreateAnswerPacketLogin("OK", evt.Peer.ID), evt.Peer.ID);
                    break;
            }
        }

        /// <summary>
        /// Sends to all.
        /// </summary>
        /// <param name="_packet">Packet.</param>
        private void SendToAll(Packet _packet)
        {
            for (int i = 0; i < m_entities.Count; i++)
            {
                m_entities[i].PeerClient.Send((byte)ChannelTypes.SENDTOALL, ref _packet);
            }
        }

        /// <summary>
        /// Sends to others.
        /// </summary>
        /// <param name="_packet">Packet.</param>
        /// <param name="_ExcludedPeerID">Exclude peer identifier.</param>
        private void SendToOthers(Packet _packet, uint _ExcludedPeerID)
        {
            for (int i = 0; i < m_entities.Count; i++)
            {
                if (m_entities[i].PeerID != _ExcludedPeerID)
                {
                    m_entities[i].PeerClient.Send((byte)ChannelTypes.SENDTOOTHER, ref _packet);
                }
            }
        }

        /// <summary>
        /// Sends to single client.
        /// </summary>
        /// <param name="_packet">Packet.</param>
        /// <param name="_toPeerID">To peer identifier.</param>
        private void SendToSingleClient(Packet _packet, uint _toPeerID)
        {
            for (int i = 0; i < m_entities.Count; i++)
            {
                if (m_entities[i].PeerID == _toPeerID)
                {
                    m_entities[i].PeerClient.Send((byte)ChannelTypes.SENDTOSERVER, ref _packet);
                }
            }
        }

        /// <summary>
        /// Creates the answer packet login.
        /// </summary>
        /// <returns>The answer packet login.</returns>
        /// <param name="Message">Message.</param>
        /// <param name="_toPeerID">To peer identifier.</param>
        public Packet CreateAnswerPacketLogin(String Message, uint _toPeerID)
        {
            byte[] data = new byte[16];
            BitBuffer buffer = new BitBuffer(128);
            buffer.AddInt((int)OpCodes.PlayerLogin)
                .AddUInt(_toPeerID)
                .AddString(Message)
                .ToArray(data);

            Packet packet = default(Packet);
            packet.Create(data);
            return packet;
        }

        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            Server ServeENETCSharp = new Server();
            ServeENETCSharp.Start();
        }
    }
}