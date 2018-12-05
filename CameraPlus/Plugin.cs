using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public string Version => "v2.1.0b4";
        
        public void OnApplicationStart()
        {
            if (_init) return;
            _init = true;
            Instance = this;

            AddNewCamera("cameraplus");

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }
        
        public void ReloadCameras()
        {
            try
            {
                string[] files = Directory.GetFiles(Environment.CurrentDirectory + "\\UserData\\CameraPlus");
                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    if (fileName.EndsWith(".cfg") && !Cameras.ContainsKey(fileName))
                    {
                        Console.WriteLine($"[Camera Plus] Found config {filePath}!");
                        Cameras.TryAdd(fileName, new CameraPlusInstance(filePath));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while reloading cameras! {e.ToString()}");
            }
        }

        public void AddNewCamera(string cameraName)
        {
            string path = Environment.CurrentDirectory + "\\UserData\\CameraPlus\\" + cameraName + ".cfg";
            if (!File.Exists(path))
                new Config(path);
        }

        public void RemoveCamera(CameraPlusBehaviour instance)
        {
            Plugin.Instance.Cameras.TryRemove(Plugin.Instance.Cameras.Where(c => c.Value.Instance == instance)?.First().Key, out var removedEntry);
            if (removedEntry != null)
                File.Delete(removedEntry.Config.FilePath);
        }

        public bool CameraExists(string cameraName)
        {
            return Cameras.Keys.Where(c => c == cameraName + ".cfg").Count() > 0;
        }

        public void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            Console.WriteLine($"[Camera Plus] Active scene changed from \"{from.name}\" to \"{to.name}\"");

            try
            {
                ReloadCameras();

                // Trigger our activeSceneChanged event for each camera, because subscribing to the events from within the CameraPlusBehaviour component yields inconsistent results.
                foreach (CameraPlusInstance c in Cameras.Values)
                {
                    c.Instance.SceneManager_activeSceneChanged(from, to);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("[Camera Plus] Exception in OnActiveSceneChanged!");
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