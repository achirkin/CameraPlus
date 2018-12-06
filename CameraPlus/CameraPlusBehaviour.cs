﻿using SFB;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using VRUIControls;
using ContextMenu = System.Windows.Forms.ContextMenu;
using Cursor = System.Windows.Forms.Cursor;
using Screen = UnityEngine.Screen;

namespace CameraPlus
{
    public class CameraPlusBehaviour : MonoBehaviour
    {
        protected readonly WaitUntil _waitForMainCamera = new WaitUntil(() => Camera.main);
        private readonly WaitForSecondsRealtime _waitForSecondsRealtime = new WaitForSecondsRealtime(1f);
        protected const int OnlyInThirdPerson = 3;
        protected const int OnlyInFirstPerson = 4;

        public bool ThirdPerson
        {
            get { return _thirdPerson; }
            set
            {
                _thirdPerson = value;
                _cameraCube.gameObject.SetActive(_thirdPerson && Config.showThirdPersonCamera);
                _cameraPreviewQuad.gameObject.SetActive(_thirdPerson && Config.showThirdPersonCamera);

                if (value)
                {
                    _cam.cullingMask &= ~(1 << OnlyInFirstPerson);
                    _cam.cullingMask |= 1 << OnlyInThirdPerson;
                }
                else
                {
                    _cam.cullingMask &= ~(1 << OnlyInThirdPerson);
                    _cam.cullingMask |= 1 << OnlyInFirstPerson;
                }
            }
        }

        protected bool _thirdPerson;
        public Vector3 ThirdPersonPos;
        public Vector3 ThirdPersonRot;
        public Config Config;

        protected RenderTexture _camRenderTexture;
        protected Material _previewMaterial;
        protected Camera _cam;
        protected Transform _cameraCube;
        protected ScreenCameraBehaviour _screenCamera;
        protected GameObject _cameraPreviewQuad;
        protected Camera _mainCamera = null;
        protected CameraMoverPointer _moverPointer = null;
        protected GameObject _cameraCubeGO;
        protected GameObject _quad;

        protected int _prevScreenWidth;
        protected int _prevScreenHeight;
        protected int _prevAA;
        protected float _prevRenderScale;
        protected int _prevLayer;
        protected float _aspectRatio;

        protected bool _mouseHeld = false;
        protected bool _isResizing = false;
        protected bool _isMoving = false;
        protected bool _contextMenuOpen = false;
        protected DateTime _lastRenderUpdate;
        protected Vector2 _initialOffset = new Vector2(0, 0);
        protected Vector2 _lastGrabPos = new Vector2(0, 0);
        protected Vector2 _initialTopRightPos = new Vector2(0, 0);
        protected ContextMenuStrip _menuStrip = new ContextMenuStrip();

        public virtual void Init(Config config)
        {
            DontDestroyOnLoad(gameObject);
            Plugin.Log("Created new camera plus behaviour component!");

            Config = config;

            StartCoroutine(DelayedInit());
        }

