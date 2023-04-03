﻿using CommonLib;
using playhouse_connector_net;
using System;
using System.Collections.Generic;
using System.Net;

namespace PlayHouseConnector.network
{
    public class PacketParser
    {
        
        public const int MAX_PACKET_SIZE = 65535;
        public const int HEADER_SIZE = 10;
        //public const int LENGTH_FIELD_SIZE = 3;
        

        public virtual List<ClientPacket> Parse(RingBuffer buffer)
        {
            
            var packets = new List<ClientPacket>();
                        
            while (buffer.Count >= HEADER_SIZE)
            {
                try { 
                    
                    int bodySize = XBitConverter.ToHostOrder(buffer.PeekInt16(0));

                    if (bodySize > MAX_PACKET_SIZE)
                    {
                        LOG.Error($"Body size over : {bodySize}",GetType() );
                        throw new IndexOutOfRangeException("BodySizeOver");
                    }

                    // If the remaining buffer is smaller than the expected packet size, wait for more data
                    if (buffer.Count < bodySize + HEADER_SIZE)
                    {
                        break;
                    }

                    buffer.Clear(2);

                    short serviceId = XBitConverter.ToHostOrder(buffer.ReadInt16());
                    short msgId = XBitConverter.ToHostOrder(buffer.ReadInt16());
                    short msgSeq = XBitConverter.ToHostOrder(buffer.ReadInt16());
                    short errorCode = XBitConverter.ToHostOrder(buffer.ReadInt16());

                    var body = new PooledBuffer(bodySize);

                    buffer.Read(body,bodySize);

                    var clientPacket = new ClientPacket(new Header(serviceId,msgId,msgSeq,errorCode),new PooledBufferPayload(body));
                    packets.Add(clientPacket);

                }
                catch (Exception e)
                {
                    LOG.Error($"Exception while parsing packet",GetType(),e);
                }
            }

            return packets;
        }
    }
}