using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.Diagnostics;
//using System.Speech.Recognition;

namespace C2program
{
    public partial class Form1 : Form
    {
        private C2SR c2;
        //private RTPClient client;
        //private C2gpio gpio;
        private int lightStatus;
        
        public String statusMsg
        {
            get
            {
                return label1.Text;
            }
            set
            {
                label1.Text = value;
            }
        }

        public String msgBox
        {
            get
            {
                return textBox2.Text;
            }
            set
            {
                textBox2.Text = value;
            }
        }
        
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            label1.Text = "Starting C2 master controller";
            lightStatus = 0;
            int numZones = 3;
            c2 = new C2SR(this, numZones);
            //c2 = new C2SRold(this);
            //gpio = new C2gpio(this);
            //client = new RTPClient(this);
            //client.StartClient();
            
            
            //Console.WriteLine("Starting C2 master controller");
            

        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            //client.StopClient();
/*            if (lightStatus == 0)
            {
                gpio.setGpioValue("192.168.2.101", 25, 1);
                lightStatus = 1;
            }
            else
            {
                gpio.setGpioValue("192.168.2.101", 25, 0);
                lightStatus = 0;
            }
*/            //long bytesRead = c2.TestStream();
            //statusMsg = "bytesRead = " + bytesRead;

        }

    }
}
