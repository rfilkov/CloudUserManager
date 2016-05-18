using System;


public class Emotion
{
    /// <summary>
    /// Gets or sets the face rectangle.
    /// </summary>
    /// <value>
    /// The face rectangle.
    /// </value>
    public FaceRectangle FaceRectangle { get; set; }

    /// <summary>
    /// Gets or sets the emotion scores.
    /// </summary>
    /// <value>
    /// The emotion scores.
    /// </value>
    public Scores Scores { get; set; }

    public override bool Equals(object o)
    {
        if (o == null) return false;

        var other = o as Emotion;

        if (other == null) return false;

        if (this.FaceRectangle == null)
        {
            if (other.FaceRectangle != null) return false;
        }
        else
        {
            if (!this.FaceRectangle.Equals(other.FaceRectangle)) return false;
        }

        if (this.Scores == null)
        {
            return other.Scores == null;
        }
        else
        {
            return this.Scores.Equals(other.Scores);
        }
    }

    public override int GetHashCode()
    {
        int r = (FaceRectangle == null) ? 0x33333333 : FaceRectangle.GetHashCode();
        int s = (Scores == null) ? 0xccccccc : Scores.GetHashCode();
        return r ^ s;
    }
}


