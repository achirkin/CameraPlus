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
        private CameraPlusBehaviour _cameraPlus;

        public CameraPlusInstance(string configPath)
        {
            Config = new Config(configPath);

            SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
        }

        ~CameraPlusInstance()
        {
            SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;
        }

        private void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SharedCoroutineStarter.instance.StartCoroutine(DelayedOnSceneLoaded(scene));
        }

        private IEnumerator DelayedOnSceneLoaded(Scene scene)
        {
            yield return _waitForSecondsRealtime;

            if (scene.buildIndex < 1) yield break;
            
            if (_cameraPlus != null) yield break;

            if (Camera.main == null)
            {
                yield break;
            }

            var gameObj = new GameObject("CameraPlus");
            _cameraPlus = gameObj.AddComponent<CameraPlusBehaviour>();
            _cameraPlus.Init(Config);
        }
    }
}
