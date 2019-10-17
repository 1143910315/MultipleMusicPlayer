using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MultipleMusicPlayer.Buffer {
    class Buffer : IBuffer {
        private class Data {
            public object UserData;
        }
        private byte[] buff;
        private readonly int bufferLength;
        private int wroteLength;
        private readonly Buffer previous;
        private Buffer next;
        private readonly int index;
        private readonly object create = new object();
        private readonly object wrote = new object();
        public object UserData {
            get {
                if (create is Data data) {
                    return data.UserData;
                }
                return null;
            }
            set {
                if (create is Data data) {
                    data.UserData = value;
                }
            }
        }
        public Buffer(int length) {
            if (length <= 0) {
                throw new Exception("length不能为负数！");
            }
            create = new Data();
            bufferLength = length;
        }
        private Buffer(Buffer buffer) {
            bufferLength = buffer.bufferLength;
            index = buffer.index;
            previous = buffer;
        }
        private void Delete() {
            lock (wrote) {
                wroteLength = 0;
            }
        }
        public void DeleteOperate(IBufferState bufferState) {
            IBuffer buffer;
            (_, _, _, _, buffer) = bufferState.Info();
            if (buffer is Buffer temp) {
                temp.Delete();
            }
        }
        private int GetWriteAbsolutePosition() {
            lock (wrote) {
                return index * bufferLength + wroteLength;
            }
        }
        private Buffer Find(IBuffer buffer, int position) {
            int move1, move2, startPosition = index * bufferLength;
            if (position >= startPosition) {
                move1 = (position - startPosition) / bufferLength;
            } else {
                move1 = (int)Math.Floor((position - startPosition) / (double)bufferLength);
            }
            if (buffer is Buffer temp) {
                startPosition = temp.index * bufferLength;
                if (position >= startPosition) {
                    move2 = (position - startPosition) / bufferLength;
                } else {
                    move2 = (int)Math.Floor((position - startPosition) / (double)bufferLength);
                }
                if (Math.Abs(move1) <= Math.Abs(move2)) {
                    temp = this;
                } else {
                    move1 = move2;
                }
            } else {
                temp = this;
            }
            for (int i = 0; i < Math.Abs(move1); i++) {
                if (move1 > 0) {
                    temp = temp.next ?? temp.CreateNext();
                } else {
                    temp = temp.previous;
                }
            }
            return temp;
        }
        public bool IsFull(IBufferState bufferState) {
            IBuffer buffer;
            int position;
            (_, _, _, position, buffer) = bufferState.Info();
            Buffer temp = Find(buffer, position);
            return bufferLength == temp.wroteLength;
        }
        public int Read(byte[] b, IBufferState bufferState) {
            int position, start, length, bufferStart;
            IBuffer buffer;
            Buffer temp;
            (_, start, length, position, buffer) = bufferState.Info();
            bufferStart = position % bufferLength;
            temp = Find(buffer, position);
            int actual = temp.ReadByte(b, start, length, bufferStart);
            bufferState.Info(actual == length, start + actual, length - actual, position + actual, temp);
            return actual;
        }
        public int Read(IntPtr ptr, IBufferState bufferState) {
            int position, start, length, bufferStart;
            IBuffer buffer;
            Buffer temp;
            (_, start, length, position, buffer) = bufferState.Info();
            bufferStart = position % bufferLength;
            temp = Find(buffer, position);
            int actual = temp.ReadPtr(ptr, start, length, bufferStart);
            _ = bufferState.Info(actual == length, start + actual, length - actual, position + actual, temp);
            return actual;
        }
        public void Read(FileStream fileStream) {
            Buffer temp = this;
            while (fileStream.Position<fileStream.Length) {
                int length=fileStream.Read(temp.buff,0,bufferLength);
                temp.wroteLength = length;
                if (length< bufferLength) {
                    temp.next = temp;
#if DEBUG
                    if (fileStream.Position < fileStream.Length) {
                        Console.WriteLine("文件未读完却意外退出！");
                    }
#endif
                    return;
                }
                temp = temp.next ?? temp.CreateNext();
            }
        }
        public void SeekOfWrite(IBufferState bufferState) {
            int position;
            IBuffer buffer;
            Buffer temp;
            (_, _, _, position, buffer) = bufferState.Info();
            temp = Find(buffer, position);
            (temp, position) = temp.MoveWriteAbsolutePosition();
            _ = bufferState.Info(true, null, null, position, temp);
        }
        private (Buffer buffer, int absolutePosition) MoveWriteAbsolutePosition() {
            lock (wrote) {
                if (next == this || next == null || wroteLength < bufferLength) {
                    return (this, index * bufferLength + wroteLength);
                } else {
                    return next.MoveWriteAbsolutePosition();
                }
            }
        }
        private Buffer CreateNext() {
            lock (create) {
                if (next != null) {
                    return next;
                }
                next = new Buffer(this);
                return next;
            }
        }
        public void Write(byte[] b, IBufferState bufferState) {
            int position, start, length, bufferStart;
            IBuffer buffer;
            Buffer temp;
            (_, start, length, position, buffer) = bufferState.Info();
            bufferStart = position % bufferLength;
            temp = Find(buffer, position);
            int actual = temp.WriteByte(b, start, length, bufferStart);
            bufferState.Info(actual == length, start + actual, length - actual, position + actual, temp);
        }
        private int WriteByte(byte[] b, int start, int length, int target) {
            lock (wrote) {
                if (buff == null) {
                    buff = new byte[bufferLength];
                    wroteLength = 0;
                }
                if (target > wroteLength) {
                    return 0;
                }
                int i = 0, freeLength = bufferLength - target;
                for (; i < length && i < freeLength; i++) {
                    buff[target + i] = b[start + i];
                }
                int wroteEnd = target + i;
                if (wroteEnd > wroteLength) {
                    wroteLength = wroteEnd;
                }
                return i;
            }
        }
        private int ReadByte(byte[] b, int start, int length, int target) {
            lock (wrote) {
                int i = 0, freeLength = wroteLength - target;
                for (; i < length && i < freeLength; i++) {
                    b[start + i] = buff[target + i];
                }
                return i;
            }
        }
        private int ReadPtr(IntPtr ptr, int start, int length, int target) {
            lock (wrote) {
                int i = 0, freeLength = wroteLength - target;
                for (; i < length && i < freeLength; i++) {
                    Marshal.WriteByte(ptr, start + i, buff[target + i]);
                }
                return i;
            }
        }
        public bool IsEndOfBuffer(IBufferState bufferState) {
            int position;
            IBuffer buffer;
            Buffer temp;
            (_, _, _, position, buffer) = bufferState.Info();
            temp = Find(buffer, position);
            return temp.next == temp && temp.GetWriteAbsolutePosition() <= position;
        }
        public void SetEndOfBuffer(IBufferState bufferState) {
            int position;
            IBuffer buffer;
            Buffer temp;
            (_, _, _, position, buffer) = bufferState.Info();
            temp = Find(buffer, position);
            temp.next = temp;
        }
        public bool IsInterrupt() {
            return next == this;
        }
        public void SetInterrupt() {
            next = this;
        }
        public void Clear() {
            Buffer temp = this;
            while (temp != null) {
                temp.Delete();
                temp = temp.next;
            }
        }
    }
}
