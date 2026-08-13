// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectExtensionsTests.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class to test the <see cref="ObjectExtensions" /> class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Mp3FileChecker.Tests;

/// <summary>
/// A class to test the <see cref="ObjectExtensions"/> class.
/// </summary>
[TestClass]
public class ObjectExtensionsTests
{
    /// <summary>
    /// Checks whether a null reference is reported as empty. The tag of an MP3 file returns null instead of
    /// an empty array for a value that is not set.
    /// </summary>
    [TestMethod]
    public void IsEmptyOrNullAcceptsANullReference()
    {
        Assert.IsTrue(((IEnumerable<string>?)null).IsEmptyOrNull());
    }

    /// <summary>
    /// Checks whether an enumerable without elements is reported as empty.
    /// </summary>
    [TestMethod]
    public void IsEmptyOrNullFindsAnEmptyEnumerable()
    {
        Assert.IsTrue(Array.Empty<string>().IsEmptyOrNull());
        Assert.IsTrue(new List<string>().IsEmptyOrNull());
    }

    /// <summary>
    /// Checks whether an enumerable with elements is not reported as empty.
    /// </summary>
    [TestMethod]
    public void IsEmptyOrNullFindsAFilledEnumerable()
    {
        Assert.IsFalse(new[] { "Queen" }.IsEmptyOrNull());
        Assert.IsFalse(new[] { string.Empty }.IsEmptyOrNull());
    }
}
