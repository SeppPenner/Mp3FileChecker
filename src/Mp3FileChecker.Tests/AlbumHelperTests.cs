// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AlbumHelperTests.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class to test the <see cref="AlbumHelper" /> class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Mp3FileChecker.Tests;

/// <summary>
/// A class to test the <see cref="AlbumHelper"/> class.
/// </summary>
[TestClass]
public class AlbumHelperTests
{
    /// <summary>
    /// Checks whether an album name of letters and digits is accepted.
    /// </summary>
    [TestMethod]
    public void IsValidAcceptsLettersAndDigits()
    {
        Assert.IsTrue(AlbumHelper.IsValid("GreatestHits"));
        Assert.IsTrue(AlbumHelper.IsValid("Album2"));
    }

    /// <summary>
    /// Checks whether an empty album name is rejected.
    /// </summary>
    [TestMethod]
    public void IsValidRejectsAnEmptyAlbumName()
    {
        Assert.IsFalse(AlbumHelper.IsValid(null));
        Assert.IsFalse(AlbumHelper.IsValid(string.Empty));
        Assert.IsFalse(AlbumHelper.IsValid("   "));
    }

    /// <summary>
    /// Checks whether an album name with a character outside of the allowed set is rejected. The space is not
    /// part of that set, that is existing behaviour, see the known quirks.
    /// </summary>
    [TestMethod]
    public void IsValidRejectsCharactersOutsideOfTheAllowedSet()
    {
        Assert.IsFalse(AlbumHelper.IsValid("Greatest Hits"));
        Assert.IsFalse(AlbumHelper.IsValid("Vol.2"));
    }

    /// <summary>
    /// Checks whether the last part of the path is used as the album name.
    /// </summary>
    [TestMethod]
    public void GetAlbumNameFromFolderUsesTheLastPartOfThePath()
    {
        Assert.AreEqual("GreatestHits", AlbumHelper.GetAlbumNameFromFolder(@"D:\Music\Rock\Queen\GreatestHits"));
    }

    /// <summary>
    /// Checks whether an empty path yields an empty album name instead of a null reference.
    /// </summary>
    [TestMethod]
    public void GetAlbumNameFromFolderReturnsAnEmptyNameForAnEmptyPath()
    {
        Assert.AreEqual(string.Empty, AlbumHelper.GetAlbumNameFromFolder(string.Empty));
    }
}
