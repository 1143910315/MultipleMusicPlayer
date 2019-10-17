using System;
using System.Collections.Generic;
using System.Text;

namespace MultipleMusicPlayer.Buffer
{
    interface IBufferState
    {
        bool Success { get; set; }
        int Start { get; set; }
        int Length { get; set; }
        int AbsolutePosition { get; set; }
        IBuffer Buffer { get; set; }
        (bool Success, int Start, int Length, int AbsolutePosition, IBuffer Buffer) Info(bool? success=null,int? start = null, int? length = null, int? absolutePosition = null, IBuffer buffer = null);
    }
}
