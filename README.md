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











