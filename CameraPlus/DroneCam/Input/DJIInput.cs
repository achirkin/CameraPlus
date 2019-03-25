using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System;

namespace CameraPlus.DroneCam.Input
{
    enum SwitchPosition
    {
        UP,
        CENTER,
        DOWN
    }

    public class DJIInput : CustomInput
    {
        static byte[] initData = { 0x55, 0xaa, 0x55, 0xaa, 0x1e, 0x00, 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x80, 0x00, 0x04, 0x04, 0x74, 0x94, 0x35, 0x00, 0xd8, 0xc0, 0x41, 0x00, 0x30, 0xf6, 0x08, 0x00, 0x00, 0xf6, 0x69, 0x9c, 0x01, 0xe8 };
        static byte[] pingData = { 0x55, 0xaa, 0x55, 0xaa, 0x1e, 0x00, 0x01, 0x00, 0x00, 0x1c, 0x02, 0x00, 0x80, 0x00, 0x06, 0x01, 0x28, 0x97, 0xae, 0x03, 0x28, 0x36, 0xa4, 0x03, 0x28, 0x36, 0xa4, 0x03, 0xab, 0xa7, 0x30, 0x00, 0x03, 0x53 };

        public float deadZone = 0.05f;

        SerialPort serialPort;
        byte[] incomingData = new byte[256];
        float startTime;

        // Start is called before the first frame update
        public override void Setup(InputConfig config)
        {
            startTime = Time.unscaledTime + 1;
            try
            {
                serialPort = new SerialPort(config.comPort, 115200, Parity.None, 8);

                serialPort.Open();
                serialPort.Write(initData, 0, initData.Length);
                Console.WriteLine("[CineCam (DJIInput)] Hello DJI Controller!");
                serialPort.Write(pingData, 0, pingData.Length);
            }
            catch (Exception exc)
            {
                Console.WriteLine("{0}: {1}", exc.GetType().ToString(), exc.Message);
                Console.WriteLine(exc.StackTrace);
            }
        }

        // Update is called once per frame
        public void Update()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Write(pingData, 0, pingData.Length);

                serialPort.Read(incomingData, 0, 256);

                if (incomingData[0] == 0x55 && Time.unscaledTime > startTime)
                { // probably positioning data
                    Vector2 leftStick = Vector2.zero;
                    Vector2 rightStick = Vector2.zero;
                    SwitchPosition leftSwitch = SwitchPosition.UP;
                    SwitchPosition rightSwitch = SwitchPosition.UP;
                    float tiltLever;

                    leftStick.Set(
                        DeadzoneAdjust((float)LittleEndiansToInt(incomingData[43], incomingData[44]) / 1000), // left stick x
                        DeadzoneAdjust((float)LittleEndiansToInt(incomingData[39], incomingData[40]) / 1000)  // left stick y
                    );

                    rightStick.Set(
                        DeadzoneAdjust((float)LittleEndiansToInt(incomingData[31], incomingData[32]) / 1000), // right stick x
                        DeadzoneAdjust((float)LittleEndiansToInt(incomingData[35], incomingData[36]) / 1000)  // right stick y
                    );

                    int rawLeftSwitch = LittleEndiansToInt(incomingData[47], incomingData[48]);
                    int rawRightSwitch = LittleEndiansToInt(incomingData[51], incomingData[52]);

                    if (rawLeftSwitch > 30)
                    {
                        leftSwitch = SwitchPosition.DOWN;
                    }
                    else if (rawLeftSwitch < -30)
                    {
                        leftSwitch = SwitchPosition.UP;
                    }
                    else
                    {
                        leftSwitch = SwitchPosition.CENTER;
                    }

                    if (rawRightSwitch > 30)
                    {
                        rightSwitch = SwitchPosition.DOWN;
                    }
                    else if (rawRightSwitch < -30)
                    {
                        rightSwitch = SwitchPosition.UP;
                    }
                    else
                    {
                        rightSwitch = SwitchPosition.CENTER;
                    }

                    tiltLever = ((float)LittleEndiansToInt(incomingData[55], incomingData[56]) / 1000);

                    if (rightSwitch == SwitchPosition.UP)
                    {
                        droneMovement.translateSpeed = 5f;
                        droneMovement.rotateSpeed = 360;
                    }
                    else if (rightSwitch == SwitchPosition.CENTER)
                    {
                        droneMovement.translateSpeed = 10f;
                        droneMovement.rotateSpeed = 360;
                    }
                    else if (rightSwitch == SwitchPosition.DOWN)
                    {
                        droneMovement.translateSpeed = 50f;
                        droneMovement.rotateSpeed = 720;
                    }

                    MovementProfile movementProfile = MovementProfile.Orbit;

                    if (leftSwitch == SwitchPosition.UP)
                    {
                        movementProfile = MovementProfile.Drone;
                    }
                    else if (leftSwitch == SwitchPosition.CENTER)
                    {
                        movementProfile = MovementProfile.CourseLock;
                    }
                    else if (leftSwitch == SwitchPosition.DOWN)
                    {
                        movementProfile = MovementProfile.Orbit;
                    }

                    droneMovement.Move(movementProfile, new Vector3(rightStick.x, leftStick.y, rightStick.y),
                        new Vector3((tiltLever * 89), leftStick.x, 0), true);

                    //Console.WriteLine("L: {0}, {1} R: {2}, {3}", leftStick.x, leftStick.y, rightStick.x, rightStick.y);
                }
            }
        }

        float DeadzoneAdjust(float input)
        {
            if (Mathf.Abs(input) < deadZone)
            {
                return 0;
            };
            return input;
        }

        // Convert Endians from controller
        static int LittleEndiansToInt(int first, int second)
        {
            if (first < 0)
            {
                first = 256 + first;
            }

            short combined = (short)((second << 8) | first);
            return combined;

        }
    }
}