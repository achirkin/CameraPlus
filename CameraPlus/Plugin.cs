using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CameraPlus
{
    public class Plugin : IPlugin
    {
        public ConcurrentDictionary<string, CameraPlusInstance> Cameras = new ConcurrentDictionary<string, CameraPlusInstance>();
        
        private bool _init;
        public static Plugin Instance { get; private set; }
        public string Name => "CameraPlus";
        public string Version => "v2.1.0b5";
        
        public void OnApplicationStart()
        {
            if (_init) return;
            _init = true;
            Instance = this;

            // Add our default cameraplus camera
            CameraUtilities.AddNewCamera("cameraplus");

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }

        public void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            // If any new cameras have been added to the config folder, render them
            CameraUtilities.ReloadCameras();

            // Trigger our activeSceneChanged event for each camera, because subscribing to the events from within the CameraPlusBehaviour component yields inconsistent results.
            foreach (CameraPlusInstance c in Cameras.Values)
            {
                c.Instance.SceneManager_activeSceneChanged(from, to);
            }
        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public static void Log(string msg)
        {
            Console.WriteLine($"[{Plugin.Instance.Name}] {msg}");
        }
    }
}