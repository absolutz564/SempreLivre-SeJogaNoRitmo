using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FfmpegUnity
{
    public class FfmpegBytesCaptureCommand : FfmpegCaptureCommand, FfmpegBytesOutputs.IOutputControl
    {
        public string[] OutputOptions = new string[1];

        FfmpegBytesOutputs bytesOutputs_ = null;

        protected override void ByteStart()
        {
            if (bytesOutputs_ != null)
            {
                bytesOutputs_.Dispose();
                bytesOutputs_ = null;
            }

            if (OutputOptions != null && OutputOptions.Length > 0)
            {
                bytesOutputs_ = FfmpegBytesOutputs.GetNewInstance(OutputOptions, this);
                RunOptions += bytesOutputs_.BuildAndStart();
            }
        }

        public byte[] GetOutputBytes(int outputNo = 0)
        {
            if (bytesOutputs_ == null)
            {
                return null;
            }

            return bytesOutputs_.GetOutputBytes(outputNo);
        }

        public int OutputOptionsCount
        {
            get
            {
                return OutputOptions.Length;
            }
        }

        protected override void OnDestroy()
        {
            if (bytesOutputs_ != null)
            {
                bytesOutputs_.Dispose();
                bytesOutputs_ = null;
            }

            base.OnDestroy();
        }
    }
}
