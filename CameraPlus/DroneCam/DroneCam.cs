using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using CameraPlus.DroneCam.Input;

namespace CameraPlus.DroneCam
{
    public class DroneCam : MonoBehaviour
    {
        public Transform smoothingTarget = null;
        public CustomInput customInput = null;
        public DroneMovement droneMovement = null;
        public Transform orbitTarget = null;
        bool inited = false;

        public DroneCam SetupCam(string name)
        {
            if (!inited)
            {
                Plugin.Log("Setting Up DroneCam");
                inited = true;
                GameObject orbitTargetGO = GameObject.Find("OrbitTarget");
                if (orbitTarget == null)
                {
                    orbitTarget = new GameObject("OrbitTarget").transform;
                    DontDestroyOnLoad(orbitTarget);
                } else
                {
                    orbitTarget = orbitTargetGO.transform;
                }
                orbitTarget.position = new Vector3(0, 1.5f, 0);

                GameObject droneCam = new GameObject($"{name}-DroneCam");
                DontDestroyOnLoad(droneCam);
                droneMovement = droneCam.AddComponent<DroneMovement>();
                InputConfig config = new InputConfig($"./UserData/CameraPlus/Input/{name}");
                customInput = droneCam.AddComponent(config.InputType) as CustomInput;
                customInput.droneMovement = droneMovement;
                customInput.Setup(config);
                droneMovement.droneCam = this;
                smoothingTarget = droneCam.transform;
                customInput.droneCam = this;
                droneMovement.cameraPlus = GetComponent<CameraPlusBehaviour>();
            }
            return this;
        }

        public void Update()
        {

        }

        public void LateUpdate()
        {
           
        }
    }
}

