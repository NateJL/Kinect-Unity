# Kinect-Unity
Kinect raw depth visualization and interaction using Unity 3D.

## Introduction
My senior capstone project utilizes the Microsoft Kinect V1 device to stream depth data from the camera on the Kinect to the program running on the PC (in this case MAC OS). 
I used the libfreenect C++ library to access the data streamed from the Kinect along with a custom C++ script to read the data, process it, and send it to a C# script running 
in a seperate program built using the Unity 3D developement platform. The depth data was then displayed on the screen with the user being able to "draw" onto the image by 
entering a certain threshold distance from the camera.

## How to Use

### Startup 
To begin running the program, make sure that a Kinect V1 is plugged in to a USB port as well as a power source. 
It is recommended to have at least 2 meters across and 4 meters of space in front of the Kinect to allow for ample movement.

### Interaction
Once the scene has loaded, the "plane" displaying the camera data with the visible directly in the center of the window. At this point the depth is being collected from the 
Kinect and should be updating the texture of the plane in real-time. Based off of the lower and upper bounds for tracking, moving into this range should result in pixels being 
drawn in the overlay texture like "painting". The default paint color is green, although it can be changed to red by pressing the 'R' key or blue by pressing the 'B' key. 
The space bar will clear all current pixels painted on to the overlay texture.

## Documentation

### Plugin 
[Libfreenect Github](https://github.com/OpenKinect/libfreenect) **|**
[OpenKinect Website](https://openkinect.org/wiki/Main_Page)

In order to communicate with the Kinect V1 device, this program uses the OpenKinect communities libfreenect library. The library allows full control of a connected 
Kinect device such as camera mode, tilt angle, and capturing raw camera data.