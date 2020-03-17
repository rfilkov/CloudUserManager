using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceLandmarkVisualizer : MonoBehaviour
{
   

    public List<FaceLandmarks> faceLandmarks;
    public GameObject LandmarkVisualizationPrefab;
    public Transform ParentVisualizer;
    public Transform VisualizerCopySizeFrom;

    private Rect NormalizedParentRect;

    void Start()
    {
        
    }
    private void SetDestinationSizes()
    {
        ParentVisualizer = VisualizerCopySizeFrom;
        NormalizedParentRect = RectTransformToScreenSpace(ParentVisualizer as RectTransform);

        Debug.Log(NormalizedParentRect);
        DestWidth = NormalizedParentRect.width;
        DestHeight = -1 * NormalizedParentRect.height;
    }
    public void VisualizeFaceNow(FaceLandmarks landmarks)
    {
        faceLandmarks.Add(landmarks);
        SetDestinationSizes();
        EmptyTransformChildren(ParentVisualizer);

        Instantiate(LandmarkVisualizationPrefab, NormalizedPoint(landmarks.mouthLeft.x, landmarks.mouthLeft.y), Quaternion.identity, ParentVisualizer);
        Instantiate(LandmarkVisualizationPrefab, NormalizedPoint(landmarks.mouthRight.x, landmarks.mouthRight.y), Quaternion.identity, ParentVisualizer);
        Instantiate(LandmarkVisualizationPrefab, NormalizedPoint(landmarks.underLipBottom.x, landmarks.underLipBottom.y), Quaternion.identity, ParentVisualizer);
        Instantiate(LandmarkVisualizationPrefab, NormalizedPoint(landmarks.underLipTop.x, landmarks.underLipTop.y), Quaternion.identity, ParentVisualizer);
        Instantiate(LandmarkVisualizationPrefab, NormalizedPoint(landmarks.upperLipBottom.x, landmarks.upperLipBottom.y), Quaternion.identity, ParentVisualizer);
        Instantiate(LandmarkVisualizationPrefab, NormalizedPoint(landmarks.upperLipTop.x, landmarks.upperLipTop.y), Quaternion.identity, ParentVisualizer);
    }
    private void EmptyTransformChildren(Transform t)
    {
        foreach (Transform child in t)
        {
            Destroy(child.gameObject);
        }
    }
    public float SourceWidth = 1280;
    public float SourceHeight = 720;
    public float DestWidth = 435;
    public float DestHeight = -240;
    public Vector3 NormalizedPoint(float sourcex, float sourcey)
    {
        float x = NormalizedValue(sourcex, 0, SourceWidth, 0, NormalizedParentRect.width) + (ParentVisualizer as RectTransform).position.x;
        float y = (ParentVisualizer as RectTransform).position.y - NormalizedValue(sourcey, 0, SourceHeight, 0, NormalizedParentRect.height);
       
        return new Vector3(x, y, 0);
    }

    public static float NormalizedValue(float current, float sourceMin, float sourceMax, float destinationMin, float destinationMax)
    {
        return current  / (sourceMax - sourceMin) * (destinationMax - destinationMin);
    }
    public static Rect RectTransformToScreenSpace(RectTransform transform)
    {
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        return new Rect((Vector2)transform.position - (size * 0.5f), size);
    }


    public void SetTextureSize(float w, float h)
    {
        SourceWidth = w;
        SourceHeight = h;
    }
}
