using UnityEngine;

public class FaceLandmarkVisualizer : MonoBehaviour
{
    public GameObject LandmarkVisualizationPrefab;
    public Transform ParentVisualizer;

    private Rect NormalizedParentRect;
    private float SourceWidth = 1280;
    private float SourceHeight = 720;

    void Start()
    {
    }

    public void VisualizeFaceLandmarksNow(FaceLandmarks landmarks)
    {
        EmptyTransformChildren(ParentVisualizer);
        NormalizedParentRect = RectTransformToScreenSpace(ParentVisualizer as RectTransform);
        
        PositionOneLandmark(landmarks.mouthLeft);
        PositionOneLandmark(landmarks.mouthRight);
        PositionOneLandmark(landmarks.underLipBottom);
        PositionOneLandmark(landmarks.underLipTop);
        PositionOneLandmark(landmarks.upperLipBottom);
        PositionOneLandmark(landmarks.upperLipTop);

        PositionOneLandmark(landmarks.eyebrowLeftInner);
        PositionOneLandmark(landmarks.eyebrowLeftOuter);
        PositionOneLandmark(landmarks.eyebrowRightInner);
        PositionOneLandmark(landmarks.eyebrowRightOuter);
        PositionOneLandmark(landmarks.eyeLeftBottom);
        PositionOneLandmark(landmarks.eyeLeftInner);
        PositionOneLandmark(landmarks.eyeLeftOuter);
        PositionOneLandmark(landmarks.eyeLeftTop);
        PositionOneLandmark(landmarks.eyeRightBottom);
        PositionOneLandmark(landmarks.eyeRightInner);
        PositionOneLandmark(landmarks.eyeRightOuter);
        PositionOneLandmark(landmarks.eyeRightTop);

        PositionOneLandmark(landmarks.pupilLeft);
        PositionOneLandmark(landmarks.pupilRight);

        PositionOneLandmark(landmarks.noseLeftAlarOutTip);
        PositionOneLandmark(landmarks.noseLeftAlarTop);
        PositionOneLandmark(landmarks.noseRightAlarOutTip);
        PositionOneLandmark(landmarks.noseRightAlarTop);
        PositionOneLandmark(landmarks.noseRootLeft);
        PositionOneLandmark(landmarks.noseRootRight);
        PositionOneLandmark(landmarks.noseTip);
    }

    private void PositionOneLandmark(FeatureCoordinate coords)
    {
        RectTransform rt = Instantiate(LandmarkVisualizationPrefab, ParentVisualizer).GetComponent<RectTransform>();
        rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, NormalizedValue(coords.x, 0, SourceWidth, 0, NormalizedParentRect.width), rt.rect.width);
        rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, NormalizedValue(coords.y, 0, SourceHeight, 0, NormalizedParentRect.height), rt.rect.height);
    }

    private void EmptyTransformChildren(Transform t)
    {
        foreach (Transform child in t)
        {
            Destroy(child.gameObject);
        }
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
