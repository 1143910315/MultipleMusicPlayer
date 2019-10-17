using System;
using System.Collections.Generic;
using System.Text;

namespace MultipleMusicPlayer.Music {
    interface IMusic {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public void Play();
        public void Stop();
    }
}
