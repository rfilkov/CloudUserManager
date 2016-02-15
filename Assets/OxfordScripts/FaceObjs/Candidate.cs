﻿using System;

/// <summary>
/// The identified candidate entity.
/// </summary>
public class Candidate
{
    /// <summary>
    /// Gets or sets the person identifier.
    /// </summary>
    /// <value>
    /// The person identifier.
    /// </value>
    public Guid PersonId
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the confidence.
    /// </summary>
    /// <value>
    /// The confidence.
    /// </value>
    public double Confidence
    {
        get;
        set;
    }

	/// <summary>
	/// Gets or sets the person.
	/// </summary>
	/// <value>The person.</value>
	public Person Person
	{
		get;
		set;
	}
}

