using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraPlus
{
    public class CameraUtilities
    {
        public static bool CameraExists(string cameraName)
        {
            return Plugin.Instance.Cameras.Keys.Where(c => c == cameraName + ".cfg").Count() > 0;
        }

        public static void AddNewCamera(string cameraName, Config Config = null)
        {
            string path = Environment.CurrentDirectory + "\\UserData\\CameraPlus\\" + cameraName + ".cfg";
            if (!File.Exists(path))
            {
                Config config = null;
                if (Config != null)
                    File.Copy(Config.FilePath, path, true);

                config = new Config(path);
                foreach (CameraPlusInstance c in Plugin.Instance.Cameras.Values.OrderBy(i => i.Config.layer))
                {
                    if (c.Config.layer > config.layer)
                        config.layer += (c.Config.layer - config.layer);
                    else if (c.Config.layer == config.layer)
                        config.layer++;
                }
                config.Save();
            }
        }

        public static bool RemoveCamera(CameraPlusBehaviour instance)
        {
            try
            {
                Plugin.Instance.Cameras.TryRemove(Plugin.Instance.Cameras.Where(c => c.Value.Instance == instance && c.Key != "cameraplus.cfg")?.First().Key, out var removedEntry);
                if (removedEntry != null)
                {
                    File.Delete(removedEntry.Config.FilePath);
                    return true;
                }
            }
            catch (Exception e)
            {
                Plugin.Log("Can't remove cam!");
            }
            return false;
        }

        public static void ReloadCameras()
        {
            try
            {
                string[] files = Directory.GetFiles(Environment.CurrentDirectory + "\\UserData\\CameraPlus");
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
    }
}
