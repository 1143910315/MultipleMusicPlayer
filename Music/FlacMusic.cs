using MultipleMusicPlayer.Buffer;
using MultipleMusicPlayer.Decoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using static MultipleMusicPlayer.Decoder.FlacDecoder;
using static MultipleMusicPlayer.Music.MusicList;

namespace MultipleMusicPlayer.Music {
    class FlacMusic : IMusic {
        class Callback : ICallback {
            public readonly ICallback.length length;
            public readonly ICallback.progress progress;
            public readonly ICallback.sample sample;
            public readonly ICallback.seek seek;

            public Callback(ICallback.length length, ICallback.progress progress, ICallback.sample sample, ICallback.seek seek) {
                this.length = length;
                this.progress = progress;
                this.sample = sample;
                this.seek = seek;
            }

            public ICallback.length GetLength() {
                return length;
            }
            public ICallback.progress GetProgress() {
                return progress;
            }
            public ICallback.sample GetSample() {
                return sample;
            }
            public ICallback.seek GetSeek() {
                return seek;
            }
        }
        private readonly FileInfo file;
        private readonly MusicControl control;
        private Thread readFileThread;
        private FileStream fileStream;
        private FlacDecoder flacDecoder;
        private IBuffer buffer;
        private ICallback callback;
        private int fileLength;
        public string Name { get => file.Name; set => throw new NotImplementedException(); }
        public string FullPath { get => file.FullName; set => throw new NotImplementedException(); }
        public FlacMusic(FileInfo fileInfo, MusicControl control) {
            file = fileInfo;
            this.control = control;
        }
        public void Play() {
            control.SetPlay(this);
            buffer = control.GetBuffer();
            buffer.SetInterrupt(false);
            fileStream = file.OpenRead();
            callback = new Callback(Length, Progress, Sample, Seek);
            buffer.UserData = callback;
            flacDecoder = new FlacDecoder();
            _ = flacDecoder.Initialize();
            _ = flacDecoder.Set_md5_checking(true);
            _ = flacDecoder.Decoder_Stream(buffer);
            readFileThread = new Thread(ReadFile);
            readFileThread.Start();
        }
        public void Stop() {
            flacDecoder.Abort();
            while (readFileThread != null && readFileThread.IsAlive) {
                Thread.Sleep(1);
            }
            readFileThread = null;
        }
        private void ReadFile() {
            fileLength = (int)fileStream.Length;
            buffer.Write(fileStream);
            fileStream.Close();
            flacDecoder.StartUntilEndOfStream();
            flacDecoder.Terminate();
        }
        public void Seek(int absolutePosition) { }
        public void Sample(int total, int rate) { }
        public void Progress(int sampleNumber) { }
        public int Length() {
            return fileLength;
        }
    }
}
