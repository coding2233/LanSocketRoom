using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

namespace wanderer.lan
{
    public class LanStream : IDisposable
    {
        private MemoryStream _buffer;
        public BinaryReader Reader { get; private set; }
        public BinaryWriter Writer { get; private set; }

        public long Length
        {
            get
            {
                return _buffer.Length;
            }
        }

        public LanStream()
        {
            _buffer = new MemoryStream();

            Writer = new BinaryWriter(_buffer);
            Reader = new BinaryReader(_buffer);
        }

        public void Dispose()
        {
            Reader.Dispose();
            Writer.Dispose();
            _buffer.Dispose();

            Reader = null;
            Writer = null;
            _buffer = null;
        }

        public void Flush()
        {
            _buffer.Flush();
        }

        public void SetSeekOrigin(SeekOrigin seekOrigin)
        {
            _buffer.Seek(0, seekOrigin);
        }

    }
}
