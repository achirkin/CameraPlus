# v3.1.0 Changes
- New song camera movement script!
   * Same as the old camera movement script, except it gets automatically read from the song directory (if it exists) when a song is played!
   * To use this new script, just right click on the camera in your game window and add a song camera movement script from the `Scripts` menu
   * Mappers, simply add a CameraMovementData.json in the custom song directory, and if the user has a camera with a song movement script attached to it that camera script will be played back when they start the song.

# v3.0.5 Changes
- Switch back to vertical FOV (oops)

# v3.0.3 Changes
- Fixed a bug where some cameras would not be displayed correctly if the width was less than the height
- Now automatically moves old cameraplus.cfg into UserData\CameraPlus if it doesn't already exist

# v3.0.2 Changes
- Automatically fit main camera to canvas
- Added render scale and fit to canvas options in the Layout menu

# v3.0.1 Changes
 - Fix LIV compatibility

# v3.0 Changes
 - Fixed performance issues
 - Added multi-cam support!
    * Right click the game window for a full menu  with the ability to add, remove, and manage your cameras!
 - New CameraMovement script support (created by Emma from the BSMG discord)
    * Allows you to create custom scripted movement paths for third person cameras, to create cool cinematic effects!
