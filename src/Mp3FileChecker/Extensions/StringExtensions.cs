// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   An extension class for <see cref="string"/>s.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Mp3FileChecker.Extensions;

/// <summary>
/// An extension class for <see cref="string"/>s.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// The empty char constant.
    /// </summary>
    private const char EmptyChar = ' ';

    /// <summary>
    /// Checks whether the text needs trimming or not.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>A value indicating wwhether the text needs trimming or not.</returns>
    public static bool NeedsTrimming(this string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return text.StartsWith(EmptyChar) || text.EndsWith(EmptyChar);
    }
}