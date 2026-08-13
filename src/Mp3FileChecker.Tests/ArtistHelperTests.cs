// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArtistHelperTests.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class to test the <see cref="ArtistHelper" /> class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Mp3FileChecker.Tests;

/// <summary>
/// A class to test the <see cref="ArtistHelper"/> class.
/// </summary>
[TestClass]
public class ArtistHelperTests
{
    /// <summary>
    /// Checks whether an artist name of letters and digits is accepted.
    /// </summary>
    [TestMethod]
    public void IsValidAcceptsLettersAndDigits()
    {
        Assert.IsTrue(ArtistHelper.IsValid("Queen"));
        Assert.IsTrue(ArtistHelper.IsValid("Blink182"));
    }

    /// <summary>
    /// Checks whether an empty artist name is rejected.
    /// </summary>
    [TestMethod]
    public void IsValidRejectsAnEmptyArtistName()
    {
        Assert.IsFalse(ArtistHelper.IsValid(null));
        Assert.IsFalse(ArtistHelper.IsValid(string.Empty));
        Assert.IsFalse(ArtistHelper.IsValid("   "));
    }

    /// <summary>
    /// Checks whether an artist name with a character outside of the allowed set is rejected. The space is
    /// not part of that set, which is why the name built from an artist folder with an underscore can never
    /// be valid. That is existing behaviour, see the known quirks.
    /// </summary>
    [TestMethod]
    public void IsValidRejectsCharactersOutsideOfTheAllowedSet()
    {
        Assert.IsFalse(ArtistHelper.IsValid("AC/DC"));
        Assert.IsFalse(ArtistHelper.IsValid("The Beatles"));
    }

    /// <summary>
    /// Checks whether the last part of the path is used as the artist name for a folder without an album.
    /// </summary>
    [TestMethod]
    public void GetArtistNameFromFolderUsesTheLastPartForAnArtistFolder()
    {
        Assert.AreEqual("Queen", ArtistHelper.GetArtistNameFromFolder(@"D:\Music\Rock\Queen", false));
    }

    /// <summary>
    /// Checks whether the part before the last one is used as the artist name for an album folder.
    /// </summary>
    [TestMethod]
    public void GetArtistNameFromFolderUsesThePartBeforeTheLastOneForAnAlbumFolder()
    {
        Assert.AreEqual("Queen", ArtistHelper.GetArtistNameFromFolder(@"D:\Music\Rock\Queen\GreatestHits", true));
    }

    /// <summary>
    /// Checks whether a folder name with an underscore is turned around, which is the convention for artists
    /// that are named with a last name and a first name.
    /// </summary>
    [TestMethod]
    public void GetArtistNameFromFolderTurnsTheUnderscoreNameAround()
    {
        Assert.AreEqual("The Beatles", ArtistHelper.GetArtistNameFromFolder(@"D:\Music\Rock\Beatles_The", false));
    }

    /// <summary>
    /// Checks whether a folder name with more than one underscore is rejected instead of being cut apart.
    /// </summary>
    [TestMethod]
    public void GetArtistNameFromFolderRejectsMoreThanOneUnderscore()
    {
        Assert.AreEqual(string.Empty, ArtistHelper.GetArtistNameFromFolder(@"D:\Music\Rock\Beatles_The_Best", false));
    }

    /// <summary>
    /// Checks whether a path with less than three parts is rejected. The path is split on the backslash
    /// literally, so a relative path or a path with forward slashes ends up here as well.
    /// </summary>
    [TestMethod]
    public void GetArtistNameFromFolderRejectsAShortPath()
    {
        Assert.AreEqual(string.Empty, ArtistHelper.GetArtistNameFromFolder(@"D:\Queen", false));
        Assert.AreEqual(string.Empty, ArtistHelper.GetArtistNameFromFolder("D:/Music/Rock/Queen", false));
    }
}
