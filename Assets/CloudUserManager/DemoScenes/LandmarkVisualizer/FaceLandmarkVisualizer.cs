using UnityEngine;

public class FaceLandmarkVisualizer : MonoBehaviour
{
    [Tooltip("The landmark visualization prefab.")]
    public GameObject landmarkVisualizationPrefab;

    [Tooltip("Parent image of the visualizer prefabs.")]
    public Transform parentVisualizer;


    private Rect normParentRect;
    private float imageWidth = 1280;
    private float imageHeight = 720;


    /// <summary>
    /// Sets the current image size.
    /// </summary>
    /// <param name="w">Image width</param>
    /// <param name="h">Image height</param>
    public void SetTextureSize(float w, float h)
    {
        imageWidth = w;
        imageHeight = h;
    }

    /// <summary>
    /// Clears the face landmarks.
    /// </summary>
    public void ClearFaceLandmarks()
    {
        EmptyTransformChildren(parentVisualizer);
    }

    /// <summary>
    /// Displays all face landmarks.
    /// </summary>
    /// <param name="landmarks"></param>
    public void VisualizeFaceLandmarks(FaceLandmarks landmarks)
    {
        try
        {
            //EmptyTransformChildren(ParentVisualizer);
            normParentRect = RectTransformToScreenSpace(parentVisualizer as RectTransform);

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
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    // instantiates and sets position of a single face landmark point
    private void PositionOneLandmark(FeatureCoordinate coords)
    {
        if(landmarkVisualizationPrefab != null && parentVisualizer != null && coords != null)
        {
            GameObject landmarkVis = Instantiate(landmarkVisualizationPrefab, parentVisualizer);
            RectTransform rt = landmarkVis ? landmarkVis.GetComponent<RectTransform>() : null;

            if(rt != null)
            {
                rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, NormalizedValue(coords.x, 0, imageWidth, 0, normParentRect.width), rt.rect.width);
                rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, NormalizedValue(coords.y, 0, imageHeight, 0, normParentRect.height), rt.rect.height);
            }
        }
    }

    // removes all currently instantiated face landmarks
    private void EmptyTransformChildren(Transform trans)
    {
        if(trans != null)
        {
            foreach (Transform child in trans)
            {
                Destroy(child.gameObject);
            }
        }
    }

    // normalizes the current value, according to source and destination limits
    private static float NormalizedValue(float current, float sourceMin, float sourceMax, float destinationMin, float destinationMax)
    {
        return current  / (sourceMax - sourceMin) * (destinationMax - destinationMin);
    }

    // converts the rect transform to screen space
    private static Rect RectTransformToScreenSpace(RectTransform transform)
    {
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        return new Rect((Vector2)transform.position - (size * 0.5f), size);
    }
}