        protected IEnumerator DelayedInit()
        {
            yield return _waitForMainCamera;
            _mainCamera = Camera.main;
            _menuStrip = null;
            XRSettings.showDeviceView = false;

            Config.ConfigChangedEvent += PluginOnConfigChangedEvent;

            var gameObj = Instantiate(_mainCamera.gameObject);
            gameObj.SetActive(false);
            gameObj.name = "Camera Plus";
            gameObj.tag = "Untagged";
            while (gameObj.transform.childCount > 0) DestroyImmediate(gameObj.transform.GetChild(0).gameObject);
            DestroyImmediate(gameObj.GetComponent("CameraRenderCallbacksManager"));
            DestroyImmediate(gameObj.GetComponent("AudioListener"));
            DestroyImmediate(gameObj.GetComponent("MeshCollider"));

            if (SteamVRCompatibility.IsAvailable)
            {
                DestroyImmediate(gameObj.GetComponent(SteamVRCompatibility.SteamVRCamera));
                DestroyImmediate(gameObj.GetComponent(SteamVRCompatibility.SteamVRFade));
            }

            _screenCamera = new GameObject("Screen Camera").AddComponent<ScreenCameraBehaviour>();

            if (_previewMaterial == null)
            {
                _previewMaterial = new Material(Shader.Find("Hidden/BlitCopyWithDepth"));
            }

            _cam = gameObj.GetComponent<Camera>();
            _cam.stereoTargetEye = StereoTargetEyeMask.None;
            _cam.enabled = true;

            gameObj.SetActive(true);

            var camera = _mainCamera.transform;
            transform.position = camera.position;
            transform.rotation = camera.rotation;

            gameObj.transform.parent = transform;
            gameObj.transform.localPosition = Vector3.zero;
            gameObj.transform.localRotation = Quaternion.identity;
            gameObj.transform.localScale = Vector3.one;

            _cameraCubeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DontDestroyOnLoad(_cameraCubeGO);
            _cameraCubeGO.SetActive(ThirdPerson);
            _cameraCube = _cameraCubeGO.transform;
            _cameraCube.localScale = new Vector3(0.15f, 0.15f, 0.22f);
            _cameraCube.name = "CameraCube";

            _quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DontDestroyOnLoad(_quad);
            DestroyImmediate(_quad.GetComponent<Collider>());
            _quad.GetComponent<MeshRenderer>().material = _previewMaterial;
            _quad.transform.parent = _cameraCube;
            _quad.transform.localPosition = new Vector3(-1f * ((_cam.aspect - 1) / 2 + 1), 0, 0.22f);
            _quad.transform.localEulerAngles = new Vector3(0, 180, 0);
            _quad.transform.localScale = new Vector3(_cam.aspect, 1, 1);
            _cameraPreviewQuad = _quad;

            ReadConfig();

            if (ThirdPerson)
            {
                ThirdPersonPos = Config.Position;
                ThirdPersonRot = Config.Rotation;

                transform.position = ThirdPersonPos;
                transform.eulerAngles = ThirdPersonRot;

                _cameraCube.position = ThirdPersonPos;
                _cameraCube.eulerAngles = ThirdPersonRot;
            }

            SceneManager_activeSceneChanged(new Scene(), new Scene());
        }

        protected virtual void OnDestroy()
        {
            Config.ConfigChangedEvent -= PluginOnConfigChangedEvent;

            if (_screenCamera)
                Destroy(_screenCamera.gameObject);

            if (_cameraCubeGO)
                Destroy(_cameraCubeGO);

            if (_quad)
                Destroy(_quad);

            if (_camRenderTexture)
                _camRenderTexture.Release();

            if(_contextMenuOpen)
                _menuStrip.Dispose();
        }

        protected virtual void PluginOnConfigChangedEvent(Config config)
        {
            ReadConfig();
        }

        protected virtual void ReadConfig()
        {
            ThirdPerson = Config.thirdPerson;

            if (!ThirdPerson)
            {
                transform.position = _mainCamera.transform.position;
                transform.rotation = _mainCamera.transform.rotation;
            }
            else
            {
                ThirdPersonPos = Config.Position;
                ThirdPersonRot = Config.Rotation;
            }

            CreateScreenRenderTexture();
            SetFOV();
        }

