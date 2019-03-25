using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XInputDotNetPure;

namespace CameraPlus.DroneCam.Input
{
    class XInput : CustomInput
    {
        MovementProfile movementProfile = MovementProfile.Drone;
        PlayerIndex player = PlayerIndex.One;

        public override void Setup(InputConfig config)
        {
            droneMovement.translateSpeed = 2;
            droneMovement.rotateSpeed = 270;

            player = config.Player;

            GamePadState padState = GamePad.GetState(player);

            Plugin.Log(string.Format("XInput controller is {0}.", padState.IsConnected ? "connected" : "disconnected"));
        }

        void Update()
        {
            GamePadState padState = GamePad.GetState(PlayerIndex.One);

            if (padState.Buttons.A == ButtonState.Pressed)
            {
                movementProfile = MovementProfile.Drone;
            }
            if (padState.Buttons.B == ButtonState.Pressed)
            {
                movementProfile = MovementProfile.FixedWing;
            }
            if (padState.Buttons.X == ButtonState.Pressed)
            {
                movementProfile = MovementProfile.CourseLock;
            }
            if (padState.Buttons.Y == ButtonState.Pressed)
            {
                movementProfile = MovementProfile.Orbit;
            }

            if (padState.DPad.Up == ButtonState.Pressed)
            {
                droneMovement.translateSpeed += 0.25f;
            }
            if (padState.DPad.Down == ButtonState.Pressed)
            {
                droneMovement.translateSpeed -= 0.25f;
            }
            if (padState.DPad.Right == ButtonState.Pressed)
            {
                droneMovement.rotateSpeed += 5;
            }
            if (padState.DPad.Left == ButtonState.Pressed)
            {
                droneMovement.rotateSpeed -= 5;
            }

            droneMovement.Move(movementProfile, new Vector3(padState.ThumbSticks.Left.X, padState.Triggers.Right - padState.Triggers.Left, padState.ThumbSticks.Left.Y),
                        new Vector3(-padState.ThumbSticks.Right.Y / 2, padState.ThumbSticks.Right.X / 2, 0));
        }

        public override void CleanUp() {
            Plugin.Log($"Goodbye player {(int)player + 1}!");
        }
    }
}
