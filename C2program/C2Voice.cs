using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpeechLib;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Net.Sockets;

namespace C2program
{
    class C2Voice
    {
        private SpeechSynthesizer myVoice;
        private Random rand;
        private List<string> shortAffirmation;
        //private int lShortAffirmation;
        private List<string> shortAcknowledge;
        //private int lShortAcknowledge;
        private List<string> howAreYou;
        //private int lHowAreYou;
        private int myZone;
        private Socket myZoneSocket;
        private RTPServer myRtpServer;

        public SpeechSynthesizer Voice
        {
            get { return myVoice; }
            set { myVoice = value; }
        }

        public int Zone
        {
            get { return myZone; }
        }

        public C2Voice(int zone)
        {
            myVoice = new SpeechSynthesizer();
            rand = new Random();
            InitializeVocabulary();
            myZone = zone;
            String host = "192.168.113." + (100+myZone);
            myRtpServer = new RTPServer(host, 1234);
            myRtpServer.StartServer();
            //ConnectSocket(host, 1234);
            //myNetStream = new NetworkStream(myZoneSocket, true);
//            SpeechAudioFormatInfo audioFormat = new SpeechAudioFormatInfo(24000, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
//            audioFormat.EncodingFormat = EncodingFormat.ALaw;
            myVoice.SetOutputToAudioStream(myRtpServer.AudioStream, new SpeechAudioFormatInfo(24000, AudioBitsPerSample.Sixteen, AudioChannel.Mono));

//            myVoice.SetOutputToAudioStream(myRtpServer.AudioStream, new SpeechAudioFormatType
//            SpeechAudioFormatType;
            myVoice.SetOutputToWaveStream(myRtpServer.AudioStream);
        }

/*        public void ConnectSocket(string host, int port)
        {
            myZoneSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            myZoneSocket.Connect(host, port);
        }
*/
        private void InitializeVocabulary()
        {
            shortAffirmation = new List<string>();
            shortAffirmation.Add("It will be done.");
            shortAffirmation.Add("You got it.");
            shortAffirmation.Add("Good as done.");
            shortAffirmation.Add("No Problem.");

            shortAcknowledge = new List<string>();
            shortAcknowledge.Add("yes");
            //shortAcknowledge.Add("yes master");
            //shortAcknowledge.Add("how can I help");
            //shortAcknowledge.Add("yes sir");
            //shortAcknowledge.Add("what can I do");
            //shortAcknowledge.Add("happy to serve");

            howAreYou = new List<string>();
            howAreYou.Add("I'm doing great. Thank you.");
            howAreYou.Add("Better now that you are here.");
            howAreYou.Add("I'm fine, thanks for asking.");
        }

        public void Speak(string message)
        {
            myVoice.SpeakAsync(message+". . . . . . . . . !");
        }

        public void ShortAcknowlege()
        {
            int msg = rand.Next(shortAcknowledge.Count);
            Speak(shortAcknowledge[msg]);
        }

        public void ShortAffirmation()
        {
            int msg = rand.Next(shortAffirmation.Count);
            Speak(shortAffirmation[msg]);
        }

        public void HowAreYou()
        {
            int msg = rand.Next(howAreYou.Count);
            Speak(howAreYou[msg] + ", , , , , , , , , , , , , , , , a.");
        }
    }

    /*
    class C2Voice
    {
        private SpVoice myVoice;
        private Random rand;
        private String[] shortAffirmation;
        private int lShortAffirmation;
        private String[] shortAcknowledge;
        private int lShortAcknowledge;
        private int myZone;

        public SpVoice Voice
        {
            get { return myVoice; }
            set { myVoice = value; }
        }

        public C2Voice(int zone)
        {
            myVoice = new SpVoice();
            rand = new Random();
            InitializeVocabulary();
            myZone = zone;
        }

        private void InitializeVocabulary()
        {
            lShortAffirmation = 4;
            shortAffirmation = new String[lShortAffirmation];
            shortAffirmation[0] = "It will be done.";
            shortAffirmation[1] = "You got it.";
            shortAffirmation[2] = "Good as done.";
            shortAffirmation[3] = "No Problem.";

            lShortAcknowledge = 4;
            shortAcknowledge = new String[lShortAcknowledge];
            shortAcknowledge[0] = "yes";
            shortAcknowledge[1] = "yeah";
            shortAcknowledge[2] = "what";
            shortAcknowledge[3] = "yes sir";
        }

        public void Speak(string message)
        {
            myVoice.Speak(message, SpeechVoiceSpeakFlags.SVSFlagsAsync);
        }

        public void ShortAcknowlege()
        {
            int msg = rand.Next(lShortAcknowledge);
            Speak(shortAcknowledge[msg]);
        }

        public void ShortAffirmation()
        {
            int msg = rand.Next(lShortAffirmation);
            Speak(shortAffirmation[msg]);
        }
    }
    */


}