        protected virtual void CreateScreenRenderTexture()
        {
            HMMainThreadDispatcher.instance.Enqueue(delegate
            {
                var replace = false;
                if (_camRenderTexture == null)
                {
                    _camRenderTexture = new RenderTexture(1, 1, 24);
                    replace = true;
                }
                else
                {
                    if (Config.antiAliasing != _prevAA || Config.renderScale != _prevRenderScale || Config.screenHeight != _prevScreenHeight || Config.screenWidth != _prevScreenWidth || Config.layer != _prevLayer)
                    {
                        replace = true;

                        _cam.targetTexture = null;
                        _screenCamera.SetRenderTexture(null);
                        _screenCamera.SetCameraInfo(new Vector2(0, 0), new Vector2(0, 0), -1000);

                        _camRenderTexture.Release();

                        _prevAA = Config.antiAliasing;
                        _prevRenderScale = Config.renderScale;
                        _prevScreenHeight = Config.screenHeight;
                        _prevScreenWidth = Config.screenWidth;
                    }
                }

                if (!replace)
                {
                    Plugin.Log("Don't need to replace");
                    return;
                }

                _lastRenderUpdate = DateTime.Now;
                GetScaledScreenResolution(Config.renderScale, out var scaledWidth, out var scaledHeight);
                _camRenderTexture.width = scaledWidth;
                _camRenderTexture.height = scaledHeight;

                _camRenderTexture.useDynamicScale = false;
                _camRenderTexture.antiAliasing = Config.antiAliasing;
                _camRenderTexture.Create();

                _cam.targetTexture = _camRenderTexture;
                _previewMaterial.SetTexture("_MainTex", _camRenderTexture);
                _screenCamera.SetRenderTexture(_camRenderTexture);
                _screenCamera.SetCameraInfo(Config.ScreenPosition, Config.ScreenSize, Config.layer);
            });
        }

        protected virtual void GetScaledScreenResolution(float scale, out int scaledWidth, out int scaledHeight)
        {
            _aspectRatio = (float)Screen.height / Screen.width;
            scaledWidth = Mathf.Clamp(Mathf.RoundToInt(Screen.width * scale), 1, int.MaxValue);
            scaledHeight = Mathf.Clamp(Mathf.RoundToInt(scaledWidth * _aspectRatio), 1, int.MaxValue);
        }

        public virtual void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            StartCoroutine(GetMainCamera());
            var pointer = Resources.FindObjectsOfTypeAll<VRPointer>().FirstOrDefault();
            if (pointer == null) return;
            if (_moverPointer) Destroy(_moverPointer);
            _moverPointer = pointer.gameObject.AddComponent<CameraMoverPointer>();
            _moverPointer.Init(this, _cameraCube);
        }

        protected IEnumerator GetMainCamera()
        {
            yield return _waitForMainCamera;
            _mainCamera = Camera.main;
        }

        protected virtual void LateUpdate()
        {
            var camera = _mainCamera.transform;

            if (ThirdPerson)
            {
                transform.position = ThirdPersonPos;
                transform.eulerAngles = ThirdPersonRot;
                _cameraCube.position = ThirdPersonPos;
                _cameraCube.eulerAngles = ThirdPersonRot;
                return;
            }

            transform.position = Vector3.Lerp(transform.position, camera.position,
                Config.positionSmooth * Time.unscaledDeltaTime);

            transform.rotation = Quaternion.Slerp(transform.rotation, camera.rotation,
                Config.rotationSmooth * Time.unscaledDeltaTime);
        }

        protected virtual void SetFOV()
        {
            if (_cam == null) return;
            var fov = (float)(57.2957801818848 *
                               (2.0 * Mathf.Atan(
                                    Mathf.Tan((float)(Config.fov * (Math.PI / 180.0) * 0.5)) /
                                    _mainCamera.aspect)));
            _cam.fieldOfView = fov;
        }

        public bool IsWithinRenderArea(Vector2 mousePos, Config c)
        {
            if (mousePos.x < c.screenPosX) return false;
            if (mousePos.x > c.screenPosX + c.screenWidth) return false;
            if (mousePos.y < c.screenPosY) return false;
            if (mousePos.y > c.screenPosY + c.screenHeight) return false;
            return true;
        }

