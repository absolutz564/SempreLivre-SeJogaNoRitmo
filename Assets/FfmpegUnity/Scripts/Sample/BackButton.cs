using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
using FfmpegUnity;
using UnityEngine.Video;
// using Nexweron.WebCamPlayer; // removido: dependência do projeto PetrobrasVideo
using UnityEngine.UI;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace NekraliusDevelopmentStudio
{
    public class BackButton : MonoBehaviour
    {
        public static string PrevSceneName;
        public int videoDuration = 15;
        public GameObject ffmpeg;
        public TextMeshProUGUI countdownText;
        public GameObject ImageCircle;

        public GameObject photoShower;
        // public CapturePhotos capturePhotos; // removido: dependência do projeto PetrobrasVideo
        public string urlVideo;
        public string endpointVideo;
        public string redirectLink;
        public string token;
        public string accessToken;
        public VideoPlayer videoPlayer;
        public UnityEngine.UI.RawImage videoRaw;
        public RenderTexture renderTexture;
        public UnityEngine.UI.RawImage rawImage;

        private string base64QRCode;

        public GameObject LoadingObject;
        public GameObject Moldura;
        // public WebCamStream cameraStream; // removido: dependência do projeto PetrobrasVideo

        private void Start()
        {

        }

        public void Init()
        {
            // cameraStream.Play();
            StartCoroutine(CountdownAndStartFfmpeg());
        }

        IEnumerator CountdownAndStartFfmpeg()
        {
            int countdown = 3;

            while (countdown > 0)
            {
                if (countdownText != null)
                    countdownText.text = countdown.ToString();

                yield return new WaitForSeconds(1f);
                countdown--;
            }

            if (countdownText != null)
                countdownText.text = "";

            ImageCircle.SetActive(false);
            countdownText.gameObject.SetActive(false);
            Moldura.SetActive(true);
            StatFfmpeg();

            yield return new WaitForSeconds(videoDuration);

            StartCoroutine(quitCoroutine());
            StartCoroutine(WaitStop());
        }

        IEnumerator WaitStop()
        {
            UnityEngine.Debug.Log("iniciou recoder");
            yield return new WaitForSeconds(1);
            LoadingObject.SetActive(true);

            string videoFilePath = Path.Combine(Application.persistentDataPath, "capture.mp4");


            yield return new WaitForSeconds(1.0f);

            while (!File.Exists(videoFilePath))
            {
                UnityEngine.Debug.LogWarning("Arquivo ainda n�o dispon�vel, aguardando...");
                yield return new WaitForSeconds(0.5f);
            }

            bool uploadSuccessful = false;

            while (!uploadSuccessful)
            {
                bool sharingViolation = false;

                try
                {
                    using (FileStream stream = File.Open(videoFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        // Apenas testa se pode acessar � nada precisa ser feito aqui
                    }

                    UploadVideo(videoFilePath);
                    uploadSuccessful = true;
                }
                catch (IOException ex)
                {
                    sharingViolation = true;
                    UnityEngine.Debug.LogWarning("Sharing violation, tentando novamente: " + ex.Message);
                }

                if (!uploadSuccessful && sharingViolation)
                {
                    yield return new WaitForSeconds(1.0f);
                }
            }

            UnityEngine.Debug.Log("Video upload locally complete");
        }

        public void StatFfmpeg()
        {
            ffmpeg.SetActive(true);
        }

        IEnumerator quitCoroutine()
        {
            FfmpegCommand[] commands = FindObjectsOfType<FfmpegCommand>();
            foreach (var command in commands)
            {
                command.StopFfmpeg();
            }

            bool loopFlag;
            do
            {
                yield return null;
                loopFlag = false;

                foreach (var command in commands)
                {
                    if (command.IsRunning)
                    {
                        loopFlag = true;
                        break;
                    }
                }
            } while (loopFlag);
        }

        public void UploadVideo(string videoFilePath)
        {
            StartCoroutine(VideoSend(videoFilePath));
        }

        public void LoadNextSccene()
        {
            SceneManager.LoadScene(2);
        }

        public IEnumerator VideoSend(string videoFilePath)
        {
            photoShower.SetActive(true);
            string previewPath = Path.Combine(Application.persistentDataPath, "capture.mp4");
            PlayPreview(previewPath);

            yield return new WaitForSeconds(1);

            Debug.Log("Starting video upload...");
            Moldura.SetActive(false);
            //capturePhotos.VideoUploadMessage.SetActive(true);

            string uniqueVideoFileName = "capture.mp4";
            byte[] videoBytes = File.ReadAllBytes(videoFilePath);

            Debug.Log($"Loaded video file {videoFilePath}, size: {videoBytes.Length} bytes");

            WWWForm form = new WWWForm();
            form.AddField("isFileIdentify", "false");
            form.AddField("identify", "false");
            form.AddField("url_redirect", redirectLink);
            form.AddBinaryData("file", videoBytes, uniqueVideoFileName, "video/mp4");

            string fullUrl = urlVideo + endpointVideo;

            using (UnityWebRequest request = UnityWebRequest.Post(fullUrl, form))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("access_token", accessToken);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error uploading video: {request.error}");
                    Debug.LogError($"Response Code: {request.responseCode}");
                    Debug.LogError($"Response: {request.downloadHandler.text}");
                }
                else
                {
                    Debug.Log("Video uploaded successfully");
                    string jsonString = request.downloadHandler.text;
                    QRCodeData data = JsonUtility.FromJson<QRCodeData>(jsonString);

                    string base64Code = data.qrcode;
                    string base64Only = base64Code.Substring(base64Code.IndexOf(",") + 1);
                    Debug.Log("Base64 Code: " + base64Only);
                    Debug.Log(data.image);
                    base64QRCode = base64Only;
                    StartCoroutine(LoadQRCode());
                }
            }
        }
        public void PlayPreview(string videoUrl)
        {
            Debug.Log("Preview URL: " + videoUrl);

            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoUrl;

            videoPlayer.renderMode = VideoRenderMode.RenderTexture;

            if (renderTexture == null)
                renderTexture = new RenderTexture(1920, 1080, 0);

            videoPlayer.targetTexture = renderTexture;
            videoRaw.texture = renderTexture;

            videoRaw.rectTransform.sizeDelta = new Vector2(677, 1200); // Mant�m o tamanho desejado

            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.isLooping = false;

            videoPlayer.Prepare();
        }

        //public void PlayPreview(string videoFilePath)
        //{
        //    CaptureFrame(videoPlayer, videoRaw);
        //}

        private IEnumerator LoadQRCode()
        {
            // Decodifica a string base64 em uma textura
            byte[] bytes = Convert.FromBase64String(base64QRCode);
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(bytes);

            // Define a textura na RawImage
            rawImage.texture = texture;
            LoadingObject.SetActive(false);

            yield return null;
        }

        [Serializable]
        public class QRCodeData
        {
            public string qrcode;
            public string image;
        }
    }
}
