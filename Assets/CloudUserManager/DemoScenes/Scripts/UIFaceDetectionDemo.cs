using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;

public class UIFaceDetectionDemo : MonoBehaviour
{
    [Tooltip("UIWebCam source used for camera shots.")]
    public UIWebcamSource webcamSource;

    [Tooltip("UI RawImage used for camera shot rendering.")]
    public RawImage cameraShot;

	[Tooltip("Whether to recognize the emotions of the detected faces, or not.")]
	public bool recognizeEmotions = false;

//	[Tooltip("Whether to draw rectangles around the detected faces on the picture.")]
//	public bool displayFaceRectangles = true;

	[Tooltip("Whether to draw arrow pointing to the head direction.")]
	public bool displayHeadDirection = false;

	[Tooltip("UI Text used for hints and status messages.")]
    public Text hintText;

    [Tooltip("UI Text used to display result.")]
    public Text resultText;

    // whether webcamSource has been set or there is web camera at all
    private bool hasCamera = false;

    // initial hint message
    private string hintMessage;

    // AspectRatioFitter component;
    private AspectRatioFitter ratioFitter;

    void Start()
    {
        if (cameraShot)
        {
            ratioFitter = cameraShot.GetComponent<AspectRatioFitter>();
        }

        hasCamera = webcamSource && webcamSource.HasCamera;

        hintMessage = hasCamera ? "Click on the camera image to make a shot" : "No camera found";
        
        SetHintText(hintMessage);
    }

    // camera panel onclick event handler
    public void OnCameraClick()
    {
        if (!hasCamera) 
			return;
        
        ClearResultText();

        if (DoCameraShot())
        {
            StartCoroutine(DoFaceDetection());
        }        
    }

    // camera-shot panel onclick event handler
    public void OnShotClick()
    {
        ClearResultText();

        if (DoImageImport())
        {
            StartCoroutine(DoFaceDetection());
        }
    }

    // camera shot step
    private bool DoCameraShot()
    {
        if (cameraShot && webcamSource)
        {
            SetShotImageTexture(webcamSource.GetSnapshot());
            return true;
        }

        return false;
    }

    // imports image and displays it on the camera-shot object
    private bool DoImageImport()
    {
        Texture2D tex = FaceDetectionUtils.ImportImage();
        if (!tex) return false;

        SetShotImageTexture(tex);

        return true;
    }

    // performs face detection
    private IEnumerator DoFaceDetection()
    {
        // get the image to detect
        Face[] faces = null;
        Texture2D texCamShot = null;

        if (cameraShot)
        {
			texCamShot = (Texture2D)cameraShot.texture;
            SetHintText("Wait...");
        }

        // get the face manager instance
		CloudFaceManager faceManager = CloudFaceManager.Instance;

        if (!faceManager)
        {
            SetHintText("Check if the FaceManager component exists in the scene.");
        }
        else if(texCamShot)
        {
			byte[] imageBytes = texCamShot.EncodeToJPG();
			yield return null;

			//faces = faceManager.DetectFaces(texCamShot);
			AsyncTask<Face[]> taskFace = new AsyncTask<Face[]>(() => {
				return faceManager.DetectFaces(imageBytes);
			});

			taskFace.Start();
			yield return null;

			while (taskFace.State == TaskState.Running)
			{
				yield return null;
			}

			if(string.IsNullOrEmpty(taskFace.ErrorMessage))
			{
				faces = taskFace.Result;

				if(faces != null && faces.Length > 0)
				{
					// stick to detected face rectangles
					FaceRectangle[] faceRects = new FaceRectangle[faces.Length];

					for(int i = 0; i < faces.Length; i++)
					{
						faceRects[i] = faces[i].faceRectangle;
					}

					yield return null;

					// get the emotions of the faces
					if(recognizeEmotions)
					{
						//Emotion[] emotions = faceManager.RecognizeEmotions(texCamShot, faceRects);
						AsyncTask<Emotion[]> taskEmot = new AsyncTask<Emotion[]>(() => {
							return faceManager.RecognizeEmotions(imageBytes, faceRects);
						});

						taskEmot.Start();
						yield return null;

						while (taskEmot.State == TaskState.Running)
						{
							yield return null;
						}

						if(string.IsNullOrEmpty(taskEmot.ErrorMessage))
						{
							Emotion[] emotions = taskEmot.Result;
							int matched = faceManager.MatchEmotionsToFaces(ref faces, ref emotions);

							if(matched != faces.Length)
							{
								Debug.Log(string.Format("Matched {0}/{1} emotions to {2} faces.", matched, emotions.Length, faces.Length));
							}
						}
						else
						{
							SetHintText(taskEmot.ErrorMessage);
						}
					}

					CloudFaceManager.DrawFaceRects(texCamShot, faces, FaceDetectionUtils.FaceColors);
					//SetHintText("Click on the camera image to make a shot");
					SetHintText(hintMessage);
					SetResultText(faces);
				}
				else
				{
					SetHintText("No face(s) detected.");
				}
			}
			else
			{
				SetHintText(taskFace.ErrorMessage);
			}
        }

        yield return null;
    }

    // display image on the camera-shot object
    private void SetShotImageTexture(Texture2D tex)
    {        
        if (ratioFitter)
        {
            ratioFitter.aspectRatio = (float)tex.width / (float)tex.height;
        }

        if (cameraShot)
        {
            cameraShot.texture = tex;
        }
    }

    // display results
    private void SetResultText(Face[] faces)
    {
        StringBuilder sbResult = new StringBuilder();

        if (faces != null && faces.Length > 0)
        {
            for (int i = 0; i < faces.Length; i++)
            {
                Face face = faces[i];
                string faceColorName = FaceDetectionUtils.FaceColorNames[i % FaceDetectionUtils.FaceColors.Length];

                string res = FaceDetectionUtils.FaceToString(face, faceColorName);

                sbResult.Append(string.Format("<color={0}>{1}</color>", faceColorName, res));
            }
        }

        string result = sbResult.ToString();

        if (resultText)
        {
            resultText.text = result;
        }
        else
        {
            Debug.Log(result);
        }
    }

    // clear result
    private void ClearResultText()
    {
        if (resultText)
        {
            resultText.text = "";
        }
    }

    // displays hint or status text
    private void SetHintText(string sHintText)
    {
        if (hintText)
        {
            hintText.text = sHintText;
        }
        else
        {
            Debug.Log(sHintText);
        }
    }

}
