using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Static class to hold all function related to communicating with the c++ plugin that
/// handles all Kinect interactions.
/// 
/// @author Nathan Larson 12/18/18
/// 
/// </summary>
public class Kinect : MonoBehaviour 
{
    public static int frameWidth = 640;
    public static int frameHeight = 480;

    public static Boolean initialized;

    [DllImport("Kinect2Unity")]
    private static extern int GetWidth();                   // get frame width            

    [DllImport("Kinect2Unity")]
    private static extern int GetHeight();                  // get frame height

    [DllImport("Kinect2Unity")]
    public static extern int InitializePlugin();            // initialize the plugin

    [DllImport("Kinect2Unity")]
    public static extern int AllocateMemory();              // allocate memory space for plugin

    [DllImport("Kinect2Unity")]
    public static extern int InitializeDevice();            // initialize the freenect device

    [DllImport("Kinect2Unity")]
    public static extern int StartThreads();               // start the grabber & processor threads

    [DllImport("Kinect2Unity")]
    public static extern int IsFrameReady();                // returns in there is a frame ready or not

    [DllImport("Kinect2Unity")]
    public static extern IntPtr GetReadyFrameByteArray();        // return a byte[] with raw depth values

    [DllImport("Kinect2Unity")]
    private static extern int FreeAllocatedMemory(IntPtr ptr);     // free allocated memory referenced by IntPtr argument

    [DllImport("Kinect2Unity")]
    public static extern int ShutdownDevice();              // safetly shutdown threads and kinect device

    public static Boolean Initialize()
    {
        string output = "Getting Frame Dimensions...";
        frameWidth = GetWidth();
        frameHeight = GetHeight();
        output += "Done.\nWidth: " + frameWidth + "\nHeight: " + frameHeight + "\n";

        output = "Initializing Plugin...";
        if (InitializePlugin() == 1){
            Debug.Log(output + "Sucess.");
        } else {
            Debug.Log(output + "Failed.");
            return false;
        }

        output = "Allocating Memory...";
        if (AllocateMemory() == 1) {
            Debug.Log(output + "Sucess.");
        } else {
            Debug.Log(output + "Failed.");
            return false;
        }

        output = "Initializing Device...";
        if (InitializeDevice() == 1) {
            Debug.Log(output + "Sucess.");
        } else {
            Debug.Log(output + "Failed.");
            return false;
        }

        output = "Starting Threads...";
        if (StartThreads() == 1) {
            Debug.Log(output + "Sucess.");
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
        if (IsFrameReady() == 1)
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
        Marshal.Copy(byte_pointer, returned_byte_array, 0, frameWidth * frameHeight * 2);

        ushort[] frame_16bit = new ushort[frameWidth * frameHeight];
        Buffer.BlockCopy(returned_byte_array, 0, frame_16bit, 0, frameWidth * frameHeight * 2);
        return frame_16bit;
    }

    /*
     * Function to free the memory at the IntPtr parameter address
     */
    public static void FreeMemory(IntPtr ptr)
    {
        if (FreeAllocatedMemory(ptr) != 1)
            Debug.Log("Failed to free memory at: " + ptr.ToString());
    }

    /*
     * Function to call the shutdown function within the plugin to properly 
     * shutdown threads and kinect
     */
    public static void Shutdown()
    {
        if (ShutdownDevice() == 1)
            Debug.Log("Successfully shutdown Device.");
    }
}
