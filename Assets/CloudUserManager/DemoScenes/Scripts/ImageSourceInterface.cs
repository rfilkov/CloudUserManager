using UnityEngine;
using System.Collections;

/// <summary>
/// This interface has to be implemented by all image sources, used for face, emotion or user recognition.
/// </summary>
public interface ImageSourceInterface
{
	/// <summary>
	/// Gets the image as texture2d.
	/// </summary>
	/// <returns>The image.</returns>
	Texture2D GetImage();

	/// <summary>
	/// Gets the transform.
	/// </summary>
	/// <returns>The transform.</returns>
	Transform GetTransform();
}


