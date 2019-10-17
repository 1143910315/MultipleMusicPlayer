using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static MultipleMusicPlayer.Music.MusicList;

namespace MultipleMusicPlayer.Music {
    class MusicFactory {
        public static IMusic GetMusic(FileInfo fileInfo, MusicControl control) {
            return fileInfo.Extension.ToLower() switch
            {
                ".flac" => new FlacMusic(fileInfo, control),
                _ => null,
            };
        }
    }
}
