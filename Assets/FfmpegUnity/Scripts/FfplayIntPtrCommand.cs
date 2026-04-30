using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FfmpegUnity
{
    public class FfplayIntPtrCommand : FfplayCommand
    {
        protected override bool IntPtrMode
        {
            get
            {
                return true;
            }
        }

        public IntPtr VideoOutput
        {
            get
            {
                return VideoOutputPtr;
            }
        }
    }
}
