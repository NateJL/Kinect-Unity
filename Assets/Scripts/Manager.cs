using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
/// <summary>
/// Main Scene manager script to handle data assignments and assist
/// communication between scripts.
/// 
/// @author Nathan Larson 12/18/18
/// 
/// </summary>

public class Manager : MonoBehaviour 
{
    public GameObject texturePlane;
    private Renderer textureRenderer;

    public GameObject drawPlane;
    private DrawScript draw_script;

    public List<int> depth_index_list;

    public Color draw_color;

    public ushort maxDepthBound = 868;
    public ushort minDepthBound = 850;


    // Use this for initialization
    void Start () 
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
            }
            catch (Exception e)
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
    private void OnApplicationQuit()
    {
        Kinect.Shutdown();
        Debug.Log("Goodbye.");
    }
}
