﻿using System;

/// <summary>
/// The person entity.
/// </summary>
public class Person
{
    /// <summary>
    /// Gets or sets the person identifier.
    /// </summary>
    /// <value>
    /// The person identifier.
    /// </value>
    public Guid PersonId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the persisted face ids.
    /// </summary>
    /// <value>
    /// The persisted face ids.
    /// </value>
    public Guid[] PersistedFaceIds
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name of the person.
    /// </value>
    public string Name
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the profile.
    /// </summary>
    /// <value>
    /// The profile.
    /// </value>
    public string UserData
    {
        get; set;
    }
}

