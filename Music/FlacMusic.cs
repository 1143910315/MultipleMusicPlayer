using MultipleMusicPlayer.Buffer;
using MultipleMusicPlayer.Decoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using static MultipleMusicPlayer.Music.MusicList;

namespace MultipleMusicPlayer.Music {
    class FlacMusic : IMusic {
        private readonly FileInfo file;
        private readonly MusicControl control;
        private Thread readFileThread;
        private FileStream fileStream;
        private FlacDecoder flacDecoder;
        private IBuffer buffer;
        public string Name { get => file.Name; set => throw new NotImplementedException(); }
        public string FullPath { get => file.FullName; set => throw new NotImplementedException(); }
        public FlacMusic(FileInfo fileInfo, MusicControl control) {
            file = fileInfo;
            this.control = control;
        }
        public void Play() {
            control.SetPlay(this);
            buffer = control.GetBuffer();
            fileStream = file.OpenRead();
            flacDecoder = new FlacDecoder();
            _ = flacDecoder.Initialize();
            _ = flacDecoder.Set_md5_checking(true);
            _ = flacDecoder.Decoder_Stream(buffer);
            readFileThread = new Thread(ReadFile);
            readFileThread.Start();
        }
        public void Stop() {
            throw new NotImplementedException();
        }
        private void ReadFile() {
            buffer.Read(fileStream);
            fileStream.Close();
            flacDecoder.StartUntilEndOfStream();
            flacDecoder.Terminate();
        }
    }
}
