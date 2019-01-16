# CameraPlus
CameraPlus is a Beat Saber mod that allows for multiple wide FOV cameras with smoothed movement, which makes for a much more pleasant overall spectator experience.

[Video Comparison](https://youtu.be/MysLXKSXGTY)  
[Third Person Preview](https://youtu.be/ltIhpt-n6b8)

# Installing
1. Use the modsaber installer: https://github.com/lolPants/modsaber-installer/releases
		It is the easiest method, it will do all these steps below in 1 click.
	
### To install manually:
	1b. Make sure that Beat Saber is not running.
	2b. Extract the contents of the zip into Beat Saber's installation folder.
		For Oculus Home: \Oculus Apps\Software\hyperbolic-magnetism-beat-saber\
		For Steam: \steamapps\common\Beat Saber\
		(The folder that contains Beat Saber.exe)
	3b. Done! You've installed the Custom Avatar Plugin.
# Usage
To edit the settings of any camera in real time, right click on the Beat Saber game window! A context menu will appear with options specific to the camera that you right clicked on!

Press <kbd>F1</kbd> to toggle the main camera between first and third person.

After you run the game once, a `cameraplus.cfg` file is created within the folder Beat Saber\UserData\CameraPlus. Any cfg files located in this folder will be used to render additional cameras.
Edit that file to configure CameraPlus:

| Parameter             | Description                                                                                  |
|-----------------------|----------------------------------------------------------------------------------------------|
| **fov**                   | Horizontal field of view of the camera                                                       |
| **antiAliasing**          | Anti-aliasing setting for the camera (1, 2, 4 or 8 only)                                     |
| **renderScale**           | The resolution scale of the camera relative to game window (similar to supersampling for VR) |
| **positionSmooth**        | How much position should smooth **(SMALLER NUMBER = SMOOTHER)**                              |
| **rotationSmooth**        | How much rotation should smooth **(SMALLER NUMBER = SMOOTHER)**                              |
| **thirdPerson**           | Whether third person camera is enabled                                                       |
| **showThirdPersonCamera** | Whether or not the third person camera is visible                                            |
| **posx**                  | X position of third person camera                                                            |
| **posy**                  | Y position of third person camera                                                            |
| **posz**                  | Z position of third person camera                                                            |
| **angx**                  | X rotation of third person camera                                                            |
| **angy**                  | Y rotation of third person camera                                                            |
| **angz**                  | Z rotation of third person camera                                                            |
| **screenWidth**           | Width of the camera render area                                                              |
| **screenHeight**          | Height of the camera render area                                                             |
| **screenPosX**            | X position of the camera in the Beat Saber window                                            |
| **screenPosY**            | Y position of the camera in the Beat Saber window                                            |
| **layer**                 | Layer to render the camera on **(HIGHER NUMBER = top)**                                      |
| **movementScriptPath**    | Path of the movement script associated with the camera                                       |

If you need help, ask us at the Beat Saber Mod Group Discord Server:  
https://discord.gg/BeatSaberMods
