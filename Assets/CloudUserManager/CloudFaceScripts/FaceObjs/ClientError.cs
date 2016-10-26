using System;
using System.Net;
using System.Runtime.Serialization;

/// <summary>
/// Represents client error with detailed error message and error code
/// </summary>
public class ClientError
{
	/// <summary>
	/// Gets or sets the detailed error message and error code
	/// </summary>
	public ClientExceptionMessage error
	{
		get;
		set;
	}
	
}


/// <summary>
/// Represents detailed error message and error code
/// </summary>
public class ClientExceptionMessage
{
	/// <summary>
	/// Gets or sets the detailed error code
	/// </summary>
	public string code
	{
		get;
		set;
	}
	
	/// <summary>
	/// Gets or sets the detailed error message
	/// </summary>
	public string message
	{
		get;
		set;
	}
}

/// <summary>
/// Represents client error with detailed error message and error code
/// </summary>
public class ServiceError
{
    /// <summary>
    /// Gets or sets the detailed error message and error code
    /// </summary>
	public string statusCode
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the detailed error message and error code
    /// </summary>
	public string message
    {
        get;
        set;
    }
}

