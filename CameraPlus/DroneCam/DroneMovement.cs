using CameraPlus;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CameraPlus.DroneCam.Input;

namespace CameraPlus.DroneCam
{
    public enum MovementProfile
    {
        Drone,
        FixedWing,
        CourseLock,
        Orbit
    }

    public class DroneMovement : MonoBehaviour
    {
        public DroneCam droneCam = null;
        public CameraPlusBehaviour cameraPlus = null;

        public float translateSpeed = 1;
        public float rotateSpeed = 270;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame

        public void Move(MovementProfile movementProfile, Vector3 translate, Vector3 rotateFirst, bool fixedXRotate = false)
        {
            Vector3 rotate = rotateFirst;
            if (fixedXRotate)
            {
                rotate = new Vector3(0, rotate.y, rotate.z);
            }
            switch (movementProfile)
            {
                case MovementProfile.Drone:
                    MoveLikeDrone(translate, rotate);
                    break;
                case MovementProfile.FixedWing:
                    MoveLikeFixedWing(translate, rotate);
                    break;
                case MovementProfile.CourseLock:
                    MoveCourseLock(translate, rotate);
                    break;
                case MovementProfile.Orbit:
                    MoveOrbit(translate, droneCam.orbitTarget);
                    break;
            }

            if (fixedXRotate && movementProfile != MovementProfile.Orbit)
            {
                transform.eulerAngles = new Vector3(rotateFirst.x, transform.eulerAngles.y, transform.eulerAngles.z);
            } else
            {
                float clampedX = transform.eulerAngles.x;
                if (clampedX >= 90 && clampedX <= 270)
                {
                    if (clampedX >= 90)
                    {
                        clampedX = 90;
                    }
                    else
                    {
                        clampedX = 270;
                    }
                }

                transform.eulerAngles = new Vector3(clampedX, transform.eulerAngles.y, transform.eulerAngles.z);
            }
        }

        void MoveLikeDrone(Vector3 translate, Vector3 rotateFirst)
        {
            translate = translate * translateSpeed * Time.unscaledDeltaTime;
            Vector3 rotate = rotateFirst * rotateSpeed * Time.unscaledDeltaTime;
            Vector3 rot = transform.eulerAngles;
            transform.eulerAngles = new Vector3(0, rot.y, 0);
            transform.Translate(translate, Space.Self);
            transform.eulerAngles = rot + rotate;
        }

        void MoveLikeFixedWing(Vector3 translateFirst, Vector3 rotateFirst)
        {
            Vector3 translate = translateFirst * translateSpeed * Time.unscaledDeltaTime;
            Vector3 rotate = rotateFirst * rotateSpeed * Time.unscaledDeltaTime;
            transform.eulerAngles += rotate;
            transform.Translate(translate, Space.Self);
        }

        void MoveCourseLock(Vector3 translateFirst, Vector3 rotateFirst)
        {
            Vector3 translate = translateFirst * translateSpeed * Time.unscaledDeltaTime;
            Vector3 rotate = rotateFirst * rotateSpeed * Time.unscaledDeltaTime;
            transform.eulerAngles += rotate;
            transform.Translate(translate, Space.World);
        }

        void MoveOrbit(Vector3 translate, Transform target)
        {
            transform.LookAt(target);
            cameraPlus.LookAt(target);
            Vector3 compensatedTranslate = translate * translateSpeed * Time.unscaledDeltaTime;
            transform.Translate(compensatedTranslate.x, compensatedTranslate.z, compensatedTranslate.y, Space.Self);
        }
    }
}
