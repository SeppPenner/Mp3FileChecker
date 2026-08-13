// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class to test the <see cref="StringExtensions" /> class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Mp3FileChecker.Tests;

/// <summary>
/// A class to test the <see cref="StringExtensions"/> class.
/// </summary>
[TestClass]
public class StringExtensionsTests
{
    /// <summary>
    /// Checks whether a leading or a trailing space is found.
    /// </summary>
    [TestMethod]
    public void NeedsTrimmingFindsALeadingOrTrailingSpace()
    {
        Assert.IsTrue(" Yesterday".NeedsTrimming());
        Assert.IsTrue("Yesterday ".NeedsTrimming());
        Assert.IsTrue(" Yesterday ".NeedsTrimming());
    }

    /// <summary>
    /// Checks whether a text without a leading or trailing space is left alone.
    /// </summary>
    [TestMethod]
    public void NeedsTrimmingAcceptsATrimmedText()
    {
        Assert.IsFalse("Yesterday".NeedsTrimming());
        Assert.IsFalse("Hey Jude".NeedsTrimming());
    }

    /// <summary>
    /// Checks whether an empty text is reported as not needing trimming. The tag of an MP3 file returns null
    /// for a value that is not set, and a text of spaces only would be emptied by trimming it, which is the
    /// job of the check for the missing value instead.
    /// </summary>
    [TestMethod]
    public void NeedsTrimmingAcceptsAnEmptyText()
    {
        Assert.IsFalse(((string?)null).NeedsTrimming());
        Assert.IsFalse(string.Empty.NeedsTrimming());
        Assert.IsFalse("   ".NeedsTrimming());
    }

    /// <summary>
    /// Checks whether a tab or a line break is not treated as a leading space, only the space itself is.
    /// </summary>
    [TestMethod]
    public void NeedsTrimmingOnlyLooksAtTheSpace()
    {
        Assert.IsFalse("\tYesterday".NeedsTrimming());
    }
}
