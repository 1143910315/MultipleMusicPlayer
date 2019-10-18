using MultipleMusicPlayer.Buffer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MultipleMusicPlayer.Sound {
    internal class PortaudioSound {
        private bool isInitialize = false;
        private IntPtr stream;
        private IBuffer outBuffer;
        private readonly IBufferState outBufferState = new BufferState();
        private PaStreamCallback streamCallback;
        private int channel;
        private static readonly long[] rest = { 0, 0, 0, 0, 0, 0, 0, 0 };
        private int bps;
        private int state;
        public Error Initialize() {
            isInitialize = true;
            return Pa_Initialize();
        }
        public Error OpenDefaultOutputStream(int numOutputChannels, double sampleRate, uint sampleSize, out IBuffer buffer) {
            bps = (int)sampleSize;
            PaSampleFormat sampleFormat = bps switch
            {
                8 => PaSampleFormat.paInt8,
                16 => PaSampleFormat.paInt16,
                24 => PaSampleFormat.paInt24,
                32 => PaSampleFormat.paInt32,
                _ => PaSampleFormat.paCustomFormat
            };
            if (sampleFormat == PaSampleFormat.paCustomFormat) {
                buffer = null;
                return Error.paSampleFormatNotSupported;
            }
            buffer = outBuffer = new Buffer.Buffer(256 * numOutputChannels * bps / 8);
            outBuffer.SetEndOfBuffer(new BufferState { AbsolutePosition = 256 * numOutputChannels * 20 });
            streamCallback = StreamCallback;
            channel = numOutputChannels;
            outBufferState.AbsolutePosition = 0;
            return Pa_OpenDefaultStream(ref stream, 0, numOutputChannels, sampleFormat, sampleRate, 256, streamCallback, IntPtr.Zero);
        }
        public Error StartStream() {
            state = 0;
            return Pa_StartStream(stream);
        }
        public void Stop() {
            state |= 0b01;
            while (state != 0) {
                Thread.Sleep(1);
            }
            outBuffer = null;
        }

        public void Abort() {
            state |= 0b10;
            while (state != 0) {
                Thread.Sleep(1);
            }
            outBuffer = null;
        }

        private PaStreamCallbackResult StreamCallback(IntPtr input, IntPtr output, uint frameCount, ref PaStreamCallbackTimeInfo timeInfo, PaStreamCallbackFlags statusFlags, IntPtr userData) {
            if ((state & 0b10) == 0b10) {
                state = 0;
                return PaStreamCallbackResult.paAbort;
            }
            if (!outBuffer.IsFull(outBufferState)) {
                if ((state & 0b01) == 0b01) {
                    state = 0;
                    return PaStreamCallbackResult.paComplete;
                }
#if DEBUG
                Debug.WriteLine("解码速度过慢！");
#endif
                //outBufferState.AbsolutePosition -= 256 * channel * bps / 8;
                //if ((state & 0b01) == 0b01) {
                //    state = 0;
                //    return PaStreamCallbackResult.paComplete;
                //}
            }
            outBufferState.Info(start: 0, length: (int)frameCount * channel * bps / 8);
            outBuffer.Read(output, outBufferState);
            bool success;
            int start, length;
            (success, start, length, _, _) = outBufferState.Info();
            if (success) {
                outBuffer.DeleteOperate(outBufferState);
            } else {
#if DEBUG
                Console.WriteLine("发生了不可能错误！");
#endif
                for (int i = 0; i < length; i++) {
                    Marshal.WriteByte(output, start, 0);
                }
            }
            if (outBuffer.IsEndOfBuffer(outBufferState)) {
                outBufferState.AbsolutePosition = 0;
            }
            //int times = rest.Length * 8;
            //IntPtr ptr;
            //for (int i = 0; i < frameCount * channel / times; i++) {
            //    ptr = new IntPtr(output.ToInt64() + i * times);
            //    Marshal.Copy(rest, 0, ptr, rest.Length);
            //}
            //if ((state & 0b01) == 0b01) {
            //    state = 0;
            //    return PaStreamCallbackResult.paComplete;
            //}
            return PaStreamCallbackResult.paContinue;
        }
        public string GetErrorText(Error errorCode) {
            return Marshal.PtrToStringAnsi(Pa_GetErrorText(errorCode));
        }
        public Error Terminate() {
            isInitialize = false;
            return (Error)Pa_Terminate();
        }
        ~PortaudioSound() {
            if (isInitialize) {
                Pa_Terminate();
            }
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate PaStreamCallbackResult PaStreamCallback(IntPtr input, IntPtr output, uint frameCount, ref PaStreamCallbackTimeInfo timeInfo, PaStreamCallbackFlags statusFlags, IntPtr userData);
        [DllImport("portaudio_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern Error Pa_Initialize();
        [DllImport("portaudio_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern Error Pa_OpenDefaultStream(ref IntPtr stream, int numInputChannels, int numOutputChannels, PaSampleFormat sampleFormat, double sampleRate, uint framesPerBuffer, PaStreamCallback streamCallback, IntPtr userData);
        [DllImport("portaudio_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern Error Pa_GetSampleSize(PaSampleFormat format);
        [DllImport("portaudio_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern Error Pa_StartStream(IntPtr stream);
        [DllImport("portaudio_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Pa_GetErrorText(Error errorCode);
        [DllImport("portaudio_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Pa_Terminate();
        public enum Error {
            paNoError = 0,
            paNotInitialized = -10000,
            paUnanticipatedHostError = -9999,
            paInvalidChannelCount = -9998,
            paInvalidSampleRate = -9997,
            paInvalidDevice = -9996,
            paInvalidFlag = -9995,
            paSampleFormatNotSupported = -9994,
            paBadIODeviceCombination = -9993,
            paInsufficientMemory = -9992,
            paBufferTooBig = -9991,
            paBufferTooSmall = -9990,
            paNullCallback = -9989,
            paBadStreamPtr = -9988,
            paTimedOut = -9987,
            paInternalError = -9986,
            paDeviceUnavailable = -9985,
            paIncompatibleHostApiSpecificStreamInfo = -9984,
            paStreamIsStopped = -9983,
            paStreamIsNotStopped = -9982,
            paInputOverflowed = -9981,
            paOutputUnderflowed = -9980,
            paHostApiNotFound = -9979,
            paInvalidHostApi = -9978,
            paCanNotReadFromACallbackStream = -9977,
            paCanNotWriteToACallbackStream = -9976,
            paCanNotReadFromAnOutputOnlyStream = -9975,
            paCanNotWriteToAnInputOnlyStream = -9974,
            paIncompatibleStreamHostApi = -9973,
            paBadBufferPtr = -9972
        }
        private enum PaStreamCallbackFlags : uint {
            paInputUnderflow = 0x00000001,
            paInputOverflow = 0x00000002,
            paOutputUnderflow = 0x00000004,
            paOutputOverflow = 0x00000008,
            paPrimingOutput = 0x00000010
        }
        public enum PaSampleFormat : uint {
            paFloat32 = 0x00000001,
            paInt32 = 0x00000002,
            paInt24 = 0x00000004,
            paInt16 = 0x00000008,
            paInt8 = 0x00000010,
            paUInt8 = 0x00000020,
            paCustomFormat = 0x00010000,
            paNonInterleaved = 0x80000000
        }
        private enum PaStreamCallbackResult {
            paContinue = 0,
            paComplete = 1,
            paAbort = 2
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct PaStreamCallbackTimeInfo {
            double inputBufferAdcTime;
            double currentTime;
            double outputBufferDacTime;
        }
    }
}
