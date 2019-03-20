using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Harmony;
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
        public string Version => "v3.2.5";

        public Action<Scene, Scene> ActiveSceneChanged;

        private HarmonyInstance _harmony;
        
        public void OnApplicationStart()
        {
            if (_init) return;
            _init = true;
            Instance = this;

            _harmony = HarmonyInstance.Create("com.brian91292.beatsaber.cameraplus");
            try
            {
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Plugin.Log("Failed to apply harmony patches!");
            }
            
            // Add our default cameraplus camera
            CameraUtilities.AddNewCamera("cameraplus");

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }

        public void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            SharedCoroutineStarter.instance.StartCoroutine(DelayedActiveSceneChanged(from, to));
        }

        IEnumerator DelayedActiveSceneChanged(Scene from, Scene to)
        {
            yield return new WaitForSeconds(0.5f);

            // If any new cameras have been added to the config folder, render them
            CameraUtilities.ReloadCameras();

            // Invoke each activeSceneChanged event
            foreach (var func in ActiveSceneChanged?.GetInvocationList())
            {
                try
                {
                    func?.DynamicInvoke(from, to);
                }
                catch (Exception ex) { Log($"Exception while invoking ActiveSceneChanged! {ex}"); }
            }
        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;

            _harmony.UnpatchAll("com.brian91292.beatsaber.cameraplus");
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
            // Fix the cursor when the user resizes the main camera to be smaller than the canvas size and they hover over the black portion of the canvas
            if (CameraPlusBehaviour.currentCursor != CameraPlusBehaviour.CursorType.None && !CameraPlusBehaviour.anyInstanceBusy && 
                CameraPlusBehaviour.wasWithinBorder && CameraPlusBehaviour.GetTopmostInstanceAtCursorPos() == null)
            {
                CameraPlusBehaviour.SetCursor(CameraPlusBehaviour.CursorType.None);
                CameraPlusBehaviour.wasWithinBorder = false;
            }
        }

        public static void Log(string msg)
        {
            Console.WriteLine($"[{Plugin.Instance.Name}] {msg}");
        }
    }
}