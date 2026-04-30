using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FfmpegUnity
{
    [RequireComponent(typeof(AudioSource))]
    public class FfplayAudio : MonoBehaviour
    {
        public FfplayCommand Ffplay
        {
            set;
            get;
        }

        float volume_ = 1f;

        void OnAudioFilterRead(float[] data, int channels)
        {
            Ffplay.OnAudioFilterReadFromFfplayAudio(data, channels);

            if (volume_ < 1f)
            {
                for (int loop = 0; loop < data.Length; loop++)
                {
                    data[loop] *= volume_;
                }
            }
        }

        void Update()
        {
            var source = GetComponent<AudioSource>();

            if (source.mute != Ffplay.Muted)
            {
                Ffplay.ToggleMuted();
            }

            volume_ = source.volume;
        }
    }
}
