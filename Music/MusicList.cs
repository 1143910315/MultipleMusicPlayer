using MultipleMusicPlayer.Buffer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MultipleMusicPlayer.Music {
    class MusicList {
        public class MusicControl {
            private IMusic _music = null;
            private readonly IBuffer buffer = new Buffer.Buffer(10240);
            public void SetPlay(IMusic music) {
                _music?.Stop();
                _music = music;
            }
            public IBuffer GetBuffer() {
                buffer.Clear();
                return buffer;
            }
        }
        public List<IMusic> FileList { get; set; } = new List<IMusic>();
        private readonly MusicControl control = new MusicControl();
        public void AddDirectory(string directory) {
            FileList = new List<IMusic>();
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            FileInfo[] Files = directoryInfo.GetFiles();
            foreach (var file in Files) {
                IMusic temp = MusicFactory.GetMusic(file, control);
                if (temp != null) {
                    FileList.Add(temp);
                }
            }
        }
        public void AddFile(string file) {
            IMusic temp = MusicFactory.GetMusic(new FileInfo(file), control);
            if (temp != null) {
                FileList.Add(temp);
            }
        }
    }
}
