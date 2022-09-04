using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour 
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
