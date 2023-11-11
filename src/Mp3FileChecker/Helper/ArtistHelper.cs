// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArtistHelper.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A helper class for artists.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Mp3FileChecker.Helper;

/// <summary>
/// A helper class for artists.
/// </summary>
public static class ArtistHelper
{
    /// <summary>
    /// The allowed chars to use within the artist name.
    /// </summary>
    private const string allowedArtistChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Checks whether the given artist name is valid or not.
    /// </summary>
    /// <param name="artistName">The artist name.</param>
    /// <returns>A value indicating whether the given artist name is valid or not.</returns>
    public static bool IsValid(string? artistName)
    {
        if (string.IsNullOrWhiteSpace(artistName))
        {
            Log.Warning("The artist name was empty");
            return false;
        }

        if (!artistName.All(allowedArtistChars.Contains))
        {
            Log.Warning("The artist name {ArtistName} contains not allowed characters", artistName);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the artist name from the folder path.
    /// </summary>
    /// <param name="folderPath">The folder path.</param>
    /// <param name="isAlbumFolder">A value indicating whether the folder path contains an album or not.</param>
    /// <returns>The artist name.</returns>
    public static string GetArtistNameFromFolder(string folderPath, bool isAlbumFolder)
    {
        var splitData = folderPath.Split('\\');

        if (splitData.Length < 3)
        {
            Log.Warning("The split data for folder {FolderPath} doesn't have at least length 3", folderPath);
            return string.Empty;
        }

        var artistName = (isAlbumFolder ? splitData.ElementAtOrDefault(splitData.Length - 2) : splitData.LastOrDefault()) ?? string.Empty;

        if (!artistName.Contains('_'))
        {
            return artistName;
        }

        splitData = artistName.Split('_');

        if (splitData.Length != 2)
        {
            Log.Warning("The artist name for {FolderPath} doesn't have length 2", folderPath);
            return string.Empty;
        }

        return $"{splitData[1]} {splitData[0]}";
    }
}
