using System;
using System.Collections.Generic;
using System.Text;

namespace MultipleMusicPlayer.Buffer {
    class BufferState : IBufferState {
        private object lockObject = new object();
        private bool _success;
        private int _start;
        private int _length;
        private int _absolutePosition;
        private IBuffer _buffer;
        public bool Success {
            get {
                lock (lockObject) {
                    return _success;
                }
            }
            set {
                lock (lockObject) {
                    _success = value;
                }
            }
        }
        public int Start {
            get {
                lock (lockObject) {
                    return _start;
                }
            }
            set {
                lock (lockObject) {
                    _start = value;
                }
            }
        }
        public int Length {
            get {
                lock (lockObject) {
                    return _length;
                }
            }
            set {
                lock (lockObject) {
                    _length = value;
                }
            }
        }
        public int AbsolutePosition {
            get {
                lock (lockObject) {
                    return _absolutePosition;
                }
            }
            set {
                lock (lockObject) {
                    _absolutePosition = value;
                }
            }
        }
        public IBuffer Buffer {
            get {
                lock (lockObject) {
                    return _buffer;
                }
            }
            set {
                lock (lockObject) {
                    _buffer = value;
                }
            }
        }
        public (bool Success, int Start, int Length, int AbsolutePosition, IBuffer Buffer) Info(bool? success = null, int? start = null, int? length = null, int? absolutePosition = null, IBuffer buffer = null) {
            lock (lockObject) {
                if (success != null && success.HasValue) {
                    _success = success.Value;
                }
                if (start != null && start.HasValue) {
                    _start = start.Value;
                }
                if (length != null && length.HasValue) {
                    _length = length.Value;
                }
                if (absolutePosition != null && absolutePosition.HasValue) {
                    _absolutePosition = absolutePosition.Value;
                }
                if (buffer != null) {
                    _buffer = buffer;
                }
                return (_success, _start, _length, _absolutePosition, _buffer);
            }
        }
    }
}
