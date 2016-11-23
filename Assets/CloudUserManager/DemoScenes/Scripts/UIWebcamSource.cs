using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class UIWebcamSource : WebcamSourceBase
{
    protected override void OnApplyTexture(WebCamTexture webcamTex)
    {
        RawImage rawimage = GetComponent<RawImage>();
        if (rawimage)
        {
            rawimage.texture = webcamTex;
            rawimage.material.mainTexture = webcamTex;
        }
    }

    protected override void OnSetAspectRatio(int width, int height)
    {
        AspectRatioFitter ratioFitter = GetComponent<AspectRatioFitter>();
        if (ratioFitter)
        {
            ratioFitter.aspectRatio = (float)width / (float)height;
        }
    }
}