        public bool IsTopmostRenderAreaAtPos(Vector2 mousePos)
        {
            if (!IsWithinRenderArea(mousePos, Config)) return false;
            foreach (CameraPlusInstance c in Plugin.Instance.Cameras.Values.ToArray())
            {
                if (c.Instance == this) continue;
                if (!IsWithinRenderArea(mousePos, c.Config) && !c.Instance._mouseHeld) continue;
                if (c.Config.layer > Config.layer)
                {
                    return false;
                }

                if (c.Config.layer == Config.layer && c.Instance._lastRenderUpdate > _lastRenderUpdate)
                {
                    return false;
                }

                if (c.Instance._mouseHeld && (c.Instance._isMoving || c.Instance._isResizing))
                {
                    return false;
                }
            }
            return true;
        }

        protected void CloseContextMenu()
        {
            if (_menuStrip != null)
            {
                _menuStrip.Dispose();
                _menuStrip = null;
            }
            _contextMenuOpen = false;
        }

        protected void OnApplicationFocus(bool hasFocus)
        {
            if(!hasFocus)
                CloseContextMenu();
        }

        protected virtual void Update()
        {
            // Only toggle the main camera in/out of third person with f1, not any extra cams
            if (Path.GetFileName(Config.FilePath) == "cameraplus.cfg")
            {
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    ThirdPerson = !ThirdPerson;
                    if (!ThirdPerson)
                    {
                        transform.position = _mainCamera.transform.position;
                        transform.rotation = _mainCamera.transform.rotation;
                    }
                    else
                    {
                        ThirdPersonPos = Config.Position;
                        ThirdPersonRot = Config.Rotation;
                    }

                    Config.thirdPerson = ThirdPerson;
                    Config.Save();
                }
            }

            bool holdingLeftClick = Input.GetMouseButton(0);
            bool holdingRightClick = Input.GetMouseButton(1);

            if (!_mouseHeld && (holdingLeftClick || holdingRightClick))
            {
                if (_menuStrip != null && Input.mousePosition.x > 0 && Input.mousePosition.x < Screen.width && Input.mousePosition.y > 0 && Input.mousePosition.y < Screen.height)
                {
                    CloseContextMenu();
                }
            }

            Vector3 mousePos = Input.mousePosition;
            if (!_mouseHeld && !IsTopmostRenderAreaAtPos(mousePos)) return;

