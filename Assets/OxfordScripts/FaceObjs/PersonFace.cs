using System;

/// <summary>
/// The person face entity.
/// </summary>
public class PersonFace
{
    /// <summary>
    /// Gets or sets the persisted face identifier.
    /// </summary>
    /// <value>
    /// The persisted face identifier.
    /// </value>
    public Guid PersistedFaceId
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the user data.
    /// </summary>
    /// <value>
    /// The user data.
    /// </value>
    public string UserData
    {
        get; set;
    }
}

