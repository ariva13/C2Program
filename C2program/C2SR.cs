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
using System.Diagnostics;
using System.Speech.Recognition;


namespace C2program
{
    class C2SR
    {
        //private SpSharedRecoContext recoContext;
        private SpInProcRecoContext[] recoContext;
        private ISpeechRecoGrammar[] grammar;
        private C2Voice[] voice;
        private Form1 form1;
        private C2gpio gpio;
        private enum State { IDLE, LISTEN, ALARM_CODE };
        private State state;
        C2SRStream mySrStream;
        private RTPReceiver[] rtpClient;
        private RTPServer[] rtpServer;
        private int missunderstandCount;
        private Timer c2attentionTimer;
        private Timer c2MotionTimer;
        private int numZones;
        private String[] zoneAddresses;
        private String appAddress;
        private bool[] zoneMotion;
        private bool[] lightsOccupied;
        private const int zonePortBase = 2000;
        private const int motionPort = 24; //TODO: check gpio pin that motion sensor is connected to
        private Dictionary<string, int> myLocationMap; //stores string zone number pairs where key is the location and value is the zone

/*        public SpInProcRecoContext RecoContext
        {
            get { return recoContext; }
            set { recoContext = value; }
        }

        public ISpeechRecoGrammar Grammar
        {
            get { return grammar; }
            set { grammar = value; }
        }
*/
        public int NumZones
        {
            get { return numZones; }
            set { numZones = value; }
        }

        public C2SR(Form1 form, int numZone)
        {
            form1 = form;
            numZones = numZone;
            appAddress = "192.168.113.90";
            gpio = new C2gpio(numZones,appAddress);
            state = State.IDLE;
            voice = new C2Voice[numZones];
            rtpClient = new RTPReceiver[numZones];
            rtpServer = new RTPServer[numZones];
            recoContext = new SpInProcRecoContext[numZones];
            grammar = new ISpeechRecoGrammar[numZones];

            //Set up unique variables (1 for all zones)
            myLocationMap = CreateLocationMap();
            
            //Set up zones
            zoneAddresses = new String[numZones];
            zoneMotion = new bool[numZones];
            lightsOccupied = new bool[numZones];

            for (int i = 0; i < numZones; i++)
            {
                zoneAddresses[i] = "192.168.113." + (100 + i + 1);
                zoneMotion[i] = false;
                lightsOccupied[i] = false;
            }
            
            

            missunderstandCount = 0;

            for (int i = 0; i < numZones; i++)
            {
                InitZone(i + 1);
                voice[i].Speak("Zone " + (i + 1) + " standing by.");
            }

            //Set up timers
            //Initialize c2attentionTimer to be ready for a time out to stop listening (don't start till listening)
            c2attentionTimer = new Timer(30000); //30 second time out for C2 to stop listening
            c2attentionTimer.Elapsed += new ElapsedEventHandler(C2attentionTimer_Elapsed);
            c2attentionTimer.AutoReset = true;
            
            //Initialize c2MotionTimer to check motion sensors every 2 seconds
            c2MotionTimer = new Timer(2000);
            c2MotionTimer.Elapsed += new ElapsedEventHandler(c2MotionTimer_Elapsed);
            c2MotionTimer.AutoReset = true;
            c2MotionTimer.Start();
            

            voice[0].Speak("C2 standing by and awaiting your instructions!");

            
        }

