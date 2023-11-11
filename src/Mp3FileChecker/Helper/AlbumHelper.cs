// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AlbumHelper.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A helper class for albums.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Mp3FileChecker.Helper;

/// <summary>
/// A helper class for albums.
/// </summary>
public static class AlbumHelper
{
    /// <summary>
    /// The allowed chars to use within the album name.
    /// </summary>
    private const string allowedAlbumChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Checks whether the given album name is valid or not.
    /// </summary>
    /// <param name="albumName">The album name.</param>
    /// <returns>A value indicating whether the given album name is valid or not.</returns>
    public static bool IsValid(string? albumName)
    {
        if (string.IsNullOrWhiteSpace(albumName))
        {
            Log.Warning("The album name was empty");
            return false;
        }

        if (!albumName.All(allowedAlbumChars.Contains))
        {
            Log.Warning("The album name {AlbumName} contains not allowed characters", albumName);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the album name from the folder path.
    /// </summary>
    /// <param name="folderPath">The folder path.</param>
    /// <returns>The album name.</returns>
    public static string GetAlbumNameFromFolder(string folderPath)
    {
        return folderPath.Split('\\').LastOrDefault() ?? string.Empty;
    }
}
