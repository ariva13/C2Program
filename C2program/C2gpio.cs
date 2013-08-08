using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace C2program
{
    class C2gpio
    {
        public const int INPUT = 0;
        public const int OUTPUT = 1;
        //private Form1 form1;
        private string userName;
        private string passwd;
        private string appAddress;
        private int numZones;

        public C2gpio(int nZones, string appAddr)
        {
            userName = "webiopi";
            passwd = "raspberry";
            numZones = nZones;
            appAddress = appAddr;

            //initialize zones for inputs and outputs
            string zoneAddress = "";
            for (int i = 0; i < nZones; i++)
            {
                zoneAddress = "192.168.113." + (100 + i + 1);
                try
                {
                    setGpioFunction(zoneAddress, 25, OUTPUT);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[C2gpio]: ERROR setting zone " + (i+1) + " gpio 25 to output. " + e.Message);
                }
            }
        }

        public C2gpio(int nZones, string appAddr, Form1 form)
            : this(nZones, appAddr)
        {
            //form1 = form;
        }

        public string HttpPost(string URI, string Parameters, string userName=null, string password=null)
        {
            // Create a request using a URL that can receive a post.
            try
            {
                WebRequest request = WebRequest.Create(URI);
                if (userName != null && password != null)
                {
                    request.Credentials = new NetworkCredential(userName, password);
                }
                // Set the ContentType property of the WebRequest.
                request.ContentType = "application/x-www-form-urlencoded";
                // Set the Method property of the request to POST.
                request.Method = "POST";
                // Create POST data and convert it to a byte array.
                byte[] byteArray = Encoding.UTF8.GetBytes(Parameters);
                //byte[] byteArray = Encoding.ASCII.GetBytes(Parameters);
                // Set the ContentType property of the WebRequest.
                request.ContentType = "application/x-www-form-urlencoded";
                // Set the ContentLength property of the WebRequest.
                request.ContentLength = byteArray.Length;
                // Get the request stream.
                Stream dataStream = request.GetRequestStream();
                // Write the data to the request stream.
                dataStream.Write(byteArray, 0, byteArray.Length);
                // Close the Stream object.
                dataStream.Close();
                // Get the response.
                WebResponse response = request.GetResponse();
                // Display the status.
                //            form1.statusMsg = ((HttpWebResponse)response).StatusDescription;
                // Get the stream containing content returned by the server.
                if (response == null)
                {
                    return null;
                }
                //= response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(response.GetResponseStream());
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                // Clean up the streams.
                reader.Close();
                //            dataStream.Close();
                response.Close();
                return responseFromServer;
            }
            catch (Exception e)
            {
                Console.WriteLine("[C2gpio]: Exception caught when sending HttpPost: " + e.Message);
                return null;
            }
        }

        public string HttpGet(string URI, string Parameters, string userName = null, string password = null)
        {
            try
            {
                // Create a request using a URL that can receive a post. 
                WebRequest request = WebRequest.Create(URI);
                if (userName != null && password != null)
                {
                    request.Credentials = new NetworkCredential(userName, password);
                }
                // Set the Method property of the request to POST.
                request.Method = "GET";
                // Create POST data and convert it to a byte array.
                byte[] byteArray = Encoding.UTF8.GetBytes(Parameters);
                // Set the ContentType property of the WebRequest.
                request.ContentType = "application/x-www-form-urlencoded";
                // Set the ContentLength property of the WebRequest.
                request.ContentLength = byteArray.Length;
                // Get the request stream.
                //            Stream dataStream = request.GetRequestStream();
                // Write the data to the request stream.
                //            dataStream.Write(byteArray, 0, byteArray.Length);
                // Close the Stream object.
                //            dataStream.Close();
                // Get the response.
                WebResponse response = request.GetResponse();
                // Display the status.
                //            form1.statusMsg = ((HttpWebResponse)response).StatusDescription;
                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                // Clean up the streams.
                reader.Close();
                dataStream.Close();
                response.Close();
                return responseFromServer;
            }
            catch (Exception e)
            {
                Console.WriteLine("[C2gpio]: Exception caught when sending HttpGet: " + e.Message);
                return null;
            }
        }

        public string getGpioValue(string address, int gpioNum)
        {
            string URI = "http://" + address + ":8000/GPIO/" + gpioNum + "/value";
            return HttpGet(URI, "", userName, passwd);
        }

        public string setGpioValue(string address, int gpioNum, int value)
        {
            string URI = "http://" + address + ":8000/GPIO/" + gpioNum + "/value/" + value;
            return HttpPost(URI, "", userName, passwd);
        }

        public string getGpioFunction(string address, int gpioNum)
        {
            string URI = "http://" + address + ":8000/GPIO/" + gpioNum + "/function";
            return HttpGet(URI, "", userName, passwd);
        }

        public string setGpioFunction(string address, int gpioNum, int function)
        {
            string URI = "";
            switch(function)
            {
                case INPUT:
                    URI = "http://" + address + ":8000/GPIO/" + gpioNum + "/function/in";
                    break;
                case OUTPUT:
                    URI = "http://" + address + ":8000/GPIO/" + gpioNum + "/function/out";
                    break;
            }
            return HttpPost(URI, "", userName, passwd);
        }

        public string AppSetAlarmOn()
        {
            string URI = "http://" + appAddress + ":8000/alarm/1";
            return HttpPost(URI, "");
        }

        public string AppSetAlarmOff()
        {
            string URI = "http://" + appAddress + ":8000/alarm/0";
            return HttpPost(URI, "");
        }

        public string AppSetLightColor(int zoneNumber, int r, int g, int b, int brightness)
        {
            if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255 || brightness < 0 || brightness > 100 || zoneNumber < 0 || zoneNumber > numZones)
            {
                Console.WriteLine("[C2gpio] ERROR: invalid parameter sent to AppSetLightColor zone: " + zoneNumber + " red: " + r +
                    " g: " + g + " b: " + b + " bri: " + brightness);
            }
            string parameters = "r=" + r + "&g=" + g + "&b=" + b + "&bri=" + brightness;
            string URI = "http://" + appAddress + ":8000/hue/" + zoneNumber + "?" + parameters;
            return HttpPost(URI, parameters);
        }

        public string AppSetLightColorDefault(int zoneNumber)
        {
            string parameters = "default=1";
            string URI = "http://" + appAddress + ":8000/hue/" + zoneNumber + "?" + parameters;
            return HttpPost(URI, parameters);
        }
    }
}
