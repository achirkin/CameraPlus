using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using SimpleJSON;

namespace CameraPlus
{
    public class CinematicCamera : MonoBehaviour
    {
        private CameraPlusBehaviour _cameraPlus;
        private bool dataLoaded = false;
        public string filepath;
        public CameraData data = new CameraData();
        private Vector3 StartPos = Vector3.zero;
        private Vector3 EndPos = Vector3.zero;
        private Vector3 StartRot = Vector3.zero;
        private Vector3 EndRot = Vector3.zero;
        private float movePerc;
        private int eventID;
        private DateTime movementStartTime, movementEndTime, movementDelayEndTime;

        public class Movements
        {
            public Vector3 StartPos;
            public Vector3 StartRot;
            public Vector3 EndPos;
            public Vector3 EndRot;
            public float Duration;
            public float Delay;
        }

        public class CameraData
        {
            public List<Movements> Movements = new List<Movements>();

            public void LoadFromJson(string jsonString)
            {
                Movements.Clear();
                JSONNode node = JSON.Parse(jsonString);

                if (!node["Movements"].IsNull)
                {
                    foreach (JSONObject movement in node["Movements"].AsArray)
                    {
                        Movements newMovement = new Movements();
                        var startPos = movement["StartPos"];
                        var startRot = movement["StartRot"];
                        newMovement.StartPos = new Vector3(startPos["x"].AsFloat, startPos["y"].AsFloat, startPos["z"].AsFloat);
                        newMovement.StartRot = new Vector3(startRot["x"].AsFloat, startRot["y"].AsFloat, startRot["z"].AsFloat);

                        var endPos = movement["EndPos"];
                        var endRot = movement["EndRot"];
                        newMovement.EndPos = new Vector3(endPos["x"].AsFloat, endPos["y"].AsFloat, endPos["z"].AsFloat);
                        newMovement.EndRot = new Vector3(endRot["x"].AsFloat, endRot["y"].AsFloat, endRot["z"].AsFloat);

                        newMovement.Delay = movement["Delay"].AsFloat;
                        newMovement.Duration = Mathf.Clamp(movement["Duration"].AsFloat, 0.01f, float.MaxValue); // Make sure duration is at least 0.01 seconds, to avoid a divide by zero error
                        Movements.Add(newMovement);
                        Plugin.Log("Parsed movement!");
                    }
                }
            }
        }

        private void Update()
        {
            if (dataLoaded)
            {
                if (movePerc == 1 && movementDelayEndTime <= DateTime.Now)
                    UpdatePosAndRot();

                long differenceTicks = (movementEndTime - movementStartTime).Ticks;
                long currentTicks = (DateTime.Now - movementStartTime).Ticks;
                movePerc = Mathf.Clamp((float)currentTicks / (float)differenceTicks, 0, 1);

                _cameraPlus.ThirdPersonPos = Vector3.LerpUnclamped(StartPos, EndPos, Ease(movePerc));
                _cameraPlus.ThirdPersonRot = Vector3.LerpUnclamped(StartRot, EndRot, Ease(movePerc));
            }
        }

        public void Init(CameraPlusBehaviour cameraPlus)
        {
            _cameraPlus = cameraPlus;
            InitLoadCameraData(Path.Combine(Environment.CurrentDirectory, "UserData\\CameraMovementData.json"));
        }

        private void InitLoadCameraData(string path)
        {
            //Making sure File Exists Hopefully in UserData Somewhere
            if (File.Exists(path))
            {
                Console.WriteLine("[CameraPlus] CameraMovementData found at: " + path);
                LoadCameraData(path);
            }
            else
            {
                Console.WriteLine("[CameraPlus] No CameraMovementData found at: " + path + " Destroying Script!");
                Destroy(this, 0);
            }
        }

        private void LoadCameraData(string path)
        {
            string jsonText = File.ReadAllText(path);
            data.LoadFromJson(jsonText);
            Console.WriteLine("[CameraPlus] Populated CameraData");

            if (data.Movements.Count == 0)
            {
                Console.WriteLine("[CameraPlus] No movement data!");
                Destroy(this, 0);
            }
            eventID = 0;
            UpdatePosAndRot();
            dataLoaded = true;

            Console.WriteLine("[CameraPlus] Found " + data.Movements.Count + " entries in: " + path);
        }

        private void UpdatePosAndRot()
        {
            eventID++;
            if (eventID >= data.Movements.Count)
                eventID = 0;

            StartRot = new Vector3(data.Movements[eventID].StartRot.x, data.Movements[eventID].StartRot.y, data.Movements[eventID].StartRot.z);
            StartPos = new Vector3(data.Movements[eventID].StartPos.x, data.Movements[eventID].StartPos.y, data.Movements[eventID].StartPos.z);

            EndRot = new Vector3(data.Movements[eventID].EndRot.x, data.Movements[eventID].EndRot.y, data.Movements[eventID].EndRot.z);
            EndPos = new Vector3(data.Movements[eventID].EndPos.x, data.Movements[eventID].EndPos.y, data.Movements[eventID].EndPos.z);

            movementStartTime = DateTime.Now;
            movementEndTime = movementStartTime.AddSeconds(data.Movements[eventID].Duration);
            movementDelayEndTime = movementStartTime.AddSeconds(data.Movements[eventID].Duration + data.Movements[eventID].Delay);
        }

        private float Ease(float p)
        {
            if (p < 0.5f) //Cubic Hopefully
            {
                return 4 * p * p * p;
            }
            else
            {
                float f = ((2 * p) - 2);
                return 0.5f * f * f * f + 1;
            }
        }
    }
}