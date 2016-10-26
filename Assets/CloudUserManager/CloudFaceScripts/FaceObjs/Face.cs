using System;


/// <summary>
/// The detected face entity.
/// </summary>
public class Face
{
    /// <summary>
    /// Gets or sets the face identifier.
    /// </summary>
    /// <value>
    /// The face identifier.
    /// </value>
    public Guid FaceId
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the face rectangle.
    /// </summary>
    /// <value>
    /// The face rectangle.
    /// </value>
    public FaceRectangle FaceRectangle
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the face landmarks.
    /// </summary>
    /// <value>
    /// The face landmarks.
    /// </value>
    public FaceLandmarks FaceLandmarks
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the face attributes.
    /// </summary>
    /// <value>
    /// The face attributes.
    /// </value>
    public FaceAttributes FaceAttributes
    {
        get;
        set;
    }

	/// <summary>
	/// Gets or sets the emotion.
	/// </summary>
	/// <value>The emotion.</value>
	public Emotion Emotion
	{
		get;
		set;
	}

	/// <summary>
	/// Gets or sets the identified candidate.
	/// </summary>
	/// <value>The identified candidate.</value>
	public Candidate Candidate
	{
		get;
		set;
	}
}