        ~C2SR()
        {
            foreach (RTPReceiver client in rtpClient)
            {
                client.StopClient();
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
        private void CreateGrammar(int zoneNum)
        {
            //m_GrammarID = 1;
            grammar[zoneNum-1] = recoContext[zoneNum-1].CreateGrammar(0);
            grammar[zoneNum-1].DictationLoad("", SpeechLoadOption.SLOStatic);
            //our program doesn't do this
            grammar[zoneNum-1].DictationSetState(SpeechRuleState.SGDSActive);
            //our program doesn't do this

            //            ISpeechGrammarRule CommandsRule;
            //            CommandsRule = Grammar.Rules.Add("CommandsRule", SpeechRuleAttributes.SRATopLevel | SpeechRuleAttributes.SRADynamic, 1);
            //            CommandsRule.Clear();
            //            object dummy = 0;
            //            string sCommand = "see";
            //            CommandsRule.InitialState.AddWordTransition(null, sCommand, " ", SpeechGrammarWordType.SGLexical, null, 0, ref dummy, 0);
            //            Grammar.Rules.Commit();
            //            Grammar.CmdSetRuleState("CommandsRule", SpeechRuleState.SGDSActive);
                        //string[] commands = new string[] {"see two", "good morning", "how are you","check"};

                        //ISpeechGrammarRule CommandsRule;
                        //CommandsRule = grammar[zoneNum-1].Rules.Add("CommandsRule",
                        //    SpeechRuleAttributes.SRATopLevel | SpeechRuleAttributes.SRADynamic, 1);

                        //CommandsRule.Clear();
                        //foreach (string command in commands)
                        //{
                        //    object dummy = 0;
                        //    //string sCommand = theRow["Command"].ToString();
                        //    CommandsRule.InitialState.AddWordTransition(null, 
                        //        command," ",SpeechGrammarWordType.SGLexical,null,0,
                        //        ref dummy,0);
                        //}
                        //grammar[zoneNum - 1].Rules.Commit();
                        //grammar[zoneNum - 1].CmdSetRuleState("CommandsRule", SpeechRuleState.SGDSActive);
/*
            grammar[zoneNum - 1].CmdLoadFromFile(@"..\..\c2grammar.txt");
            //grammar[zoneNum - 1].Rules.Commit();
            grammar[zoneNum - 1].State = SpeechGrammarState.SGSEnabled;
            Grammar g = new Grammar(@"..\..\c2grammar.txt");
            recoContext.load
*/            
        }

        private void InitZone(int zoneNum)
        {
            //Set up the zone computers to transmit speech and receive audio
            if (zoneNum < 3)
            {
//                Process.Start(@"C:\Users\Blake\Documents\Programming\CSharp\C2program\C2program\scripts\pi_speak.bat", Convert.ToString(zoneNum));
                Process.Start(@"..\..\scripts\pi_speak.bat", Convert.ToString(zoneNum));
            }
            else
            {
//                Process.Start(@"C:\Users\Blake\Documents\Programming\CSharp\C2program\C2program\scripts\pi_speak_new_mic.bat", Convert.ToString(zoneNum));
                Process.Start(@"..\..\scripts\pi_speak_new_mic.bat", Convert.ToString(zoneNum));
            }
//            Process.Start(@"C:\Users\Blake\Documents\Programming\CSharp\C2program\C2program\scripts\pi_listen.bat", Convert.ToString(zoneNum));
            Process.Start(@"..\..\scripts\pi_listen.bat", Convert.ToString(zoneNum));
            
            //Set up the voice for each zone
//            rtpServer[zoneNum - 1] = new RTPServer(zoneAddresses[zoneNum - 1], 1234);
//            rtpServer[zoneNum - 1].StartServer();
//            SpCustomStream vStream = new SpCustomStream();
//            vStream.BaseStream = rtpServer[zoneNum - 1].AudioStream;
            voice[zoneNum - 1] = new C2Voice(zoneNum);
//            voice[zoneNum - 1].Voice.AudioOutputStream = vStream;
            
            
            
            //recoContext = new SpSharedRecoContext();
            recoContext[zoneNum-1] = new SpInProcRecoContext();

            //set up the socket stream first

            //            mySrStream = new C2SRStream("192.168.2.101", 1234);
            rtpClient[zoneNum-1] = new RTPReceiver(zonePortBase+zoneNum);
            rtpClient[zoneNum-1].StartClient();
            SpCustomStream stream = new SpCustomStream();
            //            stream.BaseStream = (System.Runtime.InteropServices.ComTypes.IStream)mySrStream;
            //            stream.BaseStream = (System.Runtime.InteropServices.ComTypes.IStream)rtpClient.AudioStream;
            stream.BaseStream = rtpClient[zoneNum-1].AudioStream;
            //SpStream st = new SpStream();

            CreateGrammar(zoneNum);
            
            this.recoContext[zoneNum-1].Recognizer.AudioInputStream = stream;
            //this.recoContext.Recognizer.AudioInputStream = (ISpeechBaseStream) stream.BaseStream;
            //this.recoContext.Recognizer.AudioInputStream = (ISpeechBaseStream)rtpClient.Stream;
            //RecoContext.RetainedAudioFormat.Type = SpeechAudioFormatType.SAFT32kHz16BitMono;
            if (zoneNum < 3)
            {
                recoContext[zoneNum - 1].RetainedAudioFormat.Type = SpeechAudioFormatType.SAFT24kHz16BitMono;
            }
            else
            {
                recoContext[zoneNum - 1].RetainedAudioFormat.Type = SpeechAudioFormatType.SAFT48kHz16BitMono;
            }
            //RecoContext.RetainedAudioFormat.Type = SpeechAudioFormatType.SAFT12kHz16BitMono;
            //RecoContext.EventInterests = SPSEMANTICFORMAT. SRERecognition + SRESoundEnd + SREStreamEnd + SREStreamStart + SRESoundEnd;
            recoContext[zoneNum-1].Recognition += new SpeechLib._ISpeechRecoContextEvents_RecognitionEventHandler(InterpretCommand);
            //RecoContext.Recognition += new _ISpeechRecoContextEvents_

            recoContext[zoneNum-1].Recognizer.SetPropertyNumber("AdaptationOn", 0); //turns adaptation off so it doesn't train to noise

            
        }

        public void InterpretCommand(int StreamNumber, object StreamPosition, SpeechLib.SpeechRecognitionType RecognitionType, SpeechLib.ISpeechRecoResult Result)
        {
            //Get zone the command was heard on
            int commandZoneIdx = 0;
            for (int i = 0; i < numZones; i++)
            {
                if (recoContext[i].Equals(Result.RecoContext))
                {
                    commandZoneIdx = i;
                    break;
                }
            }

            //handle the event
            string sCommand = Result.PhraseInfo.GetText(0, -1, true);
            sCommand = sCommand.ToLower();
            form1.statusMsg = "C2 has recognized a command";
            Console.WriteLine("Received command from " + (commandZoneIdx + 1) + ": " + sCommand);
            switch (state)
            {
                case State.IDLE:
                    if (sCommand.Contains("c2") || ((sCommand.Contains("see") || sCommand.Contains('c') || sCommand.Contains("sea")) &&
                        (sCommand.Contains("too") || sCommand.Contains("two") || sCommand.Contains("to") || sCommand.Contains("2"))))
                    {
                        form1.statusMsg = "Awaiting Command:";
                        //voice[commandZoneIdx].ShortAcknowlege();
                        ResetAttentionTimer(30000); //after 30 seconds it should stip listening

                        //state = State.LISTEN;
                        goto case State.LISTEN;
                        //C2attentionTimer.Start();
                    }
                    else
                    {
                        missunderstandCount++;
                        form1.statusMsg = "C2 is waiting for \"C2\" command only";
                    }
                    break;
                case State.LISTEN:
                    if (sCommand.Contains("emergency") || (sCommand.Contains("activate") && sCommand.Contains("alarm")))
                    {
                        form1.statusMsg = "alarm is triggered";
                        voice[commandZoneIdx].Speak("Warning: Alarm has been triggered. Police have been notified and will arrive at the premises shortly.");
                        gpio.AppSetAlarmOn();
                    }
                    else if ((sCommand.Contains("emergency") || (sCommand.Contains("activate") && sCommand.Contains("alarm"))) && sCommand.Contains("off"))
                    {
                        form1.statusMsg = "alarm trigger is off";
                        voice[commandZoneIdx].Speak("Alarming is off");
                        gpio.AppSetAlarmOff();
                    }
                    else if (sCommand.Contains("light color"))
                    {
                        int r, g, b, d, bri;
                        d = 0;
                        if(sCommand.Contains("red"))
                        {
                            r = 255; g = 0; b = 0; bri = 90;
                        }
                        else if (sCommand.Contains("green"))
                        {
                            r = 0; g = 255; b = 0; bri = 90;
                        }
                        else if (sCommand.Contains("blue"))
                        {
                            r = 0; g = 0; b = 255; bri = 90;
                        }
                        else if (sCommand.Contains("purple"))
                        {
                            r = 255; g = 0; b = 255; bri = 90;
                        }
                        else if (sCommand.Contains("default"))
                        {
                            r = 0; g = 0; b = 0; bri = 100; d = 1;
                        }
                        else
                        {
                            r = 0; g = 0; b = 0; bri = 100; d = 1;
                        }
                        int[] location = GetLocation(sCommand);
                        if (location.Length < 1)
                        {
                            location = new int[1] { commandZoneIdx };
                        }
                        form1.statusMsg = "lights changing color in zones idx: " + location.ToString();
                        Console.Write("lights changeing color in zone indexes ");
                        for (int i = 0; i < location.Length; i++)
                        {
                            Console.Write(location[i] + " ");
                        }

                        foreach (int i in location)
                        {
                            if(d == 1)
                            {
                                gpio.AppSetLightColorDefault(i + 1);
                            }
                            else
                            {
                                gpio.AppSetLightColor(i + 1,r,g,b,bri);
                            }
                        }

                        voice[commandZoneIdx].Speak("Color set");

                        missunderstandCount = 0;
                        state = State.IDLE;
                    }
                    else if (sCommand.Contains("good morning"))
                    {
                        form1.statusMsg = "good morning";
                        voice[commandZoneIdx].Speak("Good Morning, how are you today?");
                        //ExecuteCommand("test.txt");
                        missunderstandCount = 0;
                        state = State.IDLE;
                    }
                    else if (sCommand.Contains("how are you"))
                    {
                        form1.statusMsg = "I'm fine";
                        voice[commandZoneIdx].HowAreYou();
                        missunderstandCount = 0;
                        state = State.IDLE;
                    }
                    else if (sCommand.Contains("song"))
                    {
                        form1.statusMsg = "sing";
                        voice[commandZoneIdx].Speak("Twinkle, Twinkle, little star. How I wonder what you are? Up above the world so high, like a diamond in the sky. Twinkle, Twinkle, little star. How I wonder what you are?");
                        missunderstandCount = 0;
                        state = State.IDLE;
                    }
                    else if (sCommand.Contains("lights") && sCommand.Contains(" on ") && (sCommand.Contains("motion") || sCommand.Contains("occupied")))
                    {
                        for (int i = 0; i < numZones; i++)
                        {
                            lightsOccupied[i] = true;
                        }
                        voice[commandZoneIdx].Speak("Lights are now motion activated");
                        missunderstandCount = 0;
                        state = State.IDLE;
                    }
                    else if (sCommand.Contains("lights") && sCommand.Contains("off") && (sCommand.Contains("motion") || sCommand.Contains("occupied")))
                    {
                        for (int i = 0; i < numZones; i++)
                        {
                            lightsOccupied[i] = false;
                        }
                        voice[commandZoneIdx].Speak("Lights are no longer motion activated");
                        missunderstandCount = 0;
                        state = State.IDLE;
                    }
                    else if (sCommand.Contains("lights") && sCommand.Contains("on"))
                    {
                        int[] location = GetLocation(sCommand);
                        if (location.Length < 1)
                        {
                            location = new int[1] { commandZoneIdx };
                        }
                        form1.statusMsg = "lights going on in zone indexes " + location.ToString();
                        Console.Write("lights going on in zone indexes ");
                        for (int i = 0; i < location.Length; i++)
                        {
                            Console.Write(location[i] + " ");
                        }

                        foreach (int i in location)
                        {
                            gpio.setGpioValue(zoneAddresses[i], 25, 1);
                        }

                        voice[commandZoneIdx].Speak("Lights On");

                        missunderstandCount = 0;
                        state = State.IDLE;
                    }
                    else if (sCommand.Contains("lights") && sCommand.Contains("off"))
                    {
                        int[] location = GetLocation(sCommand);
                        if (location.Length < 1)
                        {
                            location = new int[1] { commandZoneIdx };
                        }

                        form1.statusMsg = "lights going off in zone index " + location.ToString();
                        Console.Write("lights going off in zone indexes ");
                        for (int i = 0; i < location.Length; i++)
                        {
                            Console.Write(location[i] + " ");
                        }
                        foreach (int i in location)
                        {
                            gpio.setGpioValue(zoneAddresses[i], 25, 0);
                        }

                        voice[commandZoneIdx].Speak("Lights Off");
                        missunderstandCount = 0;
                        state = State.IDLE;
                    }
                    else if (sCommand.Contains("security") && (sCommand.Contains("arm") || sCommand.Contains("activate")))
                    {
                        voice[commandZoneIdx].Speak("Please enter code to arm security system.");
                        missunderstandCount = 0;
                        state = State.ALARM_CODE;
                        //TODO: how to enter code?
                    }
                    else
                    {
                        if (state == State.IDLE) //meaning that we got into here from goto from C2 recognized phrase (no other command in phrase)
                        {
                            state = State.LISTEN;
                            voice[commandZoneIdx].ShortAcknowlege();
                        }
                        else
                        {
                            missunderstandCount++;
                        }
                        form1.statusMsg = "C2 has recognized a command no comprendo";
                    }
                    break;
                default:
                    form1.statusMsg = "C2 is in BAD STATE";
                    break;
            }

        }

        /* takes in a string containing a location. If any of the locations (keys) from myLocationMap are contained in the msg
         * their zone numbers will be returned
         * returns an int array of the locations
         */
        private int[] GetLocation(string msg)
        {
            List<int> zones = new List<int>();
            Dictionary<string,int>.KeyCollection keyColl = myLocationMap.Keys;
            foreach (string loc in keyColl)
            {
                if (msg.Contains(loc))
                {
                    zones.Add(myLocationMap[loc]);
                }
            }

            //test for whole house options
            if (msg.Contains("whole") || msg.Contains("all") || msg.Contains("every") || msg.Contains("home") || msg.Contains("house"))
            {
                for (int i = 0; i < numZones; i++)
                {
                    zones.Add(i);
                }
            }

            //test upstairs or down stairs
            if (msg.Contains("stairs") || msg.Contains("stair"))
            {
                if (msg.Contains("up"))
                {
                    zones.Add(0);
                    zones.Add(1);
                }
                else if (msg.Contains("down"))
                {
                    zones.Add(2);
                    zones.Add(3);
                }
            }

            List<int> uniqueZones = new List<int>(zones.Distinct());

            return uniqueZones.ToArray();
        }
        
        private Dictionary<string,int> CreateLocationMap()
        {
            Dictionary<string, int> locationMap = new Dictionary<string, int>();
            locationMap.Add("office", 0);
            locationMap.Add("bedroom", 1);
            locationMap.Add("hallway", 2);
            locationMap.Add("kitchen", 3);
            locationMap.Add("living room", 2);
            locationMap.Add("dining room", 3);

            return locationMap;
        }

        public void C2attentionTimer_Elapsed(object source, ElapsedEventArgs e)
        {
            form1.statusMsg = "Stopped Listening - timer";
            state = State.IDLE;
            c2attentionTimer.Stop();
        }

        public void ResetAttentionTimer(double time)
        {
            c2attentionTimer.Stop();
            c2attentionTimer.Interval = time;
            c2attentionTimer.Start();
        }

        void c2MotionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
//            Console.WriteLine("motion timer elapsed, checking motion sensors");
            for (int i = 0; i < numZones; i++)
            {
                string motionVal = gpio.getGpioValue(zoneAddresses[i], motionPort); //TODO verify gpio port motion will be on
                if (Convert.ToInt32(motionVal) > 0)
                {
                    zoneMotion[i] = false;
                }
                else
                {
                    zoneMotion[i] = true;
                }
//                Console.WriteLine("    motionVal[" + i+"] = " + motionVal + "," + Convert.ToInt32(motionVal)+" MotionInZone: " + zoneMotion[i]
//                    +" zoneOccupied: " + lightsOccupied[i]);
                if (state == State.ALARM_CODE)
                {
                    gpio.AppSetAlarmOn();
                }
                else if (lightsOccupied[i])
                {
                    if (zoneMotion[i])
                    {
                        gpio.setGpioValue(zoneAddresses[i], 25, 1);
                    }
                    else
                    {
                        gpio.setGpioValue(zoneAddresses[i], 25, 0);
                    }
                }
            }
        }

        public void ExecuteCommand(string command)
        {

            int ExitCode;
            ProcessStartInfo ProcessInfo;
            Process Process;

            //System.Diagnostics.Process.Start(@"C:\listfiles.bat");
            ProcessInfo = new System.Diagnostics.ProcessStartInfo(@"C:\Users\Blake\Programming\CSharp\C2program\C2program\scripts\test.txt");
            //ProcessInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = false;

            Process = Process.Start(ProcessInfo);
            Process.WaitForExit();

            ExitCode = Process.ExitCode;
            Process.Close();

            //MessageBox.Show("ExitCode: " + ExitCode.ToString(), "ExecuteCommand");
        }
    }
}