            if (holdingLeftClick)
            {
                if (!_mouseHeld)
                {
                    _initialOffset.x = mousePos.x - Config.screenPosX;
                    _initialOffset.y = mousePos.y - Config.screenPosY;
                    _initialTopRightPos = Config.ScreenPosition + Config.ScreenSize;
                    _lastGrabPos = new Vector2(mousePos.x, mousePos.y);
                }
                _mouseHeld = true;

                int tol = 15;
                bool withinBottomLeft = (mousePos.x > Config.screenPosX - tol && mousePos.x < Config.screenPosX + tol && mousePos.y > Config.screenPosY - tol && mousePos.y < Config.screenPosY + tol);
                if (_isResizing || withinBottomLeft)
                {
                    _isResizing = true;
                    int changeX = (int)(_lastGrabPos.x - mousePos.x);
                    int changeY = (int)(mousePos.y - _lastGrabPos.y);
                    Config.screenWidth += changeX;
                    Config.screenHeight = Mathf.Clamp(Mathf.RoundToInt(Config.screenWidth * _aspectRatio), 1, int.MaxValue);
                    Config.screenPosX = (int)_initialTopRightPos.x - Config.screenWidth;
                    Config.screenPosY = (int)_initialTopRightPos.y - Config.screenHeight;
                    _lastGrabPos = mousePos;
                }
                else
                {
                    _isMoving = true;
                    Config.screenPosX = (int)mousePos.x - (int)_initialOffset.x;
                    Config.screenPosY = (int)mousePos.y - (int)_initialOffset.y;
                }
                Config.screenWidth = Mathf.Clamp(Config.screenWidth, 0, Screen.width);
                Config.screenHeight = Mathf.Clamp(Config.screenHeight, 0, Screen.height);
                Config.screenPosX = Mathf.Clamp(Config.screenPosX, 0, Screen.width - Config.screenWidth);
                Config.screenPosY = Mathf.Clamp(Config.screenPosY, 0, Screen.height - Config.screenHeight);

                GL.Clear(false, true, Color.black, 0);
                CreateScreenRenderTexture();
            }
            else if (holdingRightClick)
            {
                if (!_mouseHeld)
                {
                    if (_menuStrip == null)
                    {
                        _menuStrip = new ContextMenuStrip();
                        _menuStrip.Items.Add("Add New Camera", null, (p1, p2) =>
                        {
                            lock (Plugin.Instance.Cameras)
                            {
                                int index = 0;
                                string cameraName = String.Empty;
                                while (true)
                                {
                                    restart:
                                    index++;
                                    cameraName = $"customcamera{index.ToString()}";
                                    Plugin.Log($"Checking {cameraName}");
                                    if (!CameraUtilities.CameraExists(cameraName))
                                        break;
                                    else
                                        goto restart;
                                }
                                Plugin.Log($"Adding {cameraName}");
                                CameraUtilities.AddNewCamera(cameraName);
                                CameraUtilities.ReloadCameras();
                                CloseContextMenu();
                            }
                        });
                        _menuStrip.Items.Add(new ToolStripSeparator());
                        _menuStrip.Items.Add(Config.thirdPerson ? "First Person" : "Third Person", null, (p1, p2) =>
                        {
                            Plugin.Log("Toggling third person!");
                            Config.thirdPerson = !Config.thirdPerson;
                            ThirdPerson = Config.thirdPerson;
                            CreateScreenRenderTexture();
                            CloseContextMenu();
                        });
                        if (Config.thirdPerson)
                        {
                            _menuStrip.Items.Add(Config.showThirdPersonCamera ? "Hide Third Person Camera" : "Show Third Person Camera", null, (p1, p2) =>
                            {
                                Plugin.Log("Toggling third person camera!");
                                Config.showThirdPersonCamera = !Config.showThirdPersonCamera;
                                ThirdPerson = Config.thirdPerson;
                                CreateScreenRenderTexture();
                                CloseContextMenu();
                            });
                        }

                        _menuStrip.Items.Add(new ToolStripSeparator());
                        //var layerBox = new ToolStripComboBox("Layer");
                        //_menuStrip.Items.Add(layerBox);
                        _menuStrip.Items.Add("Up One Layer", null, (p1, p2) =>
                        {
                            Plugin.Log("Moving up one layer!");
                            Config.layer++;
                            CreateScreenRenderTexture();
                            CloseContextMenu();
                        });
                        _menuStrip.Items.Add("Down One Layer", null, (p1, p2) =>
                        {
                            Plugin.Log("Moving down one layer!");
                            Config.layer--;
                            CreateScreenRenderTexture();
                            CloseContextMenu();
                        });

                        _menuStrip.Items.Add("Remove Camera", null, (p1, p2) =>
                        {
                            Plugin.Log("Removing camera!");
                            lock (Plugin.Instance.Cameras)
                            {
                                if (CameraUtilities.RemoveCamera(this))
                                {
                                    CreateScreenRenderTexture();
                                    CloseContextMenu();
                                    GL.Clear(false, true, Color.black, 0);
                                    Destroy(this.gameObject);
                                }
                                else MessageBox.Show("Cannot remove main camera!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        });
                        _contextMenuOpen = true;
                    }
                    if (_menuStrip != null)
                    {
                        _menuStrip.SetBounds(Cursor.Position.X, Cursor.Position.Y, 0, 0);
                        if (!_menuStrip.Visible)
                            _menuStrip.Show();
                    }
                }
                _mouseHeld = true;
            }
            else
            {
                _isResizing = false;
                _isMoving = false;
                _mouseHeld = false;
            }
        }
    }
}