using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace C2program
{
    class RTPServer
    {
        private const int AUDIO_BUFFER_SIZE = 65536 * 16;
        private const int SAMPLES_PER_SECOND = 24000;
        private const int BYTES_PER_SAMPLE = 2;
        private const double BYTES_PER_MS = SAMPLES_PER_SECOND * BYTES_PER_SAMPLE / 1000.0;
        private const int BYTES_PER_PACKET = 576 * 2 *2;

        private Media.Rtsp.RtspServer myRtspServer;
        private Media.Rtsp.Server.Streams.RtspSourceStream myRtspStream;
        private UdpClient client;
        private IPEndPoint endPoint;
        private Stopwatch packetTimer;
        private SpeechStreamer audioStream;
        //private bool writeHeaderToConsole = false;
        private bool listening = false;
        private string host;
        private int port;
        private ushort mySeqNum;
        private uint mySourceId;
        private uint timestamp;
        private Thread listenerThread;

        /// <summary>
        /// Returns a reference to the audio stream
        /// </summary>
        public SpeechStreamer AudioStream
        {
            get { return audioStream; }
        }
        /// <summary>
        /// Gets whether the client is listening for packets
        /// </summary>
        public bool Listening
        {
            get { return listening; }
        }
        /// <summary>
        /// Gets the port the RTP client is listening on
        /// </summary>
        public int Port
        {
            get { return port; }
        }

        /// <summary>
        /// RTP Client for receiving an RTP stream containing a WAVE audio stream
        /// </summary>
        /// <param name="host">The host to send data to</param>
        /// <param name="port">The port to send data on</param>
        public RTPServer(string host, int port)
        {
            Console.WriteLine(" [RTPServer] Loading...");

            this.host = host;
            this.port = port;
            mySeqNum = 1;
            //mySourceId = 0x37b6c555;
            mySourceId = 0xd1586f52;
            timestamp = 0x11111111;

            //myRtspServer = new Media.Rtsp.RtspServer(1234);

            // Initialize the audio stream that will hold the data
            audioStream = new SpeechStreamer(AUDIO_BUFFER_SIZE, 5);

            // Initialize packet timer
            packetTimer = new Stopwatch();
            //myRtspStream = new Media.Rtsp.Server.Streams.RtpSource("pistream",
            //myRtspServer.AddStream(

            Console.WriteLine(" Done");
        }

        /// <summary>
        /// Creates a connection to the RTP stream
        /// </summary>
        public void StartServer()
        {
            if (listening == false)
            {
                // Create new UDP client. The IP end point tells us which IP is sending the data
                client = new UdpClient(host, port);
                //endPoint = new IPEndPoint(IPAddress.Any, port);

                listening = true;
                listenerThread = new Thread(ReceiveCallback);
                listenerThread.Priority = ThreadPriority.Highest;
                listenerThread.Start();

                //            myRtpClient = new Media.Rtp.RtpClient(host);
                //            myRtpClient.Connect();

                Console.WriteLine(" [RTPServer] Listening for packets on stream and sending them to " + host + ":" + port + "...");
            }
            else
            {
                Console.WriteLine(" [RTPServer]:ERROR: set to start listening when already listening. StartServer() called more than once!"); 
            }
        }

        /// <summary>
        /// Tells the UDP client to stop listening for packets.
        /// </summary>
        public void StopServer()
        {
            // Set the boolean to false to stop the asynchronous packet receiving
            listening = false;
            Console.WriteLine(" [RTPServer] Stopped listening on port " + port);
        }

        /// <summary>
        /// Handles the receiving of UDP packets from the RTP stream
        /// </summary>
        /// <param name="ar">Contains packet data</param>
        private void ReceiveCallback()
        {
            // Begin looking for the next packet
            Console.WriteLine("[RTPServer:ReceiveCallback] Stopwatch is hi res = " + Stopwatch.IsHighResolution);
            int packetsSentInTimer = 0;
            int bytesSentInTimer = 0;
            int packetsToSend = 1;
            bool catchup = true;
            int bytesToSend;
            double maxBuffer = SAMPLES_PER_SECOND + 400 * BYTES_PER_MS;
            double minBuffer = SAMPLES_PER_SECOND + 300 * BYTES_PER_MS;
            Console.WriteLine("[RTPServer:] minBuffer: " + minBuffer + " maxBuffer: " + maxBuffer);
            Thread.Sleep(11000);
            packetTimer.Start();
            TimeSpan lastElapsed = packetTimer.Elapsed;
            
            while (listening)
            {
                if (true)//(packetsToSend > packetsSentInTimer)
                {
                    if (catchup)
                    {
                        bytesToSend = BYTES_PER_PACKET * 4;
                    }
                    else
                    {
                        bytesToSend = BYTES_PER_PACKET;
                    }
                    // Read from the stream
                    byte[] buffer = new byte[bytesToSend];
                    for(int i = 0; i < bytesToSend; i = i + 2)
                    {
                        buffer[i+1] = Convert.ToByte(0x01);
                        buffer[i] = Convert.ToByte(0x00);
                    }
                    int bytesRead = audioStream.Read(buffer, 0, bytesToSend);
                    
                    //check if there was a timeout and bytes read is zero. (If timeout but some bytes read just send them)
/*                    if (bytesRead == 0) //will still send the packet to keep pi stream alive
                    {
                        
                    }
*/
                    //check if blocked on reading from audio stream because there was no audio (over 1 second)
                    //if so reset timer and all packet counts
/*                    if ((packetTimer.ElapsedMilliseconds - lastElapsed.TotalMilliseconds) > 1000)
                    {
                        Console.WriteLine("[RTPServer]: Reseting packet Timer: time elapsed while listening for packets. pktTimer: " +
                            packetTimer.ElapsedMilliseconds + " lastElapsed: " + lastElapsed.TotalMilliseconds);
                        packetTimer.Restart();
                        lastElapsed = packetTimer.Elapsed;
                        packetsSentInTimer = 0; //will increment to 1 after this current packet is sent
                        bytesSentInTimer = 0; //will increment after current packet is sent;
                        packetsToSend = 1;
                    }
                    else
                    {
//                        Console.WriteLine("    sending packet " + packetsSentInTimer + " pktTimer.Elapsed: " + packetTimer.Elapsed);
//                        lastElapsed = packetTimer.Elapsed;
                    }
*/
                    byte[] newbuff = SwapEndianness(buffer, 2);
                    
                    Media.Rtp.RtpPacket rtpPacket = new Media.Rtp.RtpPacket();
                    rtpPacket.SequenceNumber = mySeqNum;
                    rtpPacket.SynchronizationSourceIdentifier = 0x07070707;
                    rtpPacket.PayloadType = 0x0b;
                    rtpPacket.TimeStamp = timestamp;
                    rtpPacket.Channel = 0;
                    rtpPacket.Payload = newbuff;


                    // Write the bytes to the UDP socket
                    byte[] pkt = rtpPacket.ToBytes();
                    client.Send(pkt, pkt.Length);
                    //Console.WriteLine("read " + pkt.Length + " bytes");

                    mySeqNum++;
                    timestamp = timestamp + Convert.ToUInt32(bytesToSend / 2);
                    packetsSentInTimer++;
                    bytesSentInTimer = bytesSentInTimer + bytesToSend;
                }
/*                else
                {
                    if ((packetTimer.ElapsedMilliseconds - lastElapsed.TotalMilliseconds) > 1000)
                    {
                        Console.WriteLine("[RTPServer]: resetting timer in else condition because too much time elapsed pktTimer: " + 
                            packetTimer.ElapsedMilliseconds + " lastElapsed: " + lastElapsed.TotalMilliseconds);
                        packetTimer.Restart();
                        lastElapsed = packetTimer.Elapsed;
                        packetsSentInTimer = 0; //will increment to 1 after this current packet is sent
                        packetsToSend = 1;
                    }
                    else
                    {
                        Console.Write(" lastElapsed: " + lastElapsed + " pktTimer.Elapsed: " + packetTimer.Elapsed + " ");
                        lastElapsed = packetTimer.Elapsed;
                        packetsToSend = Convert.ToInt32(Math.Round(lastElapsed.TotalMilliseconds * BYTES_PER_MS / BYTES_PER_PACKET));
                        //packetsToSend = packetsToSendInTimer - packetsSentInTimer;
                        Console.WriteLine("lastElapsed(ms): " + lastElapsed.TotalMilliseconds + 
                            " packetsSentInTimer: " + packetsSentInTimer + " packetsToSend: " + packetsToSend);
                    }
                }*/
                
                //if number of bytes sent is greater than .75 seconds more than elapsed time then sleep a little to slow down
                double shouldSend = (packetTimer.ElapsedMilliseconds * BYTES_PER_MS);
                //int bytesSent = packetsSentInTimer*BYTES_PER_PACKET;
//                Console.WriteLine("[RTPServer]: checking for sleep elapsed(ms): " + packetTimer.ElapsedMilliseconds + /*" pktTimer: " + packetTimer.Elapsed +*/
//                    " so should send " + shouldSend  + " bytes so with maxBuffer: " + (shouldSend + maxBuffer) + ". Bytes sent: " + bytesSentInTimer + 
//                    " in " + packetsSentInTimer + " packets");
                while ((shouldSend + maxBuffer) < bytesSentInTimer) //as long as we've sent over our max buffer wait
                {
//                                        Console.WriteLine("     [RTPServer]: sleeping ... ");
                                        Thread.Sleep(5);
                    //Thread.Sleep(Convert.ToInt32(Math.Round(BYTES_PER_PACKET / BYTES_PER_MS)));
                    //catchup = false;
                    shouldSend = (packetTimer.ElapsedMilliseconds * BYTES_PER_MS);
                }
                if((shouldSend + minBuffer) > bytesSentInTimer) //if we've dipped below our minBuffer catch up
                {
//                    Console.WriteLine("      [RTPServer}: catchup true");
                    catchup = true;
                }
                else
                {
//                    Console.WriteLine("      [RTPServer}: catchup false");
                    catchup = false;
                }
            }
        }

        /// <summary>
        /// Grabs a value from the RTP header in Big-Endian format
        /// </summary>
        /// <param name="packet">The RTP packet</param>
        /// <param name="startBit">Start bit of the data value</param>
        /// <param name="endBit">End bit of the data value</param>
        /// <returns>The value</returns>
        private int GetRTPHeaderValue(byte[] packet, int startBit, int endBit)
        {
            int result = 0;

            // Number of bits in value
            int length = endBit - startBit + 1;

            // Values in RTP header are big endian, so need to do these conversions
            for (int i = startBit; i <= endBit; i++)
            {
                int byteIndex = i / 8;
                int bitShift = 7 - (i % 8);
                result += ((packet[byteIndex] >> bitShift) & 1) * (int)Math.Pow(2, length - i + startBit - 1);
            }
            return result;
        }

        private byte[] SwapEndianness(byte[] origBuff, int sizeOfData)
        {
            int buffLength = origBuff.Length;
            byte[] newBuff = new byte[buffLength];
            for (int i = 0; i < buffLength - sizeOfData + 1; i = i + sizeOfData)
            {
                for (int j = 0; j < sizeOfData; j++)
                {
                    newBuff[i + j] = origBuff[i + sizeOfData -1 - j];
                }
            }
            return newBuff;
        }

//        private void ReceiveCallbackTwo()
//        {
//            Media.Rtp.RtpClient rtpClient = Media.Rtp.RtpClient.Sender(new IPAddress(0xc0a87164));
//            rtpClient.AddTransportContext(new Media.Rtp.RtpClient.TransportContext(0,1,0x07070707,
//            rtpClient.Connect();
//            int bytesToSend = BYTES_PER_PACKET;
//            while (listening)
//            {
///*                Media.Rtsp.RtspServer server = new Media.Rtsp.RtspServer();
//                Media.Rtsp.Server.Streams.RtspSourceStream source = new Media.Rtsp.Server.Streams.RtspSourceStream("blakeStream", "rtsp://v4.cache5.c.youtube.com/CjYLENy73wIaLQlg0fcbksoOZBMYDSANFEIJbXYtZ29vZ2xlSARSBXdhdGNoYNWajp7Cv7WoUQw=/0/0/0/video.3gp");
//                server.AddStream(source);
//                server.Start();
// */
//                byte[] buffer = new byte[bytesToSend];
//                rtpClient.SendRtpPacket(new Media.Rtp.RtpPacket(buffer));

//            }
//        }
    }
}
