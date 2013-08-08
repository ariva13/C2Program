using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpeechLib;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace C2program
{
    public class C2SRStream : System.Runtime.InteropServices.ComTypes.IStream
    {
        public string myHost;
        public int myPort;
        public UdpClient client;
        //private byte[] leftOverBytes;

        public string Host
        {
            get
            {
                return myHost;
            }
            set
            {
                myHost = value;
            }
        }
        
        public int Port
        {
            get
            {
                return myPort;
            }
            set
            {
                myPort = value;
            }
        }
        
        public UdpClient udpClient
        {
            get
            {
                return client;
            }
            set
            {
                client = value;
            }
        }
        public SpAudioFormat Format
        {
            get
            {
                return Format;
            }
            set
            {
                Format = value;
            }
        }

        public C2SRStream(String host, int port)
        {
            this.Host = host;
            this.Port = port;
            this.udpClient = new UdpClient("192.168.2.101", 1234);
            //udpClient.DontFragment = true;
            if (!udpClient.Client.Connected)
            {
                throw new Exception("Could not connect the C2SRStream to the host:port entered in the constructor");
            }

        }

        public void Clone(out System.Runtime.InteropServices.ComTypes.IStream ppstm)
        {
            throw new NotImplementedException();
        }

        public void Commit(int grfCommitFlags)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(System.Runtime.InteropServices.ComTypes.IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {
            throw new NotImplementedException();
        }

        public void LockRegion(long libOffset, long cb, int dwLockType)
        {
            throw new NotImplementedException();
        }

        public void Read(byte[] pv, int cb, IntPtr pcbRead)
        {
            byte[] dataRead = null;
            int bytesRead = 0;
            while(udpClient.Available > 0 && bytesRead < cb)
            {
                IPEndPoint ep = null;
                byte[] oldBuff = dataRead;
                byte[] buff = udpClient.Receive(ref ep);
                bytesRead += buff.Length;
                dataRead = oldBuff.Concat(buff).ToArray();
            }
            if(pcbRead != IntPtr.Zero) Marshal.WriteInt32(pcbRead, bytesRead);
            //pcbRead = (IntPtr) bytesRead;
            pv = dataRead;
        }

        public void Revert()
        {
            throw new NotImplementedException();
        }

        public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
        {
            //cannot seek the pointer to a network stream so we will ignore this part.
        }

        public void SetSize(long libNewSize)
        {
            throw new NotImplementedException();
        }

        public void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag)
        {
            throw new NotImplementedException();
        }

        public void UnlockRegion(long libOffset, long cb, int dwLockType)
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] pv, int cb, IntPtr pcbWritten)
        {
            int numBytes = pv.Length;
            int bytesSent = udpClient.Send(pv, numBytes, this.Host, this.Port);
            pcbWritten = (IntPtr)bytesSent;
        }

    }
}
