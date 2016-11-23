using UnityEngine;
using System.IO;
using System.Text;

public static class FaceDetectionUtils 
{
    private static readonly Color[] faceColors = new Color[] { Color.green, Color.yellow, Color.cyan, Color.magenta, Color.red };
    private static readonly string[] faceColorNames = new string[] { "Green", "Yellow", "Cyan", "Magenta", "Red", };
                                      

    public static Texture2D ImportImage()
    {
        Texture2D tex = null;

#if UNITY_EDITOR
		string filePath = UnityEditor.EditorUtility.OpenFilePanel("Open image file", "", "jpg");  // string.Empty; // 
#else
		string filePath = string.Empty;
#endif

        if (!string.IsNullOrEmpty(filePath))
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);

            tex = new Texture2D(2, 2);
            tex.LoadImage(fileBytes);
        }

        return tex;
    }

    public static string FaceToString(Face face, string faceColorName)
    {
        StringBuilder sbResult = new StringBuilder();

        sbResult.Append(string.Format("{0} face:", faceColorName)).AppendLine();
        sbResult.Append(string.Format("  • Gender: {0}", face.faceAttributes.gender)).AppendLine();
		sbResult.Append(string.Format("  • Age: {0}", face.faceAttributes.age)).AppendLine();
		sbResult.Append(string.Format("  • Smile: {0:F0}%", face.faceAttributes.smile * 100f)).AppendLine();

//			sbResult.Append(string.Format("    Beard: {0}", face.FaceAttributes.FacialHair.Beard)).AppendLine();
//			sbResult.Append(string.Format("    Moustache: {0}", face.FaceAttributes.FacialHair.Moustache)).AppendLine();
//			sbResult.Append(string.Format("    Sideburns: {0}", face.FaceAttributes.FacialHair.Sideburns)).AppendLine().AppendLine();

		if(face.emotion != null && face.emotion.scores != null)
			sbResult.Append(string.Format("  • Emotion: {0}", CloudFaceManager.GetEmotionScoresAsString(face.emotion))).AppendLine();

		sbResult.AppendLine();

        return sbResult.ToString();
    }

    public static Color[] FaceColors { get { return faceColors; } }
    public static string[] FaceColorNames { get { return faceColorNames; } }
}
