using System.IO;
using UnityEngine;

namespace CameraPlus
{
    public class CameraPlusInstance
    {
        private readonly WaitForSecondsRealtime _waitForSecondsRealtime = new WaitForSecondsRealtime(0.1f);

        public Config Config;
        public CameraPlusBehaviour Instance;

        public CameraPlusInstance(string configPath)
        {
            Config = new Config(configPath);

            var gameObj = new GameObject($"CamPlus_{Path.GetFileName(configPath)}");
            Instance = gameObj.AddComponent<CameraPlusBehaviour>();
            Instance.Init(Config);
        }
    }
}
