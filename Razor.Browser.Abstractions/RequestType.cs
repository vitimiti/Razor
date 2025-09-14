// -----------------------------------------------------------------------
// <copyright file="RequestType.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Browser.Abstractions;

/// <summary>Represents the type of HTTP request that can be performed.</summary>
public enum RequestType
{
    /// <summary>Represents an HTTP GET request type, used to retrieve data from a specified resource.</summary>
    Get,

    /// <summary>Represents an HTTP POST request type, used to send data to a server to create or update a resource.</summary>
    Post,

    /// <summary>Represents an HTTP PUT request type, used to update or create a resource at a specified URI.</summary>
    Put,

    /// <summary>Represents an HTTP DELETE request type, used to delete a specified resource from the server.</summary>
    Delete,
}
