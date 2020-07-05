# CameraPlusCamera

This is an ugly adaptation of an unofficial BeatSaber plugin [CameraPlus](https://github.com/Snow1226/CameraPlus),
an adaptation that merges a video stream from a depth camera with the in-game camera picture.
If you want the original cameraPlus plugin, please use the link above.

### real-salient
This project requires another library from the [real-salient](https://github.com/achirkin/real-salient) project.
real-salient implements a GPU algorithm to extract a salient object (*you*) from the background in a RGB-D video stream.
The example is currently hardcoded to use [Intel RealSense Depth Camera D415](https://www.intelrealsense.com/depth-camera-d415/).

### Usage

It's an experimental code, so everything is hardcoded. You would need:

  1. Intel RealSense Depth Camera D415 (or any other RGB-D camera and to adapt the `real-salient` project)
  2. [Vive tracker](https://www.vive.com/eu/accessory/vive-tracker/) for the camera
       (or any other tracker and to adapt the `real-salient` project)
  3. Compile the `real-salient` project example library called `saber-salient` and copy `saber-salient.dll` to
       the Beat Saber folder `***\Beat Saber\Beat Saber_Data\Plugins`
  4. Compile this project and copy the resulting `CameraPlus.dll` to `***\Beat Saber\Plugins`
  5. Modify camera setting similar to the [example.configs](https://github.com/achirkin/CameraPlus/tree/master/example.configs) in
      the folder `***\Beat Saber\UserData\CameraPlus`.
      There, the two important settings are
        `useSaberSalient` (enable the camera video stream) and
        `useSaberOpaque` (show the camera video stream without the game video stream).

#### Demo videos:

- [360° Reason for Living by Morgan Page](https://youtu.be/1GdDrsxVWYE)
- [360° First of the Year (Equinox) by Skrillex](https://youtu.be/0zMn-zVGNNc)
