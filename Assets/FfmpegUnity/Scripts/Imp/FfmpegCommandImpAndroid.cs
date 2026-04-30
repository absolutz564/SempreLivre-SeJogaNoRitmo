using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace FfmpegUnity
{
    public class FfmpegCommandImpAndroid : FfmpegCommandImpBase
    {
        static bool isFirstCall_ = true;

        long ffmpegSessionId_ = -1;
        string[] outputAllLine_ = new string[0];
        int outputAllLinePos_ = 0;
        int stdErrPos_ = 0;

        [DllImport("ffmpegkit")]
        static extern void cancel_operation(long id);

        [DllImport("ffmpegkit")]
        static extern int set_force_stop_flag(long id);

        public FfmpegCommandImpAndroid(FfmpegCommand command) : base(command)
        {
        }

        public override IEnumerator StartFfmpegCoroutine(string runOptions)
        {
            while (ffmpegSessionId_ != -1)
            {
                yield return null;
            }

            using (AndroidJavaClass configClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig"))
            {
                if (isFirstCall_)
                {
                    isFirstCall_ = false;
                    using (AndroidJavaObject signal = new AndroidJavaClass("com.arthenica.ffmpegkit.Signal"))
                    using (AndroidJavaObject paramVal = signal.GetStatic<AndroidJavaObject>("SIGXCPU"))
                    {
                        configClass.CallStatic("ignoreSignal", paramVal);
                    }

                    //configClass.CallStatic("setAsyncConcurrencyLimit", 32);
                    //configClass.CallStatic("setSessionHistorySize", 999);
                }
            }

            stdErrPos_ = 0;

            string outputPath = Path.Combine(Application.persistentDataPath, "ExportedVideos");
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var options = runOptions.Replace("{PERSISTENT_DATA_PATH}", Application.persistentDataPath)
                .Replace("{TEMPORARY_CACHE_PATH}", Application.temporaryCachePath)
                .Replace("\r\n", "\n").Replace("\\\n", " ").Replace("^\n", " ").Replace("\n", " ");
            yield return CommandObj.StreamingAssetsCopyOptions(options);
            options = options.Replace("{STREAMING_ASSETS_PATH}", Application.temporaryCachePath + "/FfmpegUnity_temp");
            options = options.Replace("{EXPORTED_VIDEO_PATH}", outputPath);
            options = options.Replace("{VIDEO_NAME}", "capture.mp4");
            UnityEngine.Debug.Log("FFmpeg vai salvar no Android em: " + outputPath);

            using (AndroidJavaClass ffmpeg = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKit"))
            using (AndroidJavaObject ffmpegSession = ffmpeg.CallStatic<AndroidJavaObject>("executeAsync", options))
            {
                ffmpegSessionId_ = ffmpegSession.Call<long>("getSessionId");
                if (CommandObj is FfmpegPlayerCommand)
                {
                    set_force_stop_flag(ffmpegSessionId_);
                }
                //UnityEngine.Debug.Log("ffmpegSessionId_ = " + ffmpegSessionId_);
            }
        }

        void stopMain()
        {
            try
            {
                if (ffmpegSessionId_ != -1)
                {
                    cancel_operation(ffmpegSessionId_);
                    UnityEngine.Debug.Log("FFmpeg: cancel_operation called.");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Erro ao cancelar FFmpeg: " + e.Message);
            }
            finally
            {
                ffmpegSessionId_ = -1;
                isRunning_ = false;
            }
        }

        public override void StopFfmpeg()
        {
            stopMain();
        }

        public override void Dispose()
        {
            stopMain();
        }

        ~FfmpegCommandImpAndroid()
        {
            //stopMain();
        }

        bool isRunning_ = true;//false;
        public override bool IsRunning
        {
            get
            {
                return isRunning_;
            }
        }

        public override string StdErrLine(List<string> stdErrListForGetLine)
        {
            if (!isRunning_)
            {
                return null;
            }

            try
            {
                using (AndroidJavaClass configClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig"))
                using (AndroidJavaObject ffmpegSession = configClass.CallStatic<AndroidJavaObject>("getSession", ffmpegSessionId_))
                {
                    if (ffmpegSession == null)
                    {
                        return null;
                    }
                    if (outputAllLine_.Length <= outputAllLinePos_)
                    {
                        string outputAll = ffmpegSession.Call<string>("getOutput");
                        int nextPos = outputAll.Length;
                        outputAll = outputAll.Substring(stdErrPos_).Replace("\r\n", "\n").Replace("\r", "\n");
                        stdErrPos_ = nextPos;
                        if (string.IsNullOrEmpty(outputAll))
                        {
                            return null;
                        }
                        outputAllLine_ = outputAll.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                        outputAllLinePos_ = 0;
                    }
                    if (outputAllLine_.Length <= 0)
                    {
                        return null;
                    }
                    string ret = outputAllLine_[outputAllLinePos_];
                    outputAllLinePos_++;
                    stdErrListForGetLine.Add(ret);
                    return ret;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return null;
            }
        }

        public override int ReturnCode
        {
            get
            {
                using (AndroidJavaClass configClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig"))
                using (AndroidJavaObject ffmpegSession = configClass.CallStatic<AndroidJavaObject>("getSession", ffmpegSessionId_))
                {
                    if (ffmpegSession == null)
                    {
                        return 0;
                    }
                    using (var state = ffmpegSession.Call<AndroidJavaObject>("getReturnCode"))
                    {
                        if (state == null)
                        {
                            return 0;
                        }
                        return state.Call<int>("getValue");
                    }
                }
            }
        }

        public override bool IsGetStatusFromImp
        {
            get
            {
                return true;
            }
        }

        public override float Time
        {
            get
            {
                using (AndroidJavaClass configClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig"))
                using (AndroidJavaObject ffmpegSession = configClass.CallStatic<AndroidJavaObject>("getSession", ffmpegSessionId_))
                {
                    if (ffmpegSession == null)
                    {
                        return -1f;
                    }
                    using (var statistics = ffmpegSession.Call<AndroidJavaObject>("getLastReceivedStatistics"))
                    {
                        if (statistics == null)
                        {
                            return 0f;
                        }

                        float fps = statistics.Call<float>("getVideoFps");
                        if (fps > 0f)
                        {
                            return statistics.Call<int>("getVideoFrameNumber") / fps;
                        }

                        return statistics.Call<int>("getTime") / 1000f;
                    }
                }
            }
        }

        public override double Speed
        {
            get
            {
                using (AndroidJavaClass configClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig"))
                using (AndroidJavaObject ffmpegSession = configClass.CallStatic<AndroidJavaObject>("getSession", ffmpegSessionId_))
                {
                    if (ffmpegSession == null)
                    {
                        return -1.0;
                    }
                    using (var statistics = ffmpegSession.Call<AndroidJavaObject>("getLastReceivedStatistics"))
                    {
                        if (statistics == null)
                        {
                            return 0.0;
                        }
                        return statistics.Call<double>("getSpeed");
                    }
                }
            }
        }

        public override void Update()
        {
            base.Update();

            updateIsRunning();
        }

        void updateIsRunning()
        {
            using (AndroidJavaClass configClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig"))
            using (AndroidJavaObject ffmpegSession = configClass.CallStatic<AndroidJavaObject>("getSession", ffmpegSessionId_))
            {
                if (ffmpegSession == null)
                {
                    //isRunning_ = false;
                    return;
                }

                using (AndroidJavaObject state = ffmpegSession.Call<AndroidJavaObject>("getState"))
                using (AndroidJavaClass sessionState = new AndroidJavaClass("com.arthenica.ffmpegkit.SessionState"))
                using (AndroidJavaObject createdState = sessionState.GetStatic<AndroidJavaObject>("CREATED"))
                using (AndroidJavaObject runningState = sessionState.GetStatic<AndroidJavaObject>("RUNNING"))
                {
                    isRunning_ = state.Call<bool>("equals", createdState) || state.Call<bool>("equals", runningState);
                }
            }
        }

        public static IEnumerator SetFontFiles(string[] files)
        {
            List<string> dirPaths = new List<string>();

            foreach (string file in files)
            {
                string file2 = file.Replace("{PERSISTENT_DATA_PATH}", Application.persistentDataPath)
                    .Replace("{TEMPORARY_CACHE_PATH}", Application.temporaryCachePath);
                string path = file2.Replace("{STREAMING_ASSETS_PATH}", Application.streamingAssetsPath);

                if (path.Contains("://"))
                {
                    string toPath = file2.Replace("{STREAMING_ASSETS_PATH}", Application.temporaryCachePath + "/FfmpegUnity_temp");

                    using (UnityWebRequest webRequest = UnityWebRequest.Get(path))
                    {
                        yield return webRequest.SendWebRequest();
                        File.WriteAllBytes(toPath, webRequest.downloadHandler.data);
                    }

                    path = toPath;
                }

                dirPaths.Add(Path.GetDirectoryName(path));
            }

            using (AndroidJavaClass configClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig"))
            using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaClass arrays = new AndroidJavaClass("java.util.Arrays"))
            using (AndroidJavaObject list = arrays.CallStatic<AndroidJavaObject>("asList", new string[][] { dirPaths.ToArray() }))
            {
                configClass.CallStatic("setFontDirectoryList", activity, list, null);
            }
        }
    }
}
