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

### Display
To display the depth data returned from the Kinect, there is a plane object that acts as the "window" to draw to. Attached to this plane is a custom rendering script that 
dynamically creates and modifies the texture at runtime. The depth data is drawn to the texture in a RGBA32 texture format with 8-bits per channel (red, green, blue, alpha) 
and a resolution of 640x480 to match the resolution of the Kinect depth camera. As the depth data is read into the texture, the values are analyzed to check if any pixels are 
within the drawing threshold, if so they are added to a list of indices to be used to draw pixels on to the overlay texture.

## Documentation

### Plugin 
[Libfreenect Github](https://github.com/OpenKinect/libfreenect) **|**
[OpenKinect Website](https://openkinect.org/wiki/Main_Page)

In order to communicate with the Kinect V1 device, this program uses the OpenKinect communities libfreenect library. The library allows full control of a connected 
Kinect device such as camera mode, tilt angle, and capturing raw camera data.

### Data Structures and Variables
```
#define WIDTH 640
#define HEIGHT 480
#define NUM_FRAMES 3
#define TRUE 1
#define FALSE 0
											
// Struct for each node containing a frame.
struct Frame {
	int free;
	uint16_t *depth_16bit;
	pthread_mutex_t frame_lock;
} ;

// Struct for a running thread 
struct ThreadInfo {
	pthread_t thread;
	int current_index;
} ;

ThreadInfo grabber_thread;			
ThreadInfo processor_thread;			

Frame frame_list[NUM_FRAMES];		

int *current_frame;
uint16_t *ready_16bit_frame;
uint16_t *depth_mid_16bit;
int frame_ready;

uint16_t t_gamma[2048];
int running;
int frame_count;

pthread_mutex_t log_lock = PTHREAD_MUTEX_INITIALIZER;
pthread_mutex_t ready_frame_lock = PTHREAD_MUTEX_INITIALIZER;
pthread_mutex_t next_frame_lock = PTHREAD_MUTEX_INITIALIZER;

freenect_context *f_ctx;
freenect_device *f_dev;
int freenect_angle = 0;
int freenect_led;
freenect_video_format current_format = FREENECT_VIDEO_RGB;
int got_depth = 0;
int die = 0;
```

### Functions
---
#### `GetWidth()` and `GetHeight()`
The `GetWidth()` and `GetHeight()` functions simply return the width and height of the dimensions the depth data is being recorded in (ie. resolution).
```
/*
 * Function to be called from main program to return width of image.
 */
extern "C" int EXPORT_API GetWidth()
{
	return WIDTH;
} ;

/*
 * Function to be called from main program to return height of image.
 */
extern "C" int EXPORT_API GetHeight()
{
	return HEIGHT;
} ;
```
---
#### `InitializePlugin()`
The `InitializePlugin()` function is called from the main program at the start of its execution. It first tries to open a ".txt" file to act as a log for program processes. 
The function then initializes the global values of the plugin as well as the indices of both the grabber and processor thread.
```
/*
 * Function to initialize plugin.
 */
extern "C" int EXPORT_API InitializePlugin()
{
	try {
		time_t now = time(0);
		char* dt = ctime(&now);
		FILE* fp = fopen("LogFile.txt, "w");
		fprintf(fp, dt);
		fclose(fp);
	} catch (const char* msg) {
		return 0;
	}
	
	die = FALSE;
	running = FALSE;
	frame_ready = FALSE;
	frame_count = 0;
	grabber_thread.current_index = 0;
	processor_thread.current_index = 0;
	int i;
	for (i=0; i<2048; i++)
	{
		float v = i/2048.0f;
		v = powfv, 3)* 6;
		t_gamma[i] = static_cast<uint16_t>(v*6*256);
	}
	
	return 1;
} ;
```
---
#### `InitializeDevice()`
The `InitializeDevice()` function is called from the main program to attempt and establish a connection with the Kinect device. It first attempts to initialize the 
connection, followed by detecting the number of devices connected, and finishes by attempting to open the device to communication. All of the steps are 
logged in the log file and will return either `TRUE` or `FALSE` to signal success or failure to establish a connection.
```
/*
 * Function.
 */
extern "C" int EXPORT_API InitializeDevice()
{
	writeToLog("-(Freenect): Initializing Kinect...\n");
	if(freenect_init(&f_ctx, NULL) < 0)
	{
		writeToLog("-(Freenect): freenect_init() failed.\n");
		return 0;
	} else {
		writeToLog("-(Freenect): freenect_init() success!\n");
	}
	freenect_set_log_level(f_ctx, FREENECT_LOG_DEBUG);
	
	int nr_devices = freenect_num_devices (f_ctx);
	char initLog[64];
	snprintf(initLog, sizeof(initLog),"-(Freenect): Number of devices found: %d\n", nr_devices);
	writeToLog(initLog);
	int user_device_number = 0;
	if(nr_devices < 1)
		return 0;
	if (freenect_open_device(f_ctx, &f_dev, user_device_number) < 0)
	{
		writeToLog("-(Freenect): Could not open device\n");
		return 0;
	} else {
		writeToLog("-(Freenect): Successfully opened device\n");
	}
	return 1;
} ;
```
---
#### `AllocateMemory()`
At the initialization of the program, the `AllocateMemory()` function is called from the C# script to allocate memory for the 3 frames stored at any point during runtime.
```
extern "C" int EXPORT_API AllocateMemory()
{
	try {
		for(int i = 0; i < NUM_FRAMES; i++)
		{
			frame_list[i].depth_16bit = (uint16_t*)malloc(WIDTH*HEIGHT*sizeof(uint16_t));
			frame_list[i].frame_lock = PTHREAD_MUTEX_INITIALIZER;
			frame_list[i].free = TRUE;
		}
		current_frame = new int[WIDTH*HEIGHT];
		ready_16bit_frame = new uint16_t[WIDTH*HEIGHT];
		depth_mid_16bit = (uint16_t*)malloc(WIDTH*HEIGHT*sizeof(uint16_t)
	}
} ;
```
---
#### `FreeAllocatedMemory()`
The `FreeAllocatedMemory()` function is called from the main program to attempt and free up the memory associated with the pointer passed to function. 
If it fails it will log the failure in the log file as well as return `FALSE`, otherwise it will return `TRUE`.
```
/*
 * Function to free allocated memory for return pointer from main program.
 */
extern "C" int EXPORT_API FreeAllocatedMemory(int *arrayPtr)
{
	try {
		delete[] arrayPtr;
		return 1;
	} catch (const char* msg) {
		writeToLog("-Memory Deallocation Failed.\n");
		return 0;
	}
} ;
```
---
#### `StartThreads()`
The `StartThreads()` function simply attempts to create the grabber and processor threads and logs the result (Success or Failure). I used the **pthread** library to 
implement the multithreaded aspect of the program due to my familiarity with the library and its built in mutex lock system. Using a unique lock for each frame 
helped to avoid any potential race conditions, as well as locks for the log file output, ready frame data, and next frame data. It will return `TRUE` if both threads 
are created successfully, otherwise will return `FALSE`.
```
/*
 * Function to attempt and create the grabber and processor threads.
 */
extern "C" int EXPORT_API StartThreads()
{
	int err;
	err = pthread_create(&grabber_thread.thread, NULL, grabberThreadFunc, NULL);	// try to start grabber thread
	if(err) {
		writeToLog("-(Grabber Thread): pthread_create Failed.\n");
		return 0;
	} else {
		writeToLog("-(Grabber Thread): pthread_create Success.\n");
	}
	err = pthread_create(&processor_thread.thread, NULL, processorThreadFunc, NULL);	// try to start processing thread
	pthread_mutex_lock(&log_lock);
	if(err) {
		writeToLog("-(Process Thread): pthread_create Failed.\n");
		return 0;
	} else {
		writeToLog("-(Process Thread): pthread_create Success.\n");
	}
	pthread_mutex_unlock(&log_lock);
	return 1;
} ;
```
---
#### `depth_cb()`
The `depth_cb()` function is set as a callback function to receive depth data streaming from the Kinect through the libfreenect library. It is called whenever a new frame is 
collected by the device, and if it has not gotten a new frame since processing the previous one then it will lock the `next_frame_lock mutex`, collect the data, then release 
the mutex lock.
```
/*
 * Callback function for depth data from Kinect device.
 */
void depth_cb(freenect_device *dev, void *v_depth, uint32_t timestamp)
{
	if(got_depth == 0)
	{
		pthread_mutex_lock(&next_frame_lock);
		uint16_t *depth = (uint16_t*)v_depth;
		for(int i = 0; i < WIDTH*HEIGHT; i++)
		{
			depth_mid_16bit[i] = depth[i];
		}
		got_depth++;
		frame_count++;
		pthread_mutex_lock(&next_frame_lock);
	}
}
```
---
#### `IsFrameReady()`
The `IsFrameReady()` function is called by the main program to check if there is an available frame to grab. If this function 
returns `TRUE` then the main program will attempt to call the `GetReadyFrameByteArray()` function.
```
/*
 * Function.
 */
extern "C" int EXPORT_API IsFrameReady()
{
	return frame_ready;
}
```
---
#### `GetReadyFrameByteArray()`
The `GetReadyFrameByteArray()` function is called outside of the plugin script by the main program to return the current ready frame. Due to the low-level communication 
between the plugin and the main program, the array of 16-bit values must be converted to an array of 8-bit (1 byte) values. The function does this by taking one element 
from the 16-bit array and storing it in 2 adjacent array elements in the 8-bit array, ie:

`16bit_arr[0] = 0101001011101001`
will be returned as:

`8bit_arr[0] = 01010010`

`8bit_arr[1] = 11101001`
```
/*
 * Returns a pointer array of byte values,
 * length is (width*height)*2 since we are returning an 8-bit array storing 16-bit values.
 */
extern "C" uint16_t * EXPORT_API GetReadyFrameByteArray()
{
	uint8_t *temp = new  uint8_t[WIDTH*HEIGHT*2];
	pthread_mutex_lock(&ready_frame_lock);
	int index_8bit = 0;
	int index_16bit = 0;
	while(index_16bit < WIDTH*HEIGHT)
	{
		temp[index_8bit++] = ready_16bit_frame[index_16bit] & 0xff;
		temp[index_8bit++] = (ready_16bit_frame[index_16bit++] >> 8);
	}
	frame_ready = FALSE;
	pthread_mutex_unlock($ready_frame_lock);
	return temp;
}
```
---
#### `*grabberThreadFunc()`
The `*grabberThreadFunc()` function is used as the main function for the grabber thread to operate. It runs continuously from start, checking if there is a new frame 
available from the Kinect. If a new frame is received then it locks the mutex for the current frame index, copies the new frame to that element, and then releases the lock. 
it then increments the index and waits for another new frame to be received.
```
/*
 * Main function for the grabber thread  to handle getting
 * depth data from Kinect.
 */
void *grabberThreadFunc(void *arg)
{
	int grabber_frame_count = 0;
	pthread_mutex_lock(&log_lock);
	writeToLog("-(Grabber Thread): Started.\n");
	pthread_mutex_unlock(&log_lock);
    
	freenect_set_tilt_degs(f_dev,freenect_angle);
	freenect_set_led(f_dev,LED_RED);
	freenect_set_depth_callback(f_dev, depth_cb);
	freenect_set_depth_mode(f_dev, freenect_find_depth_mode(FREENECT_RESOLUTION_MEDIUM, FREENECT_DEPTH_11BIT));
	
	freenect_start_depth(f_dev);
	
	while(!die && freenect_process_events(f_ctx) >= 0)
	{
		if(grabber_thread.current_index >= NUM_FRAMES)      // if index out of range go back to 0
		grabber_thread.current_index = 0;
        
		pthread_mutex_lock(&frame_list[grabber_thread.current_index].frame_lock);   // acquire the current index frame lock
		if(frame_list[grabber_thread.current_index].free && got_depth)                           // if the lock is free:
		{
			pthread_mutex_lock(&next_frame_lock);
			for(int i = 0; i < WIDTH*HEIGHT; i++)
			{
				frame_list[grabber_thread.current_index].depth_16bit[i] = depth_mid_16bit[i];
			}
			pthread_mutex_unlock(&next_frame_lock);
            
			frame_list[grabber_thread.current_index].free = FALSE;                          // frame is no longer free
			pthread_mutex_unlock(&frame_list[grabber_thread.current_index].frame_lock);

			grabber_thread.current_index++;
			grabber_frame_count++;
			got_depth = 0;
		} else {
			pthread_mutex_unlock(&frame_list[grabber_thread.current_index].frame_lock);
		}
        
		running=TRUE;
	}
	running = FALSE;

	freenect_stop_depth(f_dev);
	freenect_close_device(f_dev);
	freenect_shutdown(f_ctx);
    
	pthread_mutex_lock(&log_lock);
	writeToLog("-(Grabber Thread): Ended.\n");
	pthread_mutex_unlock(&log_lock);
	pthread_exit(NULL);
}
```
---
#### `*processorThreadFunc()`
The `*processorThreadFunc()` function is used as the main function for the processor thread to operate. It runs continuously alongside the grabber thread waiting to 
process any new frames added by the grabber thread. If the current frame is not the same as the previously processed frame, it will attempt to lock the mutex for that 
frame and check if it is free to process. It then locks the mutex for the current ready frame (that will be read by the main program), copies the current frame to the ready 
frame, then releases the ready frame mutex and waits to process the next frame.
```
/*
 * Main function for the processor thread that handles getting the
 * raw depth data ready for Unity
 */
void *processorThreadFunc(void *arg)
{
	int processor_frame_count = 0;
	while(!running) {}              // wait for signal from grabber to start
	pthread_mutex_lock(&log_lock);
	writeToLog("-(Process Thread): Started.\n");
	pthread_mutex_unlocklock(&log_lock);
    
	int last = -1;
	while(running)          // While the grabber thread is still running
	{
		if(last != processor_thread.current_index)      // if we are not processing the same frame
		{
			if(processor_thread.current_index >= NUM_FRAMES)
				processor_thread.current_index = 0;
            
			pthread_mutex_lock(&frame_list[processor_thread.current_index].frame_lock);     // acquire current frame lock
			if(!frame_list[processor_thread.current_index].free)                            // if the frame is not free:
			{
                
//==================================================== Processing Section ==========================================
				pthread_mutex_lock(&ready_frame_lock);          // acquire lock for current ready frame
				frame_ready = FALSE;                            // frame is not ready
				for(int i = 0; i < WIDTH*HEIGHT; i++)
				{
					current_frame[i] = (int) frame_list[processor_thread.current_index].depth_16bit[i];
					ready_16bit_frame[i] = frame_list[processor_thread.current_index].depth_16bit[i];
				}
				frame_ready = TRUE;
				pthread_mutex_unlock(&ready_frame_lock);  // release lock for current ready frame
//==================================================================================================================
                
				frame_list[processor_thread.current_index].free = TRUE;                     // free the current frame
                
				pthread_mutex_unlock(&frame_list[processor_thread.current_index].frame_lock);   // release current frame lock
				last = processor_thread.current_index;                                          // set last to current index
				processor_thread.current_index++;                                               // increment index
				processor_frame_count++;
			} else {
				pthread_mutex_unlock(&frame_list[processor_thread.current_index].frame_lock);
			}
		}
	}
    
	pthread_mutex_lock(&log_lock);
	writeToLog("-(Process Thread): Ended.\n");
	pthread_mutex_unlock(&log_lock);
	pthread_exit(NULL);
}
```
---
#### `ShutdownDevice()`
The `ShutdownDevice()` function is used by the main program to safely termiate the active threads as well as shut down the Kinect device.
```
/*
 * Function to be called from the main program to properly end threads and shut down device.
 */
extern "C" int EXPORT_API ShutdownDevice()
{
	if(!die {
		die = TRUE;
		return 1;
	} else {
		return 0;
	}
}
```
---
#### `writeToLog()`
The `writeToLog()` functions are simply called when a process wants to write a message to the log file. There are two functions with different 
parameters to allow for different variable types to be written to the log.
```
/*
 * Functions to write input to log file.
 */
void writeToLog(char logEntry[])
{
	FILE* fp = fopen("LogFile.txt, "a");
		fprintf(fp, logEntry);
		fclose(fp);
}
void writeToLog(const char* logEntry)
{
	FILE* fp = fopen("LogFile.txt, "a");
		fprintf(fp, logEntry);
		fclose(fp);
}
```

### Unity Scripts
---
#### `Kinect.cs`
This script acts as an object to communicate with the C++ plugin from the Unity side. All of the external functions imported from the .dll plugin file are located here, as 
well as handling the initialization and shutdown of the Kinect device. From the initialization function the script manually allocates memory, establishes a connection with the 
Kinect, and starts the threads for the plugin. Since nearly all other scripts in the environment need information from the Kinect in order to operate, it is a static class 
thus does not need an active instance to operate.
```
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
/// <summary>
/// Static class to hold all function related to communicating with the c++ plugin that
/// handles all Kinect interactions.
/// @author Nathan Larson 12/18/18
/// 
/// </summary>
public class Kinect : MonoBehavior
{
	public static int frameWidth = 640;
	public static int frameHeight = 480;
	
	public static Boolean initialized;
	
	[DllImport("Kinect2Unity")]
	private static extern int GetWidth();		// get frame width
	
	[DllImport("Kinect2Unity")]
	private static extern int GetHeight();		// get frame height
	
	[DllImport("Kinect2Unity")]
	private static extern int InitializePlugin();		// initialize the plugin
	
	[DllImport("Kinect2Unity")]
	private static extern int AllocateMemory();		// allocated memory space for plugin
	
	[DllImport("Kinect2Unity")]
	private static extern int InitializeDevice();		// initialize the freenect device
	
	[DllImport("Kinect2Unity")]
	private static extern int StartThreads();		// start the grabber and processor threads
	
	[DllImport("Kinect2Unity")]
	private static extern int IsFrameReady();		// returns if there is a frame ready or not
	
	[DllImport("Kinect2Unity")]
	private static extern int GetReadyFrameByteArray();		// return a byte[] with raw depth values
	
	[DllImport("Kinect2Unity")]
	private static extern int FreeAllocatedMemory(IntPtr ptr);		// free allocated memory referenced by IntrPtr argument
	
	[DllImport("Kinect2Unity")]
	private static extern int ShutdownDevice();		// safely shutdown threads and Kinect device
	
	
	public static Boolean Initialize()
	{
		string output = "Getting Frame Dimensions...";
		frameWidth = GetWidth();
		frameHeight = GetHeight();
		output += "Done.\nWidth: " + frameWidth + "\nHeight: " + frameHeight + "\n";
		
		output = "Initializing Plugin...";
		if(InitializePlugin() == 1){
			Debug.Log(output + "Success.");
		} else {
			Debug.Log(output + "Failed.");
			return false;
		}
		
		output = "Allocating Memory...";
		if(AllocateMemory() == 1){
			Debug.Log(output + "Success.");
		} else {
			Debug.Log(output + "Failed.");
			return false;
		}
		
		output = "Initializing Device...";
		if(InitializeDevice() == 1){
			Debug.Log(output + "Success.");
		} else {
			Debug.Log(output + "Failed.");
			return false;
		}
		
		output = "Starting Threads...";
		if(StartThreads() == 1){
			Debug.Log(output + "Success.");
		} else {
			Debug.Log(output + "Failed.");
			return false;
		}
		return true;
	}
	
	/*
	 * Boolean wrapper for plugin frame ready check
	 */
	public static Boolean FrameReady()
	{
		if(IsFrameReady() == 1)
			return true;
		else
			return false;
	}
	
	/*
	 * Return a ushort array representation of the original frame,
	 * returned from the plugin as a byte[] array.
	 */
	public static ushort[] Get16BitArray(IntPtr byte_pointer)
	{
		byte[] returned_byte_array = new byte[frameWidth * frameHeight * 2];
		Marshall.Copy(byte_pointer, returned_byte_array, 0, frameWidth * frameHeight * 2);
		
		ushort[] frame_16bit = new ushort[frameWidth * frameHeight];
		Buffer.BlockCopy(returned_byte_array, 0, frame_16bit, 0, frameWidth * frameHeight * 2);
		return frame_16bit;
	}
	
	/*
	 * Function to free the memory at the IntPtr parameters address
	 */
	public static void FreeMemory(IntPtr ptr)
	{
		if(FreeAllocatedMemory(ptr) != 1)
			Debug.Log("Failed to free memory at: " + ptr.ToString());
	}
	
	/*
	 * Function to call the shutdown function within the plugin to properly shutdown threads and Kinect
	 */
	public static void ShutDown()
	{
		if(ShutdownDevice() == 1)
			Debug.Log("Successfully shutdown Device. ");
	}
}
```
---
#### `Manager.cs`
The manager acts as the main "driver" for the scene, being the only script that contains an update function that is called every frame in order to maintain 
straightforward synchronization. At the start of the programs execution, this script will do some minor checks to make sure it is ready to run, then call the kinect script 
Initialize function to begin setting up the Kinect device.
```
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
/// <summary>
/// Main Scene manager script to handle data assignments and assist
/// communication between scripts.
/// @author Nathan Larson 12/18/18
/// 
/// </summary>
public class Manager : MonoBehavior
{
	public GameObject texturePlane;
	public Renderer textureRenderer;
	
	public GameObject drawPlane;
	private DrawScript draw_script;
	
	public List<int> depth_index_list;
	
	public Color draw_color;
	
	public ushort maxDepthBound = 868;
	public ushort minDepthBound = 850;
	
	// Use this for initialization
	void Start()
	{
		Debug.Log("Starting Manager...");
		depth_index_list = new List<int>();
		draw_color = new Color(0, 1, 0, 1);
		textureRenderer = texturePlane.GetComponent<Renderer>();
		draw_script = drawPlane.GetComponent<DrawScript>();
		
		Kinect.initialized = false;
		Kinect.initialized = Kinect.Initialize();
		Debug.Log("Kinect Initialized: " + Kinect.initialized);
	}
	
	// Update is called once per frame
	void Update()
	{
		if (Kinect.FrameReady() && Kinect.initialized)
		{
			try
			{
				IntPtr byte_pointer = Kinect.GetReadyFrameByteArray();

				ushort[] newFrame = Kinect.Get16BitArray(byte_pointer);

				texturePlane.GetComponent<RenderScript>().SetTexturePixels16bit(newFrame);
				drawPlane.GetComponent<DrawScript>().CheckPixels();
				Kinect.FreeMemory(byte_pointer);
			} catch (Exception e)
			{
				Debug.Log("Failed: " + e);
			}
		}

		if (Input.GetKeyUp(KeyCode.Space)){
			draw_script.ClearAllTexturePixels();
		}
		else if (Input.GetKeyUp(KeyCode.R)) {
			draw_color = Color.red;
		}
		else if (Input.GetKeyUp(KeyCode.G)){
			draw_color = Color.green;
		}
		else if (Input.GetKeyUp(KeyCode.B)){
			draw_color = Color.blue;
		}
	}
	
	/* 
	 * Called when the application is exited
	 */
	void OnApplicationQuit()
	{
		Kinect.ShutDown();
		Debug.Log("Goodbye.");
	}
}
```
---
#### `DrawScript.cs`
This script handles the "drawing" input from the user based off of their distance/depth from the camera and whether or not the distance is within the threshold. 
If the depth value at a certain point is within the threshold, the selected color will be drawn on the overlay texture by this script.
```
using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Script attached to the overlay texture used for drawing pixels to the texture
/// based off of the users depth values and bounds.
/// @author Nathan Larson 12/18/18
/// 
/// </summary>
public class DrawScript : MonoBehavior
{
	public Manager manager;

	public Texture2D texture;
	private TextureFormat format;

	// Use this for initialization
	void Start () 
	{
		format = TextureFormat.RGBA32;
		texture = new Texture2D(Kinect.frameWidth, Kinect.frameHeight, format, false);
		GetComponent<Renderer>().material.mainTexture = texture;
		ClearAllTexturePixels();
	}
	
	/*
	 * Clear all the pixels in the texture and set them to transparent
	 */
	 public void ClearAllTexturePixels()
	 {
		var data = texture.GetRawTextureData<Color32>();

		for (int i = 0; i < data.Length; i++)
			data[i] = new Color32(0, 0, 0, 0);

		texture.Apply();
	}

	/*
	 * get the list of indexes where the depth data values are within the threshold
	 */
	public void CheckPixels()
	{
		var data = texture.GetRawTextureData<Color32>();
		int[] index_list = manager.depth_index_list.ToArray();
		for (int i = 0; i < index_list.Length; i++)
		{
			data[index_list[i]] = manager.draw_color;
		}
		texture.Apply();
	}
}
```
---
#### `RenderScript.cs`
This script handles the rendering of the depth data that is received from the Kinect. The data is altered slighlty to give a better image, then converted to a RGBA value 
using the bytes stored in the given array. A darker image means the distance from the camera is greater, while a lighter color means the distance is smaller.
```
using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Script to handle the texture and rendering of the depth data taken from
/// the Kinect.
/// @author Nathan Larson 12/18/18
/// 
/// </summary>
public class RenderScript : MonoBehavior
{
	public Manager manager;

	public float MAX_16BIT_VALUE = 65536;
	public float MAX_11BIT_VALUE = 2048;

	public Texture2D texture;
	private TextureFormat format;

	public GameObject drawPlane;

	/*
	 * Initialize the texture and format
	 */
	void Start()
	{
		format = TextureFormat.RGBA32;
		texture = new Texture2D(Kinect.frameWidth, Kinect.frameHeight, format, false);
		GetComponent<Renderer>().material.mainTexture = texture;

		SetAllTexturePixels(0);
	}
	
	/*
	 * Function to set the ushort[] array input to the textures pixel array
	 * @param ushort[]
	 */
	public void SetTexturePixels16bit(ushort[] newPixels)
	{
		manager.depth_index_list.Clear();
		var data = texture.GetRawTextureData<Color32>();
		for (int i = 0; i < data.Length; i++)
		{
			byte value = (byte)newPixels[i];
			if(newPixels[i] > manager.minDepthBound && newPixels[i] < manager.maxDepthBound)
			{
				manager.depth_index_list.Add(i);
			}

			Color32 new_color;
			new_color = new Color32(value, value, value, 1);
			data[i] = new_color;
		}
		texture.Apply();
	}

	/*
	 * Return array of Colors representing each pixel in the texture.
	 */
	public Color[] GetColors()
	{
		return texture.GetPixels();
	}
	
	/*
	 * Set the color of all pixels from given byte[] array.
	 */
	public void SetTexturePixels8bit(byte[] newPixels)
	{
		var data = texture.GetRawTextureData<Color32>();
		for (int i = 0; i < data.Length; i++)
		{
			Color32 new_color = new Color32(newPixels[3*i+0], newPixels[3*i+1], newPixels[3*i+2], 255);
			data[i] = new_color;
		}
		texture.Apply();
	}

	/*
	 * Set all pixel colors to the same value from the given byte.
	 */
	public void SetAllTexturePixels(byte one_value)
	{
		var data = texture.GetRawTextureData<Color32>();

		for (int i = 0; i < data.Length; i++)
			data[i] = new Color32(one_value, one_value, one_value, 0);

		texture.Apply();
	}
}
```
---
#### `UIManager.cs`
This script is not vital to the operation of the program itself, it acts as more of a utility to allow easier modification and displaying of data on the screen. 
It calculates an approximate frames per second to display on the screen, as well as allowing the user to alter the min and max threshold values without halting the execution.
```
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Script to handle displaying and updating the UI values such as FPS and min/max distance threshold.
/// @author Nathan Larson 12/18/18
/// 
/// </summary>
public class UIManager : MonoBehavior
{
	public Manager manager;

	public Canvas mainCanvas;
	public GameObject framerateText;
	public GameObject drawColorBox;

	public InputField minDepthBoundField;
	public InputField maxDepthBoundField;

	// Use this for initialization
	void Start () 
	{
		mainCanvas.renderMode = RenderMode.WorldSpace;
		mainCanvas.transform.position.Set(mainCanvas.transform.position.x, mainCanvas.transform.position.y, -1);
		framerateText.GetComponent<Text>().text = "FPS: null";
		drawColorBox.GetComponent<Image>().color = Color.black;

		minDepthBoundField.contentType = InputField.ContentType.IntegerNumber;
		minDepthBoundField.characterLimit = 4;
		minDepthBoundField.text = manager.minDepthBound.ToString();
		maxDepthBoundField.contentType = InputField.ContentType.IntegerNumber;
		maxDepthBoundField.characterLimit = 4;
		maxDepthBoundField.text = manager.maxDepthBound.ToString();
	}
	
	// Update is called once per frame
	void Update () 
	{
		framerateText.GetComponent<Text>().text = "FPS: " + (1.0f / Time.smoothDeltaTime).ToString();
		drawColorBox.GetComponent<Image>().color = manager.draw_color;
	}

	/*
	 * callback function for lower depth bound input field
	 */
	public void setLowerDepthBound()
	{
		manager.minDepthBound = ushort.Parse(minDepthBoundField.text);
		Debug.Log("Min: " + manager.minDepthBound);
	}

	/*
	 * callback function for upper depth bound input field
	 */
	public void setUpperDepthBound()
	{
		manager.maxDepthBound = ushort.Parse(maxDepthBoundField.text);
		Debug.Log("Max: " + manager.maxDepthBound);
	}
}
```
