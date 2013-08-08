using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpeechLib;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace C2program
{
    public class SpeechStreamer : Stream, /*ISpeechBaseStream,*/ System.Runtime.InteropServices.ComTypes.IStream
    {
        private AutoResetEvent _writeEvent;
        private List<byte> _buffer;
        private int _buffersize;
        private int _readposition;
        private int _writeposition;
        private bool _reset;
        private SpAudioFormat format;
        private Stopwatch readTimer;
        private int myReadTimeout; //read timeout in milliseconds

        public SpeechStreamer(int bufferSize)
        {
            _writeEvent = new AutoResetEvent(false);
            _buffersize = bufferSize;
            _buffer = new List<byte>(_buffersize);
            for (int i = 0; i < _buffersize; i++)
                _buffer.Add(new byte());
            _readposition = 0;
            _writeposition = 0;
            this.ReadTimeout = Int32.MaxValue;
            readTimer = new Stopwatch();
            readTimer.Start();
        }

        public SpeechStreamer(int bufferSize, int readTimeout) : this(bufferSize)
        {
            this.ReadTimeout = readTimeout;
        }

        public override int ReadTimeout
        {
            get
            {
                return myReadTimeout;
            }
            set
            {
                myReadTimeout = value;
            }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return -1L; }
        }

        public override long Position
        {
            get { return 0L; }
            set { }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0L;
        }

        public override void SetLength(long value)
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            readTimer.Restart();
            int i = 0;
            while (i < count && _writeEvent != null && readTimer.ElapsedMilliseconds < this.ReadTimeout)
            {
//                Console.WriteLine("[SpeechStreamer]: readTimer elapsed time: " + readTimer.ElapsedMilliseconds + " elapsed: " + readTimer.Elapsed);
                if (!_reset && _readposition >= _writeposition)
                {
                    _writeEvent.WaitOne(Math.Min(ReadTimeout,100), true);
                    continue;
                }
                buffer[i] = _buffer[_readposition + offset];
                _readposition++;
                if (_readposition == _buffersize)
                {
                    _readposition = 0;
                    _reset = false;
                }
                i++;
            }

            return i;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = offset; i < offset + count; i++)
            {
                _buffer[_writeposition] = buffer[i];
                _writeposition++;
                if (_writeposition == _buffersize)
                {
                    _writeposition = 0;
                    _reset = true;
                }
            }
            _writeEvent.Set();

        }

        public override void Close()
        {
            _writeEvent.Close();
            _writeEvent = null;
            base.Close();
        }

        public override void Flush()
        {

        }

        public SpAudioFormat Format
        {
            get
            {
                return format;
            }
            set
            {
                format = value;
            }
        }
/*
        public int Read(out object Buffer, int NumberOfBytes)
        {
            byte[] buffer = new byte[_buffersize];
            int bytesRead = Read(buffer, 0, NumberOfBytes);
            Buffer = buffer;
            return bytesRead;
        }

        public dynamic Seek(object Position, SpeechStreamSeekPositionType Origin = SpeechStreamSeekPositionType.SSSPTRelativeToStart)
        {
            return Seek(0, 0);
        }

        public int Write(object Buffer)
        {
            byte[] buffer = new byte[_buffersize];
            buffer = (byte[]) Buffer;
            Write(buffer, 0, buffer.Length);
            return buffer.Length;
        }
 */


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
            byte[] buffer = new byte[_buffersize];
            int bytesRead = Read(pv, 0, cb);
            if (pcbRead != IntPtr.Zero) Marshal.WriteInt32(pcbRead, bytesRead);
        }

        public void Revert()
        {
            throw new NotImplementedException();
        }

        public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
        {
            return;
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
            Write(pv, 0, cb);
            if (pcbWritten != IntPtr.Zero) Marshal.WriteInt32(pcbWritten, cb);
        }
    }
}
