using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MultipleMusicPlayer.Buffer {
    internal interface IBuffer {
        void Write(byte[] b, IBufferState bufferState);
        int Read(byte[] b, IBufferState bufferState);
        int Read(IntPtr ptr, IBufferState bufferState);
        void Write(FileStream fileStream);
        //void CreateNext();
        //int GetBufferLength(IBufferState bufferState);
        void SeekOfWrite(IBufferState bufferState);
        bool IsFull(IBufferState bufferState);
        void DeleteOperate(IBufferState bufferState);
        bool IsEndOfBuffer(IBufferState bufferState);
        void SetEndOfBuffer(IBufferState bufferState);
        bool IsInterrupt();
        void SetInterrupt(bool interrupt);
        void Clear();
        object UserData { get; set; }
    }
}
