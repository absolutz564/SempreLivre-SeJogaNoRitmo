using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FfmpegUnity
{
    public class FfplayBytesCommand : FfplayCommand, FfmpegBytesInputs.IInputControl
    {
        FfmpegBytesInputs bytesInputs_;
        List<byte[]>[] tempBytes_;
        string fileName_;

        public bool InputBytesIsEmpty
        {
            get
            {
                if (bytesInputs_ == null)
                {
                    return true;
                }
                return bytesInputs_.IsEmpty;
            }
        }

        public void AddInputBytes(byte[] bytes, int inputNo = 0)
        {
            if (inputNo != 0)
            {
                throw new System.ArgumentException("inputNo must be 0.");
            }

            StartCoroutine(addInputBytes(bytes, inputNo));
        }

        IEnumerator addInputBytes(byte[] bytes, int inputNo = 0)
        {
            while (bytesInputs_ == null && tempBytes_ == null)
            {
                yield return null;
            }

            if (bytesInputs_ == null)
            {
                tempBytes_[inputNo].Add(bytes);
            }
            else
            {
                bytesInputs_.AddInputBytes(bytes, inputNo);
            }
        }

        protected override string GetFileName()
        {
            if (bytesInputs_ == null)
            {
                bytesInputs_ = FfmpegBytesInputs.GetNewInstanceForFfplay();
                fileName_ = bytesInputs_.StartForFfplay();
            }

            return fileName_;
        }

        public override void Dispose()
        {
            if (bytesInputs_ != null)
            {
                bytesInputs_.Dispose();
            }
            bytesInputs_ = null;

            base.Dispose();
        }
    }
}
