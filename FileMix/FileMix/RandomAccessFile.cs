﻿using System;
using System.IO;

namespace FileMix
{

    public class RandomAccessFile
    {
        private FileStream _stream;
        #if NET || NET_2_0
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern int FlushFileBuffers(IntPtr fileHandle);
        #endif

        public RandomAccessFile(String file, String fileMode)
        {
            try
            {
                _stream = new FileStream(file, FileMode.OpenOrCreate, fileMode.Equals("rw") ? FileAccess.ReadWrite : FileAccess.Read);
            }
            catch
            {
                
            }
        }

        public FileStream Stream
        {
            get { return _stream; }
        }

        public void Close()
        {
            _stream.Close();
            _stream.Dispose();
        }

        public long Length()
        {
            return _stream.Length;
        }

        public int Read(byte[] bytes, int offset, int length)
        {
            return _stream.Read(bytes, offset, length);
        }

        public void Read(byte[] bytes)
        {
            _stream.Read(bytes, 0, bytes.Length);
        }

        public void Seek(long pos)
        {
            _stream.Seek(pos, SeekOrigin.Begin);
        }

        public void Sync()
        {
            _stream.Flush();
            #if NET_2_0 
            FlushFileBuffers(_stream.SafeFileHandle.DangerousGetHandle());
            #elif NET
            FlushFileBuffers(_stream.Handle);
            #endif

        }

        public RandomAccessFile GetFD()
        {
            return this;
        }

        public void Write(byte[] bytes)
        {
            Write(bytes, 0, bytes.Length);
        }

        public void Write(byte[] bytes, int offset, int length)
        {
            try
            {
                _stream.Write(bytes, offset, length);
            }
            catch
            {
            }
        }
    }
}
