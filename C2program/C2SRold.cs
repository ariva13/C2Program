using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpeechLib;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Timers;


namespace C2program
{
    class C2SRold
    {
        //private SpSharedRecoContext recoContext;
        private SpInProcRecoContext recoContext;
        private ISpeechRecoGrammar grammar;
        private C2Voice voice;
        private Form1 form1;
        private C2gpio gpio;
        private enum State { IDLE, LISTEN };
        private State state;
        C2SRStream mySrStream;
        private RTPReceiver rtpClient;
        private int missunderstandCount;
        private Timer C2attentionTimer;

/*        public SpInProcRecoContext RecoContext
        {
            get { return recoContext; }
            set { recoContext = value; }
        }
        */
        public ISpeechRecoGrammar Grammar
        {
            get { return grammar; }
            set { grammar = value; }
        }

        public C2SRold(Form1 form)
        {
            form1 = form;
            gpio = new C2gpio(1,"");
            state = State.IDLE;
            voice = new C2Voice(1);
            C2attentionTimer = new Timer(30000); //60 second time out for C2 to stop listening
            C2attentionTimer.Elapsed += new ElapsedEventHandler(C2attentionTimer_Elapsed);
            C2attentionTimer.AutoReset = false;

            missunderstandCount = 0;
            voice.Speak("C2 standing by and awaiting your instructions!");

            //recoContext = new SpSharedRecoContext();
            recoContext = new SpInProcRecoContext();
            
            //set up the socket stream first
            //IPEndPoint receiver = new IPEndPoint(new IPAddress(("192.168.2.101"), 1234);
//            UdpClient udpClient = new UdpClient("192.168.2.101", 1234);
            //UdpClient udpClient = new UdpClient(1234);
            //udpClient.Connect(receiver);
//            Socket socket = udpClient.Client;

            //TcpClient tcpClient = new TcpClient("192.168.2.101", 1234);
//            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
//            socket.Connect("192.168.2.101", 1234);
//            if (!socket.Connected)
//            {
//                form1.statusMsg = "socket was never connected!";
//                return;
//            }

            //SpMMAudioIn instream = new SpMMAudioIn();
//            ASRStreamClass myAsrStream = new ASRStreamClass();
//            mySrStream = new C2SRStream("192.168.2.101", 1234);
            rtpClient = new RTPReceiver(1234);
            rtpClient.StartClient();
            SpCustomStream stream = new SpCustomStream();
//            stream.BaseStream = (System.Runtime.InteropServices.ComTypes.IStream)mySrStream;
//            stream.BaseStream = (System.Runtime.InteropServices.ComTypes.IStream)rtpClient.AudioStream;
            stream.BaseStream = rtpClient.AudioStream;
            //SpStream st = new SpStream();
            //st.
            


            //m_GrammarID = 1;
            Grammar = this.recoContext.CreateGrammar(0);
            Grammar.DictationLoad("", SpeechLoadOption.SLOStatic);
            //our program doesn't do this
            Grammar.DictationSetState(SpeechRuleState.SGDSActive);
            //our program doesn't do this

            //            ISpeechGrammarRule CommandsRule;
            //            CommandsRule = Grammar.Rules.Add("CommandsRule", SpeechRuleAttributes.SRATopLevel | SpeechRuleAttributes.SRADynamic, 1);
            //            CommandsRule.Clear();
            //            object dummy = 0;
            //            string sCommand = "see";
            //            CommandsRule.InitialState.AddWordTransition(null, sCommand, " ", SpeechGrammarWordType.SGLexical, null, 0, ref dummy, 0);
            //            Grammar.Rules.Commit();
            //            Grammar.CmdSetRuleState("CommandsRule", SpeechRuleState.SGDSActive);
            //stream.get
            this.recoContext.Recognizer.AudioInputStream = stream;
            //this.recoContext.Recognizer.AudioInputStream = (ISpeechBaseStream) stream.BaseStream;
            //this.recoContext.Recognizer.AudioInputStream = (ISpeechBaseStream)rtpClient.Stream;
            //RecoContext.EventInterests = SpeechRecoEvents.SREAllEvents;
            //RecoContext.RetainedAudioFormat.Type = SpeechAudioFormatType.SAFT32kHz16BitMono;
            recoContext.RetainedAudioFormat.Type = SpeechAudioFormatType.SAFT24kHz16BitMono;
            //RecoContext.EventInterests = SPSEMANTICFORMAT. SRERecognition + SRESoundEnd + SREStreamEnd + SREStreamStart + SRESoundEnd;
            recoContext.Recognition += new SpeechLib._ISpeechRecoContextEvents_RecognitionEventHandler(InterpretCommand);
            //RecoContext.Recognition += new _ISpeechRecoContextEvents_

            recoContext.Recognizer.SetPropertyNumber("AdaptationOn", 0);
        }

