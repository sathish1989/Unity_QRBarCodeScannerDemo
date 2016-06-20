namespace InstantBonusGames
{
    using UnityEngine;
    using System;
    using System.Collections;
    using ZXing;
    using UnityEngine.UI;


    [AddComponentMenu("System/BarCodeReader")]
    public class BarCodeReader : MonoBehaviour
    {
        public bool doQRCodeScanning = true;

        private bool cameraInitialized;

        private BarcodeReader barCodeReader;

#if UNITY_IOS || UNITY_ANDROID
        private WebCamTexture deviceCamTexture;
#endif

        public RawImage webCamRenderer;
        public GameObject displayText;

        Button enterButton;
        InputField enteredText;

        static IEnumerator coroutine;

        public CanvasGroup flashCanvas;
        bool flash = false;

        #region Unity relation functions

        void Awake()
        {
        }

        IEnumerator Start()
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam | UserAuthorization.Microphone);

            //iOS & Android purpose only
            if (Application.platform == RuntimePlatform.WindowsPlayer ||
                    Application.platform == RuntimePlatform.WindowsEditor)
            {
                webCamRenderer.transform.localEulerAngles = new Vector3(0, 180f, 0);
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                webCamRenderer.transform.localEulerAngles = new Vector3(0, 180f, 270f);
            }
            else if(Application.platform == RuntimePlatform.Android)
            {
                webCamRenderer.transform.localEulerAngles = new Vector3(0, 0, 270f);
            }

            barCodeReader = new BarcodeReader();
            barCodeReader.AutoRotate = true;

            InitializeDeviceCamera();

            ScanBarCode();
        }

        private void Update()
        {
#if UNITY_IOS || UNITY_ANDROID
            if (Application.HasUserAuthorization(UserAuthorization.WebCam | UserAuthorization.Microphone))
            {
                if (flash)
                {
                    flashCanvas.alpha = flashCanvas.alpha - Time.deltaTime;
                    if (flashCanvas.alpha <= 0)
                    {
                        flashCanvas.alpha = 0;
                        flash = false;
                    }
                }

                if (cameraInitialized && doQRCodeScanning)
                {
                    if (!deviceCamTexture.didUpdateThisFrame)
                        return;

                    try
                    {
                        Color32LuminanceSource lum = new Color32LuminanceSource(deviceCamTexture.width, deviceCamTexture.height);
                        lum.SetPixels(deviceCamTexture.GetPixels32());
                        lum.rotateCounterClockwise();
                        
                        var data = barCodeReader.Decode(lum);

                        if (data == null)
                        {
                            return;
                        }

                        if (data != null)
                        {
                            doQRCodeScanning = false;

                            // QRCode detected.
                            Debug.Log(data.Text);

                            displayText.gameObject.GetComponent<TextMesh>().text = data.Text;

                            coroutine = ProcessScan(data.Text);
                            StartCoroutine(coroutine);

                            data = null;
                            //menus.LoginComplete(); // should be an event instead of reference to menus, but this is faster to code for now
                        }
                        else
                        {
                            Debug.Log("No QR code detected !");
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                        StopBarCodeScan();
                    }
                }
            }
#endif
        }

        void OnDestroy()
        {
            StopBarCodeScan();
        }

        #endregion

        #region Camera related

        public void InitializeDeviceCamera()
        {
#if UNITY_IOS || UNITY_ANDROID
            if (Application.HasUserAuthorization(UserAuthorization.WebCam | UserAuthorization.Microphone))
            {

                deviceCamTexture = new WebCamTexture();
                webCamRenderer.texture = deviceCamTexture;

                webCamRenderer.gameObject.SetActive(false);

                cameraInitialized = true;

                //FlipUV (false, true);
            }
#endif
        }

        void FlipUV(bool horzFlip, bool vertFlip)
        {
            var mesh = webCamRenderer.GetComponent<MeshFilter>().mesh;

            var uvs = mesh.uv;

            for (var i = 0; i < uvs.Length; ++i)
            {
                if (vertFlip)
                {
                    if (Mathf.Approximately(uvs[i].y, 1.0f))
                    {
                        uvs[i].y = 0.0f;
                    }
                    else
                    {
                        uvs[i].y = 1.0f;
                    }
                }

                if (horzFlip)
                {
                    if (Mathf.Approximately(uvs[i].x, 1.0f))
                    {
                        uvs[i].x = 0.0f;
                    }
                    else
                    {
                        uvs[i].x = 1.0f;
                    }
                }
            }

            mesh.uv = uvs;
        }

        public void ScanBarCode()
        {
#if UNITY_IOS || UNITY_ANDROID
            if (WebCamTexture.devices.Length > 0)
            {
                webCamRenderer.gameObject.SetActive(true);

                doQRCodeScanning = true;

                if (Application.HasUserAuthorization(UserAuthorization.WebCam | UserAuthorization.Microphone))
                {
                    deviceCamTexture.Play();
                }
            }
#endif
        }

        public void StopBarCodeScan()
        {
#if UNITY_IOS || UNITY_ANDROID
            if (Application.HasUserAuthorization(UserAuthorization.WebCam | UserAuthorization.Microphone))
            {
                deviceCamTexture.Stop();
            }

            doQRCodeScanning = false;

            webCamRenderer.gameObject.SetActive(false);
#endif
        }

        #endregion

        IEnumerator ProcessScan(string scanData)
        {
            flashCanvas.alpha = 1;
            flash = true;
            GetComponent<AudioSource>().Play();
            yield return new WaitForSeconds(2f);
			doQRCodeScanning = true;

            StopCoroutine(coroutine);
        }
    }
}