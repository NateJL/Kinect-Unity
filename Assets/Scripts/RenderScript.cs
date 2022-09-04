using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script to handle the texture and rendering of the depth data taken from
/// the Kinect.
/// 
/// @author Nathan Larson 12/18/18
/// 
/// </summary>

public class RenderScript : MonoBehaviour 
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

    public Color[] GetColors()
    {
        return texture.GetPixels();
    }

    /*
     * 
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
     * 
     */
    public void SetAllTexturePixels(byte one_value)
    {
        var data = texture.GetRawTextureData<Color32>();

        for (int i = 0; i < data.Length; i++)
            data[i] = new Color32(one_value, one_value, one_value, 0);
            
        texture.Apply();
    }

}
