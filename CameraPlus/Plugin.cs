using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CameraPlus
{
    public class Plugin : IPlugin
    {
        public Dictionary<string, CameraPlusInstance> Cameras = new Dictionary<string, CameraPlusInstance>();
        
        private bool _init;

        public static Plugin Instance { get; private set; }
        public string Name => "CameraPlus";
        public string Version => "v2.0.1";

        public void OnApplicationStart()
        {
            if (_init) return;
            _init = true;
            Instance = this;

            Cameras.Add("cameraplus.cfg", new CameraPlusInstance(Environment.CurrentDirectory + "\\UserData\\CameraPlus\\cameraplus.cfg"));

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }

        private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            string[] files = Directory.GetFiles(Environment.CurrentDirectory + "\\UserData\\CameraPlus");
            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                if (fileName != "cameraplus.cfg" && fileName.EndsWith(".cfg") && !Cameras.ContainsKey(fileName))
                {
                    Console.WriteLine($"[Camera Plus] Found config {filePath}!");
                    Cameras.Add(fileName, new CameraPlusInstance(filePath));
                }
            }
        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
            foreach (CameraPlusInstance instance in Cameras.Values)
            {
                instance.Config.Save();
            }
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
    }
}