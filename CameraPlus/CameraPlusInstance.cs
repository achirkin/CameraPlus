using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            var gameObj = new GameObject("CameraPlus");
            Instance = gameObj.AddComponent<CameraPlusBehaviour>();
            Instance.Init(Config);
        }
    }
}
