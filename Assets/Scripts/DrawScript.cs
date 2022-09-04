using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Script attached to the overlay texture used for drawing pixels to the texture
/// based off of the users depth values and bounds.
/// @author Nathan Larson 12/18/18
/// 
/// </summary>

public class DrawScript : MonoBehaviour 
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
