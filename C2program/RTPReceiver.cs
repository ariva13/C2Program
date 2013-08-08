using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;

namespace C2program
{
/*    class RTPClient
    {
        private const int PORT = 1234;      // The port we are listening to
 
        private MemoryStream stream;        // Memory stream which we will write the RTP packet data to
        private UdpClient client;           // UDP client which connects to RTP stream
        private IPEndPoint endPoint;        // Reference to end point of connection
        private Thread listenerThread;      // This thread handles the 'listening' and writes packets to the stream as the arrive
        private bool listening = false;     // If true, the client is currently listening for packets
        private bool printHeader = true;    // Whether or not to print off the packet header information to the console
        private Form1 form1;

        public MemoryStream Stream
        {
            get { return stream; }
            set { stream = value; }
        }

        /// <summary>
        /// RTP Client for receiving an RTP stream containing a WAVE audio stream
        /// </summary>
        /// <param name="port">The port to listen on</param>
        public RTPClient(Form1 form)
        {
            //Console.Title = "RTP Client";
            stream = new MemoryStream();
            form1 = form;
        }
 
        /// <summary>
        /// Creates a connection to the RTP stream
        /// </summary>
        public void StartClient()
        {
            // Create new UDP client. The IP end point tells us which IP is sending the data
            client = new UdpClient(PORT);
            endPoint = new IPEndPoint(IPAddress.Any, PORT);
 
            listenerThread = new Thread(ReceiveCallback);
            listenerThread.Start();
        }
 
        /// <summary>
        /// Tells the UDP client to stop listening for packets.
        /// </summary>
        public void StopClient()
        {
            // Tell listener thread that we are done listening.
            listening = false;
        }
 
        /// <summary>
        /// Handles the receiving of UDP packets from the RTP stream
        /// </summary>
        private void ReceiveCallback()
        {
            listening = true;
 
            while (listening)
            {
                // Receive RTP packet
                byte[] packet = client.Receive(ref endPoint);
 
                // Decode the header of the packet.
                // Each piece of information has a start bit and an end bit.
                // The GetRTPHeaderValue takes the packet, the start bit index and the
                // end bit index and determines what the header value is. The header values
                // are all in Big Endian, which makes things more complicated.
                int version = GetRTPHeaderValue(packet, 0, 1);
                int padding = GetRTPHeaderValue(packet, 2, 2);
                int extension = GetRTPHeaderValue(packet, 3, 3);
                int csrcCount = GetRTPHeaderValue(packet, 4, 7);
                int marker = GetRTPHeaderValue(packet, 8, 8);
                int payloadType = GetRTPHeaderValue(packet, 9, 15);
                int sequenceNum = GetRTPHeaderValue(packet, 16, 31);
                int timestamp = GetRTPHeaderValue(packet, 32, 63);
                int ssrcId = GetRTPHeaderValue(packet, 64, 95);
 
                if (printHeader)
                {
                    String streamStatus = "{" + version + "}" + sequenceNum;
                    form1.msgBox = streamStatus;
                    Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8}",
                        version, padding, extension, csrcCount, marker, payloadType,
                        sequenceNum, timestamp, ssrcId);
                }
 
                stream.Write(packet, 12, packet.Length - 12);
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
    }
*/ 
    
    /// <summary>
    /// Connects to an RTP stream and listens for data
    /// </summary>
    public class RTPReceiver
    {
        private const int AUDIO_BUFFER_SIZE = 65536;

        private UdpClient client;
        private IPEndPoint endPoint;
        private SpeechStreamer audioStream;
        private bool writeHeaderToConsole = false;
        private bool listening = false;
        private int port;
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
        /// <param name="port">The port to listen on</param>
        public RTPReceiver(int port)
        {
            Console.WriteLine(" [RTPClient] Loading...");

            this.port = port;

            // Initialize the audio stream that will hold the data
            audioStream = new SpeechStreamer(AUDIO_BUFFER_SIZE);

            Console.WriteLine(" Done");
        }

        /// <summary>
        /// Creates a connection to the RTP stream
        /// </summary>
        public void StartClient()
        {
            // Create new UDP client. The IP end point tells us which IP is sending the data
            client = new UdpClient(port);
            endPoint = new IPEndPoint(IPAddress.Any, port);

            listening = true;
            listenerThread = new Thread(ReceiveCallback);
            listenerThread.Start();

            Console.WriteLine(" [RTPClient] Listening for packets on port " + port + "...");
        }

        /// <summary>
        /// Tells the UDP client to stop listening for packets.
        /// </summary>
        public void StopClient()
        {
            // Set the boolean to false to stop the asynchronous packet receiving
            listening = false;
            Console.WriteLine(" [RTPClient] Stopped listening on port " + port);
        }

        /// <summary>
        /// Handles the receiving of UDP packets from the RTP stream
        /// </summary>
        /// <param name="ar">Contains packet data</param>
        private void ReceiveCallback()
        {
            // Begin looking for the next packet
            while (listening)
            {
                // Receive packet
                byte[] packet = client.Receive(ref endPoint);

                // Decode the header of the packet
                int version = GetRTPHeaderValue(packet, 0, 1);
                int padding = GetRTPHeaderValue(packet, 2, 2);
                int extension = GetRTPHeaderValue(packet, 3, 3);
                int csrcCount = GetRTPHeaderValue(packet, 4, 7);
                int marker = GetRTPHeaderValue(packet, 8, 8);
                int payloadType = GetRTPHeaderValue(packet, 9, 15);
                int sequenceNum = GetRTPHeaderValue(packet, 16, 31);
                int timestamp = GetRTPHeaderValue(packet, 32, 63);
                int ssrcId = GetRTPHeaderValue(packet, 64, 95);

                if (writeHeaderToConsole)
                {
                    Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8}",
                        version,
                        padding,
                        extension,
                        csrcCount,
                        marker,
                        payloadType,
                        sequenceNum,
                        timestamp,
                        ssrcId);
                }

                // Write the packet to the audio stream
                audioStream.Write(packet, 12, packet.Length - 12);
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
    }
 
}
