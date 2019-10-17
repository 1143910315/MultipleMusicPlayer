using MultipleMusicPlayer.Buffer;
using MultipleMusicPlayer.Sound;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MultipleMusicPlayer.Decoder {
    class FlacDecoder {
        private IntPtr decoder = IntPtr.Zero;
        private FLAC__StreamDecoderReadCallback readCallbackFunction;
        private FLAC__StreamDecoderSeekCallback seekCallbackFunction;
        private FLAC__StreamDecoderTellCallback tellCallbackFunction;
        private FLAC__StreamDecoderLengthCallback lengthCallbackFunction;
        private FLAC__StreamDecoderEofCallback eofCallbackFunction;
        private FLAC__StreamDecoderWriteCallback writeCallbackFunction;
        private FLAC__StreamDecoderMetadataCallback metadataCallbackFunction;
        private FLAC__StreamDecoderErrorCallback errorCallbackFunction;
        private readonly object work = new object();
        private IBuffer readBuffer;
        private IBuffer writeBuffer = null;
        private readonly IBufferState readBufferState = new BufferState();
        private readonly IBufferState writeBufferState = new BufferState();
        private readonly PortaudioSound portaudioSound = new PortaudioSound();
        private bool seek = false;
        private int seekSample;
        private uint bps;
        private byte[] tempByte = new byte[1];
        private bool playing = false;
        public interface ICallback {
            public delegate void seek(int absolutePosition);
            public delegate void sample(int total, int rate);
            public delegate void progress(int sampleNumber);
            public delegate int length();
            public sample GetSample();
            public progress GetProgress();
            public seek GetSeek();
            public length GetLength();
        }
        public bool Initialize() {
            decoder = FLAC__stream_decoder_new();
            if (decoder.Equals(IntPtr.Zero)) {
                return false;
            }
            return true;
        }
        public bool Set_md5_checking(bool value) {
            int val = value ? 1 : 0;
            if (FLAC__stream_decoder_set_md5_checking(decoder, val) == 0) {
                return false;
            }
            return true;
        }
        public void StartUntilEndOfStream() {
            FLAC__stream_decoder_process_until_end_of_stream(decoder);
        }
        public void Seek(int sample) {
            seekSample = sample;
            seek = true;
        }
        public void Terminate() {
            if (!decoder.Equals(IntPtr.Zero)) {
                FLAC__stream_decoder_delete(decoder);
                decoder = IntPtr.Zero;
            }
        }
        ~FlacDecoder() {
            if (!decoder.Equals(IntPtr.Zero)) {
                FLAC__stream_decoder_delete(decoder);
                decoder = IntPtr.Zero;
            }
        }
        public FLAC__StreamDecoderInitStatus Decoder_Stream(IBuffer buffer) {
            readBuffer = buffer;
            readBufferState.AbsolutePosition = 0;
            readCallbackFunction = ReadCallbackFunction;
            seekCallbackFunction = SeekCallbackFunction;
            tellCallbackFunction = TellCallbackFunction;
            lengthCallbackFunction = LengthCallbackFunction;
            eofCallbackFunction = EofCallbackFunction;
            writeCallbackFunction = WriteCallbackFunction;
            metadataCallbackFunction = MetadataCallbackFunction;
            errorCallbackFunction = ErrorCallbackFunction;
            portaudioSound.Initialize();
            return FLAC__stream_decoder_init_stream(decoder, readCallbackFunction, seekCallbackFunction, tellCallbackFunction, lengthCallbackFunction, eofCallbackFunction, writeCallbackFunction, metadataCallbackFunction, errorCallbackFunction, IntPtr.Zero);
        }
        public FLAC__StreamDecoderInitStatus Decoder_File(string fileName) {
            writeCallbackFunction = WriteCallbackFunction;
            metadataCallbackFunction = MetadataCallbackFunction;
            errorCallbackFunction = ErrorCallbackFunction;
            portaudioSound.Initialize();
            return FLAC__stream_decoder_init_file(decoder, fileName, writeCallbackFunction, metadataCallbackFunction, errorCallbackFunction, IntPtr.Zero);
        }
        private FLAC__StreamDecoderReadStatus ReadCallbackFunction(IntPtr decoder, IntPtr buffer, ref uint bytes, IntPtr client_data) {
            lock (work) {
                if (readBuffer.IsEndOfBuffer(readBufferState)) {
                    return FLAC__StreamDecoderReadStatus.FLAC__STREAM_DECODER_READ_STATUS_END_OF_STREAM;
                }
                bool complete = false;
                int readLength = 0;
                readBufferState.Info(start: 0, length: (int)bytes);
                do {
                    if (readBuffer.IsInterrupt()) {
                        return FLAC__StreamDecoderReadStatus.FLAC__STREAM_DECODER_READ_STATUS_ABORT;
                    }
                    readLength += readBuffer.Read(buffer, readBufferState);
                    complete = readBufferState.Success;
                    if (readBuffer.IsEndOfBuffer(readBufferState)) {
                        bytes = (uint)readLength;
                        return FLAC__StreamDecoderReadStatus.FLAC__STREAM_DECODER_READ_STATUS_CONTINUE;
                    }
                    if (!complete) {
                        Thread.Sleep(1);
                    }
                } while (!complete);
                return FLAC__StreamDecoderReadStatus.FLAC__STREAM_DECODER_READ_STATUS_CONTINUE;
            }
        }
        private FLAC__StreamDecoderSeekStatus SeekCallbackFunction(IntPtr decoder, ulong absolute_byte_offset, IntPtr client_data) {
            lock (work) {
                if (readBuffer.UserData is ICallback callback) {
                    ICallback.seek call = callback.GetSeek();
                    if (call != null) {
                        call((int)absolute_byte_offset);
                        readBufferState.AbsolutePosition = (int)absolute_byte_offset;
                        return FLAC__StreamDecoderSeekStatus.FLAC__STREAM_DECODER_SEEK_STATUS_OK;
                    }
                }
                return FLAC__StreamDecoderSeekStatus.FLAC__STREAM_DECODER_SEEK_STATUS_UNSUPPORTED;
            }
        }

        private FLAC__StreamDecoderTellStatus TellCallbackFunction(IntPtr decoder, ref ulong absolute_byte_offset, IntPtr client_data) {
            absolute_byte_offset = (ulong)readBufferState.AbsolutePosition;
            return FLAC__StreamDecoderTellStatus.FLAC__STREAM_DECODER_TELL_STATUS_OK;
        }
        private FLAC__StreamDecoderLengthStatus LengthCallbackFunction(IntPtr decoder, ref ulong stream_length, IntPtr client_data) {
            lock (work) {
                if (readBuffer.UserData is ICallback callback) {
                    ICallback.length call = callback.GetLength();
                    if (call != null) {
                        stream_length = (ulong)call();
                        return FLAC__StreamDecoderLengthStatus.FLAC__STREAM_DECODER_LENGTH_STATUS_OK;
                    }
                }
                return FLAC__StreamDecoderLengthStatus.FLAC__STREAM_DECODER_LENGTH_STATUS_UNSUPPORTED;
            }
        }
        private int EofCallbackFunction(IntPtr decoder, IntPtr client_data) {
            lock (work) {
                if (readBuffer.IsEndOfBuffer(readBufferState)) {
                    return 1;
                } else {
                    return 0;
                }
            }
        }
        private FLAC__StreamDecoderWriteStatus WriteCallbackFunction(IntPtr decoder, IntPtr frame, IntPtr buffer, IntPtr client_data) {
            if (readBuffer.UserData is ICallback callback) {
                callback.GetProgress()?.Invoke((int)FLAC__frame_sample_number(frame));
            }
            int channels = (int)FLAC__frame_channels(frame);
            if (writeBuffer == null) {
                portaudioSound.OpenDefaultOutputStream(channels, FLAC__frame_sample_rate(frame), bps, out writeBuffer);
            }
            uint blocksize = FLAC__frame_blocksize(frame);
            long maxSize = blocksize * bps / 8 * channels;
            if (maxSize > tempByte.Length) {
                tempByte = new byte[maxSize];
            }
            int writePosition = 0;
            IntPtr[] data = new IntPtr[channels];
            for (int i = 0; i < channels; i++) {
                data[i] = new IntPtr(Marshal.ReadInt32(buffer, i * 4));
            }
            for (int i = 0; i < blocksize; i++) {
                for (int j = 0; j < channels; j++) {
                    for (int k = 0; k < bps / 8; k++) {
                        tempByte[writePosition++] = Marshal.ReadByte(data[j], i * 4 + k);
                    }
                }
            }
            _ = writeBufferState.Info(start: 0, length: writePosition);
            while (writeBuffer.IsFull(writeBufferState)) {
                Thread.Sleep(1);
            }
            writeBuffer.Write(tempByte, writeBufferState);
            while (!writeBufferState.Success) {
                if (writeBuffer.IsEndOfBuffer(writeBufferState)) {
                    if (!playing) {
                        _ = portaudioSound.StartStream();
                    }
                    writeBufferState.AbsolutePosition = 0;
                }
                while (writeBuffer.IsFull(writeBufferState)) {
                    Thread.Sleep(1);
                }
                writeBuffer.Write(tempByte, writeBufferState);
            }
            return FLAC__StreamDecoderWriteStatus.FLAC__STREAM_DECODER_WRITE_STATUS_CONTINUE;
        }
        private void MetadataCallbackFunction(IntPtr decoder, IntPtr metadata, IntPtr client_data) {
            lock (work) {
                if (readBuffer.UserData is ICallback callback) {
                    callback.GetSample()?.Invoke((int)FLAC__metadata_total_samples(metadata), (int)FLAC__metadata_sample_rate(metadata));
                }
                bps = FLAC__metadata_bits_per_sample(metadata);
                if (seek) {
                    FLAC__stream_decoder_seek_absolute(decoder, (ulong)seekSample);
                    seek = false;
                }
            }
        }
        private void ErrorCallbackFunction(IntPtr decoder, FLAC__StreamDecoderErrorStatus status, IntPtr client_data) {
        }
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr FLAC__stream_decoder_new();
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FLAC__stream_decoder_set_md5_checking(IntPtr decoder, int value);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FLAC__StreamDecoderInitStatus FLAC__stream_decoder_init_file(IntPtr decoder, string filename, FLAC__StreamDecoderWriteCallback writeCallback, FLAC__StreamDecoderMetadataCallback metadataCallback, FLAC__StreamDecoderErrorCallback errorCallback, IntPtr client_data);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FLAC__StreamDecoderInitStatus FLAC__stream_decoder_init_stream(IntPtr decoder, FLAC__StreamDecoderReadCallback read_callback, FLAC__StreamDecoderSeekCallback seek_callback, FLAC__StreamDecoderTellCallback tell_callback, FLAC__StreamDecoderLengthCallback length_callback, FLAC__StreamDecoderEofCallback eof_callback, FLAC__StreamDecoderWriteCallback write_callback, FLAC__StreamDecoderMetadataCallback metadata_callback, FLAC__StreamDecoderErrorCallback error_callback, IntPtr client_data);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FLAC__stream_decoder_process_until_end_of_stream(IntPtr decoder);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FLAC__stream_decoder_seek_absolute(IntPtr decoder, ulong sample);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void FLAC__stream_decoder_delete(IntPtr decoder);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint FLAC__frame_blocksize(IntPtr frame);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong FLAC__frame_sample_number(IntPtr frame);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint FLAC__frame_channels(IntPtr frame);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint FLAC__frame_sample_rate(IntPtr frame);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FLAC__MetadataType FLAC__metadata_bits_type(IntPtr metadata);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint FLAC__metadata_sample_rate(IntPtr metadata);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint FLAC__metadata_bits_per_sample(IntPtr metadata);
        [DllImport("libFLAC_dynamic.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong FLAC__metadata_total_samples(IntPtr metadata);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate FLAC__StreamDecoderReadStatus FLAC__StreamDecoderReadCallback(IntPtr decoder, IntPtr buffer, ref uint bytes, IntPtr client_data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate FLAC__StreamDecoderSeekStatus FLAC__StreamDecoderSeekCallback(IntPtr decoder, ulong absolute_byte_offset, IntPtr client_data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate FLAC__StreamDecoderTellStatus FLAC__StreamDecoderTellCallback(IntPtr decoder, ref ulong absolute_byte_offset, IntPtr client_data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate FLAC__StreamDecoderLengthStatus FLAC__StreamDecoderLengthCallback(IntPtr decoder, ref ulong stream_length, IntPtr client_data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int FLAC__StreamDecoderEofCallback(IntPtr decoder, IntPtr client_data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate FLAC__StreamDecoderWriteStatus FLAC__StreamDecoderWriteCallback(IntPtr decoder, IntPtr frame, IntPtr buffer, IntPtr client_data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FLAC__StreamDecoderMetadataCallback(IntPtr decoder, IntPtr metadata, IntPtr client_data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FLAC__StreamDecoderErrorCallback(IntPtr decoder, FLAC__StreamDecoderErrorStatus status, IntPtr client_data);
        public enum FLAC__StreamDecoderReadStatus {
            FLAC__STREAM_DECODER_READ_STATUS_CONTINUE,
            FLAC__STREAM_DECODER_READ_STATUS_END_OF_STREAM,
            FLAC__STREAM_DECODER_READ_STATUS_ABORT
        }
        public enum FLAC__StreamDecoderSeekStatus {
            FLAC__STREAM_DECODER_SEEK_STATUS_OK,
            FLAC__STREAM_DECODER_SEEK_STATUS_ERROR,
            FLAC__STREAM_DECODER_SEEK_STATUS_UNSUPPORTED
        }
        public enum FLAC__StreamDecoderTellStatus {
            FLAC__STREAM_DECODER_TELL_STATUS_OK,
            FLAC__STREAM_DECODER_TELL_STATUS_ERROR,
            FLAC__STREAM_DECODER_TELL_STATUS_UNSUPPORTED
        }
        public enum FLAC__StreamDecoderLengthStatus {
            FLAC__STREAM_DECODER_LENGTH_STATUS_OK,
            FLAC__STREAM_DECODER_LENGTH_STATUS_ERROR,
            FLAC__STREAM_DECODER_LENGTH_STATUS_UNSUPPORTED
        }
        public enum FLAC__StreamDecoderWriteStatus {
            FLAC__STREAM_DECODER_WRITE_STATUS_CONTINUE,
            FLAC__STREAM_DECODER_WRITE_STATUS_ABORT
        }
        public enum FLAC__StreamDecoderErrorStatus {
            FLAC__STREAM_DECODER_ERROR_STATUS_LOST_SYNC,
            FLAC__STREAM_DECODER_ERROR_STATUS_BAD_HEADER,
            FLAC__STREAM_DECODER_ERROR_STATUS_FRAME_CRC_MISMATCH,
            FLAC__STREAM_DECODER_ERROR_STATUS_UNPARSEABLE_STREAM
        }
        public enum FLAC__StreamDecoderInitStatus {
            FLAC__STREAM_DECODER_INIT_STATUS_OK = 0,
            FLAC__STREAM_DECODER_INIT_STATUS_UNSUPPORTED_CONTAINER,
            FLAC__STREAM_DECODER_INIT_STATUS_INVALID_CALLBACKS,
            FLAC__STREAM_DECODER_INIT_STATUS_MEMORY_ALLOCATION_ERROR,
            FLAC__STREAM_DECODER_INIT_STATUS_ERROR_OPENING_FILE,
            FLAC__STREAM_DECODER_INIT_STATUS_ALREADY_INITIALIZED
        }
        public enum FLAC__MetadataType {
            FLAC__METADATA_TYPE_STREAMINFO = 0,
            FLAC__METADATA_TYPE_PADDING = 1,
            FLAC__METADATA_TYPE_APPLICATION = 2,
            FLAC__METADATA_TYPE_SEEKTABLE = 3,
            FLAC__METADATA_TYPE_VORBIS_COMMENT = 4,
            FLAC__METADATA_TYPE_CUESHEET = 5,
            FLAC__METADATA_TYPE_PICTURE = 6,
            FLAC__METADATA_TYPE_UNDEFINED = 7,
            FLAC__MAX_METADATA_TYPE = 126,

        }
    }
}
