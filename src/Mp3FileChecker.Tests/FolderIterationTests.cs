// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FolderIterationTests.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class to test the folder walk of the <see cref="Program" /> class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Mp3FileChecker.Tests;

/// <summary>
/// A class to test the folder walk of the <see cref="Program"/> class. The walk decides by the folder
/// depth alone whether a folder is an artist folder or an album folder, so the depth is what is tested
/// here. No MP3 files are involved, the folders are empty or hold a text file.
/// </summary>
[TestClass]
public class FolderIterationTests
{
    /// <summary>
    /// The message template of the finding that a folder of the first two levels holds files.
    /// </summary>
    private const string FilesInFolderTemplate = "There shouldn't be any files in folder {Folder}, but found some files {@Files}";

    /// <summary>
    /// The message template of the finding that an artist folder holds files that are no MP3 files.
    /// </summary>
    private const string InvalidFilesTemplate = "There are some invalid files {@Files} in the folder {FolderPath}";

    /// <summary>
    /// The message template of the finding that an album folder name contains not allowed characters.
    /// </summary>
    private const string InvalidAlbumNameTemplate = "The album name {AlbumName} contains not allowed characters";

    /// <summary>
    /// The message template of the finding that a folder does not exist.
    /// </summary>
    private const string MissingFolderTemplate = "The folder path was empty or not found: {FolderPath}";

    /// <summary>
    /// The sink that holds the findings of the running test.
    /// </summary>
    private LogCollector logCollector = new();

    /// <summary>
    /// The music folder of the running test, it is the folder of depth 0.
    /// </summary>
    private string testDirectory = string.Empty;

    /// <summary>
    /// Creates an empty directory outside of the repository and redirects the log into memory.
    /// </summary>
    [TestInitialize]
    public void CreateTestDirectory()
    {
        this.testDirectory = Path.Combine(Path.GetTempPath(), $"Mp3FileChecker_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.testDirectory);
        this.logCollector = new LogCollector();
        Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(this.logCollector).CreateLogger();
    }

    /// <summary>
    /// Removes the directory of the finished test.
    /// </summary>
    [TestCleanup]
    public void DeleteTestDirectory()
    {
        Log.CloseAndFlush();

        if (Directory.Exists(this.testDirectory))
        {
            Directory.Delete(this.testDirectory, true);
        }
    }

    /// <summary>
    /// Checks whether every folder of the second level is reported for the files in it. The depth used to be
    /// handed to the recursion with a post increment, which gave the first sub folder the depth of its parent
    /// and every following sibling a higher one, so the result depended on the number of sibling folders.
    /// </summary>
    [TestMethod]
    public void IterateFolderReportsTheFilesOfEverySecondLevelFolder()
    {
        this.CreateFolderWithFile("Rock");
        this.CreateFolderWithFile("Pop");
        this.CreateFolderWithFile("Jazz");

        Program.IterateFolder(this.testDirectory, 0);

        var findings = this.logCollector.GetEvents(LogEventLevel.Error, FilesInFolderTemplate);
        Assert.AreEqual(3, findings.Count, "Every folder of the second level holds a file and has to be reported.");
    }

    /// <summary>
    /// Checks whether the files of the music folder itself are still checked against the depth of that folder
    /// after its sub folders have been walked. The post increment used to leave the depth of the parent folder
    /// raised by the number of its sub folders.
    /// </summary>
    [TestMethod]
    public void IterateFolderKeepsTheDepthOfTheMusicFolderAfterWalkingItsSubFolders()
    {
        Directory.CreateDirectory(Path.Combine(this.testDirectory, "Rock"));
        Directory.CreateDirectory(Path.Combine(this.testDirectory, "Pop"));
        File.WriteAllText(Path.Combine(this.testDirectory, "stray.txt"), string.Empty);

        Program.IterateFolder(this.testDirectory, 0);

        var findings = this.logCollector.GetEvents(LogEventLevel.Error, FilesInFolderTemplate);
        Assert.AreEqual(1, findings.Count, "The music folder holds a file and has to be reported.");
        Assert.AreEqual(this.testDirectory, LogCollector.GetPropertyValue(findings[0], "Folder"));
    }

    /// <summary>
    /// Checks whether the third level is treated as an artist folder, where a file that is no MP3 file is a
    /// warning instead of the error the first two levels report.
    /// </summary>
    [TestMethod]
    public void IterateFolderTreatsTheThirdLevelAsAnArtistFolder()
    {
        var artistFolder = Path.Combine(this.testDirectory, "Rock", "Queen");
        Directory.CreateDirectory(artistFolder);
        File.WriteAllText(Path.Combine(artistFolder, "cover.jpg"), string.Empty);

        Program.IterateFolder(this.testDirectory, 0);

        Assert.AreEqual(1, this.logCollector.GetEvents(LogEventLevel.Warning, InvalidFilesTemplate).Count);
        Assert.AreEqual(0, this.logCollector.GetEvents(LogEventLevel.Error, FilesInFolderTemplate).Count);
    }

    /// <summary>
    /// Checks whether an album folder name that contains not allowed characters is reported. The album check
    /// used to validate the artist name a second time, so the album folder name was never looked at.
    /// </summary>
    [TestMethod]
    public void IterateFolderReportsAnInvalidAlbumFolderName()
    {
        Directory.CreateDirectory(Path.Combine(this.testDirectory, "Rock", "Queen", "Greatest Hits"));

        Program.IterateFolder(this.testDirectory, 0);

        var findings = this.logCollector.GetEvents(LogEventLevel.Warning, InvalidAlbumNameTemplate);
        Assert.AreEqual(1, findings.Count, "The album folder name holds a space, which is not an allowed character.");
        Assert.AreEqual("Greatest Hits", LogCollector.GetPropertyValue(findings[0], "AlbumName"));
    }

    /// <summary>
    /// Checks whether an album folder that follows the convention is accepted without a finding.
    /// </summary>
    [TestMethod]
    public void IterateFolderAcceptsAValidAlbumFolderName()
    {
        Directory.CreateDirectory(Path.Combine(this.testDirectory, "Rock", "Queen", "GreatestHits"));

        Program.IterateFolder(this.testDirectory, 0);

        Assert.AreEqual(0, this.logCollector.GetEvents(LogEventLevel.Warning, InvalidAlbumNameTemplate).Count);
        Assert.AreEqual(0, this.logCollector.GetEvents(LogEventLevel.Error, FilesInFolderTemplate).Count);
    }

    /// <summary>
    /// Checks whether a folder that does not exist is reported instead of ending the run with an exception.
    /// </summary>
    [TestMethod]
    public void IterateFolderReportsAFolderThatDoesNotExist()
    {
        Program.IterateFolder(Path.Combine(this.testDirectory, "DoesNotExist"), 0);

        Assert.AreEqual(1, this.logCollector.GetEvents(LogEventLevel.Error, MissingFolderTemplate).Count);
    }

    /// <summary>
    /// Creates a folder below the music folder and puts a text file into it.
    /// </summary>
    /// <param name="folderName">The folder name.</param>
    private void CreateFolderWithFile(string folderName)
    {
        var folderPath = Path.Combine(this.testDirectory, folderName);
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "stray.txt"), string.Empty);
    }
}
