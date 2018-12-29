using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using VRUIControls;
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
        protected CameraMovement _cameraMovement = null;

        protected int _prevScreenWidth;
        protected int _prevScreenHeight;
        protected int _prevAA;
        protected float _prevRenderScale;
        protected int _prevLayer;
        protected float _aspectRatio;

        protected bool _wasWindowActive = false;
        protected bool _mouseHeld = false;
        protected bool _isResizing = false;
        protected bool _isMoving = false;
        protected bool _contextMenuOpen = false;
        protected bool _isCameraDestroyed = false;
        protected DateTime _lastRenderUpdate;
        protected Vector2 _initialOffset = new Vector2(0, 0);
        protected Vector2 _lastGrabPos = new Vector2(0, 0);
        protected Vector2 _initialTopRightPos = new Vector2(0, 0);
        protected ContextMenuStrip _menuStrip = new ContextMenuStrip();
        protected List<ToolStripItem> _controlTracker = new List<ToolStripItem>();

        [DllImport("user32.dll")]
        static extern System.IntPtr GetActiveWindow();

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

            _cameraMovement = gameObj.AddComponent<CameraMovement>();
            if (Config.movementScriptPath != String.Empty)
                _cameraMovement.Init(this);

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
            _cam.name = Path.GetFileName(Config.FilePath);

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
            Plugin.Log($"Camera \"{Path.GetFileName(Config.FilePath)} successfully initialized!\"");
        }
        
        protected virtual void OnDestroy()
        {
            Config.ConfigChangedEvent -= PluginOnConfigChangedEvent;

            // Close our context menu if its open, and destroy all associated controls, otherwise the game will lock up
            CloseContextMenu();

            _camRenderTexture.Release();

            if (_screenCamera)
                Destroy(_screenCamera.gameObject);
            if (_cameraCubeGO)
                Destroy(_cameraCubeGO);
            if (_quad)
                Destroy(_quad);
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
            var pointer = to.name == "GameCore" ? Resources.FindObjectsOfTypeAll<VRPointer>().Last() : Resources.FindObjectsOfTypeAll<VRPointer>().First();
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

                if (c.Instance._mouseHeld && (c.Instance._isMoving || c.Instance._isResizing || c.Instance._contextMenuOpen))
                {
                    return false;
                }
            }
            return true;
        }
        
        protected virtual void Update()
        {
            if (GetActiveWindow() == System.IntPtr.Zero && _wasWindowActive)
            {
                CloseContextMenu();
                _wasWindowActive = false;
            }
            else
                _wasWindowActive = true;

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
            HandleContextMenu();
        }

        protected void CloseContextMenu()
        {
            if (_menuStrip != null)
            {
                _menuStrip.Close();
                _menuStrip.Items.Clear();
                foreach (ToolStripItem item in _controlTracker)
                {
                    if (item is ToolStripMenuItem)
                        (item as ToolStripMenuItem).DropDownItems.Clear();
                    item.Dispose();
                }
                _menuStrip.Dispose();
                _menuStrip = null;
            }
            _contextMenuOpen = false;
        }

        public static byte[] GetResource(Assembly asm, string ResourceName)
        {
            System.IO.Stream stream = asm.GetManifestResourceStream(ResourceName);
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
            return data;
        }

        protected void HandleContextMenu()
        {
            bool holdingLeftClick = Input.GetMouseButton(0);
            bool holdingRightClick = Input.GetMouseButton(1);

            // Close the context menu when we click anywhere within the bounds of the application
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
                if (_mouseHeld) return;
                if (_menuStrip == null)
                {
                    _menuStrip = new ContextMenuStrip();
                    // Adds a new camera into the scene
                    _menuStrip.Items.Add("Add New Camera", null, (p1, p2) =>
                    {
                        lock (Plugin.Instance.Cameras)
                        {
                            string cameraName = CameraUtilities.GetNextCameraName();
                            Plugin.Log($"Adding new config with name {cameraName}.cfg");
                            CameraUtilities.AddNewCamera(cameraName);
                            CameraUtilities.ReloadCameras();
                            CloseContextMenu();
                        }
                    });

                    // Instantiates an exact copy of the currently selected camera
                    _menuStrip.Items.Add("Duplicate Selected Camera", null, (p1, p2) =>
                    {
                        lock (Plugin.Instance.Cameras)
                        {
                            string cameraName = CameraUtilities.GetNextCameraName();
                            Plugin.Log($"Adding {cameraName}");
                            CameraUtilities.AddNewCamera(cameraName, Config);
                            CameraUtilities.ReloadCameras();
                            CloseContextMenu();
                        }
                    });

                    // Removes the selected camera from the scene, and deletes the config associated with it
                    _menuStrip.Items.Add("Remove Selected Camera", null, (p1, p2) =>
                    {
                        lock (Plugin.Instance.Cameras)
                        {
                            if (CameraUtilities.RemoveCamera(this))
                            {
                                _isCameraDestroyed = true;
                                CreateScreenRenderTexture();
                                CloseContextMenu();
                                GL.Clear(false, true, Color.black, 0);
                                Destroy(this.gameObject);
                                Plugin.Log("Camera removed!");
                            }
                            else MessageBox.Show("Cannot remove main camera!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    });
                    _menuStrip.Items.Add(new ToolStripSeparator());

                    // Toggles between third/first person
                    _menuStrip.Items.Add(Config.thirdPerson ? "First Person" : "Third Person", null, (p1, p2) =>
                    {
                        Config.thirdPerson = !Config.thirdPerson;
                        ThirdPerson = Config.thirdPerson;
                        CreateScreenRenderTexture();
                        CloseContextMenu();
                        Config.Save();
                    });
                    if (Config.thirdPerson)
                    {
                        // Hides/unhides the third person camera that appears when a camera is in third person mode
                        _menuStrip.Items.Add(Config.showThirdPersonCamera ? "Hide Third Person Camera" : "Show Third Person Camera", null, (p1, p2) =>
                        {
                            Config.showThirdPersonCamera = !Config.showThirdPersonCamera;
                            ThirdPerson = Config.thirdPerson;
                            CreateScreenRenderTexture();
                            CloseContextMenu();
                            Config.Save();
                        });
                    }
                    _menuStrip.Items.Add(new ToolStripSeparator());

                    var _layoutMenu = new ToolStripMenuItem("Layout");
                    _controlTracker.Add(_layoutMenu);
                    // Sets the layer associated with the current camera
                    _layoutMenu.DropDownItems.Add(new ToolStripLabel("Layer"));
                    var _layerBox = new ToolStripNumberControl();
                    _controlTracker.Add(_layerBox);
                    _layerBox.Maximum = int.MaxValue;
                    _layerBox.Minimum = int.MinValue;
                    _layerBox.Value = Config.layer;
                    _layerBox.ValueChanged += (sender, args) =>
                    {
                        Config.layer = (int)_layerBox.Value;
                        CreateScreenRenderTexture();
                        Config.Save();
                    };
                    _layoutMenu.DropDownItems.Add(_layerBox);

                    // Sets the size of the current cameras pixelrect
                    _layoutMenu.DropDownItems.Add(new ToolStripLabel("Size"));
                    var _widthBox = new ToolStripNumberControl();
                    _controlTracker.Add(_widthBox);
                    _widthBox.Maximum = Screen.width;
                    _widthBox.Minimum = 0;
                    _widthBox.Value = Config.screenWidth;
                    _widthBox.ValueChanged += (sender, args) =>
                    {
                        Config.screenWidth = (int)_widthBox.Value;
                        CreateScreenRenderTexture();
                        Config.Save();
                    };
                    _layoutMenu.DropDownItems.Add(_widthBox);
                    var _heightBox = new ToolStripNumberControl();
                    _controlTracker.Add(_heightBox);
                    _heightBox.Maximum = Screen.height;
                    _heightBox.Minimum = 0;
                    _heightBox.Value = Config.screenHeight;
                    _heightBox.ValueChanged += (sender, args) =>
                    {
                        Config.screenHeight = (int)_heightBox.Value;
                        CreateScreenRenderTexture();
                        Config.Save();
                    };
                    _layoutMenu.DropDownItems.Add(_heightBox);

                    // Set the location of the current cameras pixelrect
                    _layoutMenu.DropDownItems.Add(new ToolStripLabel("Position"));
                    var _xBox = new ToolStripNumberControl();
                    _controlTracker.Add(_xBox);
                    _xBox.Maximum = Screen.width;
                    _xBox.Minimum = 0;
                    _xBox.Value = Config.screenPosX;
                    _xBox.ValueChanged += (sender, args) =>
                    {
                        Config.screenPosX = (int)_xBox.Value;
                        CreateScreenRenderTexture();
                        Config.Save();
                    };
                    _layoutMenu.DropDownItems.Add(_xBox);
                    var _yBox = new ToolStripNumberControl();
                    _controlTracker.Add(_yBox);
                    _yBox.Maximum = Screen.height;
                    _yBox.Minimum = 0;
                    _yBox.Value = Config.screenPosY;
                    _yBox.ValueChanged += (sender, args) =>
                    {
                        Config.screenPosY = (int)_yBox.Value;
                        CreateScreenRenderTexture();
                        Config.Save();
                    };
                    _layoutMenu.DropDownItems.Add(_yBox);
                    _menuStrip.Items.Add(_layoutMenu);

                    // FOV
                    _layoutMenu.DropDownItems.Add(new ToolStripLabel("FOV"));
                    var _fov = new ToolStripNumberControl();
                    _controlTracker.Add(_fov);
                    _fov.Maximum = Screen.width;
                    _fov.Minimum = 0;
                    _fov.Value = (decimal)Config.fov;

                    _fov.ValueChanged += (sender, args) =>
                    {
                        Config.fov = (int)_fov.Value;
                        SetFOV();
                        CreateScreenRenderTexture();
                        Config.Save();
                    };
                    _layoutMenu.DropDownItems.Add(_fov);

                    // Scripts submenu
                    var _scriptsMenu = new ToolStripMenuItem("Scripts");
                    _controlTracker.Add(_scriptsMenu);

                    // Add menu
                    var _addMenu = new ToolStripMenuItem("Add");
                    _controlTracker.Add(_addMenu);
                    ToolStripItem _addCameraMovement = _addMenu.DropDownItems.Add("Camera Movement", null, (p1, p2) =>
                    {
                        OpenFileDialog ofd = new OpenFileDialog();
                        string path = Path.Combine(Environment.CurrentDirectory, "UserData\\CameraPlus\\Scripts");
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);
                        
                        string defaultScript = Path.Combine(path, "CameraMovementData.json");
                        if (!File.Exists(defaultScript))
                            File.WriteAllBytes(defaultScript, GetResource(Assembly.GetExecutingAssembly(), "CameraPlus.Resources.CameraMovementData.json"));
                        
                        ofd.InitialDirectory = path;
                        ofd.Title = "Select a script";
                        ofd.FileOk += (sender, e) => { string file = ((OpenFileDialog)sender).FileName;
                            if (File.Exists(file))
                            {
                                Config.movementScriptPath = file;
                                if (Config.movementScriptPath != String.Empty)
                                {
                                    if (_cameraMovement.Init(this))
                                    {
                                        Config.thirdPerson = true;
                                        ThirdPerson = true;
                                        CreateScreenRenderTexture();
                                    }
                                }
                                Config.Save();
                            }
                        };
                        ofd.ShowDialog();
                        CloseContextMenu();
                    });
                    _addCameraMovement.Enabled = Config.movementScriptPath == String.Empty;
                    _scriptsMenu.DropDownItems.Add(_addMenu);

                    // Remove menu
                    var _removeMenu = new ToolStripMenuItem("Remove");
                    _controlTracker.Add(_removeMenu);
                    ToolStripItem _removeCameraMovement = _removeMenu.DropDownItems.Add("Camera Movement", null, (p1, p2) =>
                    {
                        Config.movementScriptPath = String.Empty;
                        _cameraMovement.Shutdown();
                        Config.Save();
                        CloseContextMenu();
                    });
                    _removeCameraMovement.Enabled = Config.movementScriptPath != String.Empty;
                    _scriptsMenu.DropDownItems.Add(_removeMenu);
                    _menuStrip.Items.Add(_scriptsMenu);
                    
                    // Extras submenu
                    var _extrasMenu = new ToolStripMenuItem("Extras");
                    _controlTracker.Add(_extrasMenu);
                    // Just the right number...
                    _extrasMenu.DropDownItems.Add("Spawn 38 Cameras", null, (p1, p2) =>
                    {
                        StartCoroutine(CameraUtilities.Spawn38Cameras());
                        CloseContextMenu();
                    });
                    _menuStrip.Items.Add(_extrasMenu);

                    _contextMenuOpen = true;
                }
                if (_menuStrip != null)
                {
                    _menuStrip.SetBounds(Cursor.Position.X, Cursor.Position.Y, 0, 0);
                    if (!_menuStrip.Visible)
                        _menuStrip.Show();
                }
                _mouseHeld = true;
            }
            else if (_isResizing || _isMoving || _mouseHeld)
            {
                if (!_contextMenuOpen)
                {
                    if (!_isCameraDestroyed)
                    {
                        Config.Save();
                    }
                }

                _isResizing = false;
                _isMoving = false;
                _mouseHeld = false;
            }
        }
    }
}