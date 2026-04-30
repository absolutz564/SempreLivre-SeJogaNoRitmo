using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FfmpegUnity
{
    public class FfmpegGetTexturePerFrameIntPtrCommand : FfmpegGetTexturePerFrameCommand
    {
        Dictionary<int, IntPtr> resultVideoPtrs_ = new Dictionary<int, IntPtr>();
        Dictionary<int, int> widths_ = new Dictionary<int, int>();
        Dictionary<int, int> heights_ = new Dictionary<int, int>();

        protected override bool IntPtrMode
        {
            get
            {
                return true;
            }
        }

        public int GetVideoOutputWidth(int id = 0)
        {
            if (widths_.ContainsKey(id))
            {
                return widths_[id];
            }
            GetVideoOutputBytes(id);
            if (widths_.ContainsKey(id))
            {
                return widths_[id];
            }
            return -1;
        }

        public int GetVideoOutputHeight(int id = 0)
        {
            if (heights_.ContainsKey(id))
            {
                return heights_[id];
            }
            GetVideoOutputBytes(id);
            if (heights_.ContainsKey(id))
            {
                return heights_[id];
            }
            return -1;
        }

        public IntPtr GetVideoOutputIntPtr(int id = 0)
        {
            byte[] byteArray;
            int width, height;
            try
            {
                byteArray = GetResultVideoBuffer(id, out width, out height);
            }
            catch (KeyNotFoundException)
            {
                return IntPtr.Zero;
            }
            int size = width * height * 4 * Marshal.SizeOf(typeof(byte));

            if (!resultVideoPtrs_.ContainsKey(id) || widths_[id] != width || heights_[id] != height)
            {
                if (resultVideoPtrs_.ContainsKey(id))
                {
                    Marshal.FreeHGlobal(resultVideoPtrs_[id]);
                    resultVideoPtrs_.Remove(id);
                }

                IntPtr ptr = Marshal.AllocHGlobal(size);
                resultVideoPtrs_[id] = ptr;

                widths_[id] = width;
                heights_[id] = height;
            }

            if (byteArray != null)
            {
                Marshal.Copy(byteArray, 0, resultVideoPtrs_[id], size);
            }

            return resultVideoPtrs_[id];
        }

        public byte[] GetVideoOutputBytes(int id = 0)
        {
            int width, height;
            byte[] ret;
            try
            {
                ret = GetResultVideoBuffer(id, out width, out height);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
            widths_[id] = width;
            heights_[id] = height;
            return ret;
        }

        protected override void OnDestroy()
        {
            foreach (var resultVideoPtr in resultVideoPtrs_.Values)
            {
                Marshal.FreeHGlobal(resultVideoPtr);
            }
            resultVideoPtrs_.Clear();

            base.OnDestroy();
        }
    }
}
