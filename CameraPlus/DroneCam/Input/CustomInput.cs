using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraPlus.DroneCam.Input
{
    abstract public class CustomInput : MonoBehaviour
    {
        public DroneMovement droneMovement = null;
        public DroneCam droneCam = null;

        public abstract void Setup(InputConfig config);
        public abstract void CleanUp();
    }
}