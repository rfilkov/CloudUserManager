using System;
using System.Collections.Generic;


public class Scores
{
    /// <summary>
    /// 
    /// </summary>
    public float Anger { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public float Contempt { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public float Disgust { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public float Fear { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public float Happiness { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public float Neutral { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public float Sadness { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public float Surprise { get; set; }

    public override bool Equals(object o)
    {
        if (o == null) return false;

        var other = o as Scores;
        if (other == null) return false;

        return this.Anger == other.Anger &&
            this.Disgust == other.Disgust &&
            this.Fear == other.Fear &&
            this.Happiness == other.Happiness &&
            this.Neutral == other.Neutral &&
            this.Sadness == other.Sadness &&
            this.Surprise == other.Surprise;
    }

    public override int GetHashCode()
    {
        return Anger.GetHashCode() ^
            Disgust.GetHashCode() ^
            Fear.GetHashCode() ^
            Happiness.GetHashCode() ^
            Neutral.GetHashCode() ^
            Sadness.GetHashCode() ^
            Surprise.GetHashCode();
    }
}