        ~C2SRold()
        {
            rtpClient.StopClient();
        }

        public void InterpretCommand(int StreamNumber, object StreamPosition, SpeechLib.SpeechRecognitionType RecognitionType, SpeechLib.ISpeechRecoResult Result)
        {
            //handle the event
            string sCommand = Result.PhraseInfo.GetText(0, -1, true);
            sCommand = sCommand.ToLower();
            form1.statusMsg = "C2 has recognized a command";
            switch (state)
            {
                case State.IDLE:
                    if (sCommand.Contains("c2") || ((sCommand.Contains("see") || sCommand.Contains('c') || sCommand.Contains("sea")) && 
                        (sCommand.Contains("too") || sCommand.Contains("two") || sCommand.Contains("to") || sCommand.Contains("2")) ))
                    {
                        form1.statusMsg = "Awaiting Command:";
                        voice.ShortAcknowlege();
                        
                        state = State.LISTEN;
                        //C2attentionTimer.Start();
                    }
                    else
                    {
                        missunderstandCount++;
                        form1.statusMsg = "C2 is waiting for \"C2\" command only";
                    }
                    break;
                case State.LISTEN:
                    if (sCommand.Contains("good morning"))
                    {
                        form1.statusMsg = "good morning";
                        voice.Speak("Good Morning, how are you today?");
                        missunderstandCount = 0;
                        state = State.IDLE;
                    }
                    else if (sCommand.Contains("lights") && sCommand.Contains("on"))
                    {
                        form1.statusMsg = "lights going on";
                        voice.Speak("Lights On");
                        gpio.setGpioValue("192.168.113.101", 25, 1);
                        missunderstandCount = 0;
                        state = State.IDLE;
                    }
                    else if (sCommand.Contains("lights") && sCommand.Contains("off"))
                    {
                        form1.statusMsg = "lights going off";
                        voice.Speak("Lights Off");
                        gpio.setGpioValue("192.168.113.101", 25, 0);
                        missunderstandCount = 0;
                        state = State.IDLE;
                    }
                    else
                    {
                        missunderstandCount++;
                        form1.statusMsg = "C2 has recognized a command no comprendo";
                    }
                    break;
                default:
                    form1.statusMsg = "C2 is in BAD STATE";
                    break;
            }

        }
/*        public long TestStream()
        {
            byte[] buffStream = new byte[2000];
            int bytesToRead = 300;
            int br = 0;
            IntPtr bytesRead = new IntPtr(br);
            mySrStream.Read(buffStream, bytesToRead, bytesRead);
            //public void Read(byte[] pv, int cb, IntPtr pcbRead)
            form1.msgBox = "Trying to read " + bytesToRead + ": read " + bytesRead+" bytes";
            return bytesRead.ToInt32();
        }
*/
        public void C2attentionTimer_Elapsed(object source, ElapsedEventArgs e)
        {
            state = State.IDLE;
        }
    }
}
