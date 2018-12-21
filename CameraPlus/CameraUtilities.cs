using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CameraPlus
{
    public class CameraUtilities
    {
        public static bool CameraExists(string cameraName)
        {
            return Plugin.Instance.Cameras.Keys.Where(c => c == cameraName + ".cfg").Count() > 0;
        }
        
        public static void AddNewCamera(string cameraName, Config CopyConfig = null, bool meme = false)
        {
            string path = Path.Combine(Environment.CurrentDirectory, "UserData\\CameraPlus\\" + cameraName + ".cfg");
            if (!File.Exists(path))
            {
                Config config = null;
                if (CopyConfig != null)
                    File.Copy(CopyConfig.FilePath, path, true);

                config = new Config(path);
                foreach (CameraPlusInstance c in Plugin.Instance.Cameras.Values.OrderBy(i => i.Config.layer))
                {
                    if (c.Config.layer > config.layer)
                        config.layer += (c.Config.layer - config.layer);
                    else if (c.Config.layer == config.layer)
                        config.layer++;
                }
                if (meme)
                {
                    config.screenWidth = (int)Random.Range(200, Screen.width / 1.5f);
                    config.screenHeight = (int)Random.Range(200, Screen.height / 1.5f);
                    config.screenPosX = Random.Range(-200, Screen.width - config.screenWidth + 200);
                    config.screenPosY = Random.Range(-200, Screen.height - config.screenHeight + 200);
                    config.thirdPerson = Random.Range(0, 2) == 0;
                    config.renderScale = Random.Range(0.1f, 1.0f);
                    config.posx += Random.Range(-5, 5);
                    config.posy += Random.Range(-2, 2);
                    config.posz += Random.Range(-5, 5);
                    config.angx = Random.Range(0, 360);
                    config.angy = Random.Range(0, 360);
                    config.angz = Random.Range(0, 360);
                }
                config.Save();
                Plugin.Log($"Success creating new camera \"{cameraName}\"");
            }
            else
            {
                Plugin.Log($"Camera \"{cameraName}\" already exists!");
            }
        }

        public static string GetNextCameraName()
        {
            int index = 1;
            string cameraName = String.Empty;
            while (true)
            {
                cameraName = $"customcamera{index.ToString()}";
                if (!CameraUtilities.CameraExists(cameraName))
                    break;

                index++;
            }
            return cameraName;
        }

        public static bool RemoveCamera(CameraPlusBehaviour instance, bool delete = true)
        {
            try
            {
                if (Plugin.Instance.Cameras.TryRemove(Plugin.Instance.Cameras.Where(c => c.Value.Instance == instance && c.Key != "cameraplus.cfg")?.First().Key, out var removedEntry))
                {
                    if (delete)
                    {
                        if (File.Exists(removedEntry.Config.FilePath))
                            File.Delete(removedEntry.Config.FilePath);
                    }

                    return true;
                }
            }
            catch (Exception)
            {
                Plugin.Log("Can't remove cam!");
            }
            return false;
        }

        public static void ReloadCameras()
        {
            try
            {
                string[] files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "UserData\\CameraPlus"));
                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    if (fileName.EndsWith(".cfg") && !Plugin.Instance.Cameras.ContainsKey(fileName))
                    {
                        Plugin.Log($"Found config {filePath}!");
                        Plugin.Instance.Cameras.TryAdd(fileName, new CameraPlusInstance(filePath));
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log($"Exception while reloading cameras! {e.ToString()}");
            }
        }
        
        public static IEnumerator Spawn38Cameras()
        {
            lock (Plugin.Instance.Cameras)
            {
                for (int i = 0; i < 38; i++)
                {
                    AddNewCamera(GetNextCameraName(), null, true);
                    ReloadCameras();

                    yield return null;
                }
            }
        }
    }
}
