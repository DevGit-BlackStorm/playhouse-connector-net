﻿using System;
using System.Text;
using System.Threading;
using CommonLib;
using NetCoreServer;
using PlayHouse;
using PlayHouse.Utils;

namespace PlayHouseConnector.Network
{
    internal class WsClient : NetCoreServer.WsClient, IClient
    {
        private readonly ClientNetwork _clientNetwork;

        //private readonly IConnectorListener _connectorListener;
        private readonly LOG<WsClient> _log = new();
        private readonly PacketParser _packetParser = new();
        private readonly RingBuffer _recvBuffer = new(1024 * 1024 * 2);
        private readonly PooledByteBuffer _sendBuffer = new(1024 * 1024 * 2);
        private bool _stop;

        public WsClient(string host, int port, ClientNetwork clientNetwork) : base(host, port)
        {
            _clientNetwork = clientNetwork;

            OptionNoDelay = true;
            OptionKeepAlive = true;

            OptionReceiveBufferSize = 64 * 1024;
            OptionSendBufferSize = 64 * 1024;

        }

        public void ClientConnect()
        {
            base.Connect();
        }

        public void ClientDisconnect()
        {
            base.Disconnect();
        }

        public void ClientConnectAsync()
        {
            base.ConnectAsync();
        }

        public void ClientDisconnectAsync()
        {
            base.DisconnectAsync();
        }

        public bool IsClientConnected()
        {
            return IsConnected;
        }

        public void Send(ClientPacket clientPacket)
        {
            using (clientPacket)
            {
                _sendBuffer.Clear();
                clientPacket.GetBytes(_sendBuffer);
                base.Send(_sendBuffer.Buffer(), 0, _sendBuffer.Count);
            }
        }

        public bool IsStopped()
        {
            return _stop;
        }

        public void DisconnectAndStop()
        {
            _stop = true;
            CloseAsync(1000);
            while (IsConnected)
                Thread.Yield();
        }

        public override void OnWsConnecting(HttpRequest request)
        {
            request.SetBegin("GET", "/");
            request.SetHeader("Host", "localhost");
            request.SetHeader("Origin", "http://localhost");
            request.SetHeader("Upgrade", "websocket");
            request.SetHeader("Connection", "Upgrade");
            request.SetHeader("Sec-WebSocket-Key", Convert.ToBase64String(WsNonce));
            request.SetHeader("Sec-WebSocket-Protocol", "chat, superchat");
            request.SetHeader("Sec-WebSocket-Version", "13");
            request.SetBody();
        }

        public override void OnWsConnected(HttpResponse response)
        {
            _stop = false;

            _log.Info(() => $"Ws Socket client connected  - [sid:{Id}]");
            _clientNetwork.OnConnected();
        }


        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                _recvBuffer.Write(buffer, offset, size);
                var packets = _packetParser.Parse(_recvBuffer);
                packets.ForEach(packet => { _clientNetwork.OnReceive(packet); });
            }
            catch (Exception ex)
            {
                _log.Error(() => $"OnReceived Exception - [exception:{ex}]");
                Disconnect();
            }
        }

        protected override void OnDisconnected()
        {
            _log.Info(() => $"Ws client disconnected  - [sid:{Id}]");
            _clientNetwork.OnDisconnected();
        }
    }
}