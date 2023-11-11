// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   The main program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Mp3FileChecker;

/// <summary>
/// The main program.
/// </summary>
public static class Program
{
    /// <summary>
    /// The MP3 file ending.
    /// </summary>
    private const string mp3FileEnding = ".mp3";

    /// <summary>
    /// The allowed chars to use within the title.
    /// </summary>
    private const string allowedTitleChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'?!";

    /// <summary>
    /// The allowed chars to use within the genre.
    /// </summary>
    private const string allowedGenreChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// A value indicating whether the test mode is on or not (No file updates are made in the test mode).
    /// </summary>
    private static bool useTestMode = false;

    /// <summary>
    /// The main method.
    /// </summary>
    /// <param name="musicFolder">The top level music folder.</param>
    /// <param name="testMode">A value indicating whether the test mode is on or not (No file updates are made in the test mode).</param>
    public static void Main(string musicFolder, bool testMode = false)
    {
        // Setup Serilog logging.
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File($"log{DateTimeOffset.Now:yyyyMMdd_HHmmss}.txt")
            .CreateLogger();

        // Get and validate settings.
        if (string.IsNullOrWhiteSpace(musicFolder))
        {
            Log.Error("The music folder was empty");
            return;
        }

        useTestMode = testMode;

        // Iterate the folders below the main music folder.
        IterateFolder(musicFolder, 0);
    }

    /// <summary>
    /// Iterates the given folder.
    /// </summary>
    /// <param name="folderPath">The folder path.</param>
    /// <param name="currentDepth">The current folder depth.</param>
    private static void IterateFolder(string folderPath, int currentDepth)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            Log.Error("The folder path was empty or not found: {FolderPath}", folderPath);
            return;
        }

        // Check all sub folders first.
        foreach (var subFolder in Directory.GetDirectories(folderPath))
        {
            IterateFolder(subFolder, currentDepth++);
        }

        // Check files afterwards.
        var files = Directory.GetFiles(folderPath);

        switch (currentDepth)
        {
            case 0:
            case 1:
                if (files.Length != 0)
                {
                    Log.Error("There shouldn't be any files in folder {Folder}, but found some files {@Files}", folderPath, files);
                }

                break;

            case 2:
                CheckFilesPerArtist(files, folderPath);
                return;

            case 3:
                CheckFilesPerAlbum(files, folderPath);
                return;
        }
    }

    /// <summary>
    /// Checks the files per artist folder (No album).
    /// </summary>
    /// <param name="files">The files.</param>
    /// <param name="folderPath">The folder path.</param>
    private static void CheckFilesPerArtist(string[] files, string folderPath)
    {
        // Get an check the artist name from the folder path.
        var artistNameFromFolder = ArtistHelper.GetArtistNameFromFolder(folderPath, false);

        if (!ArtistHelper.IsValid(artistNameFromFolder))
        {
            return;
        }

        // There should be no non mp3 files within the artist folder.
        var nonMp3Files = files.Where(f => !f.EndsWith(mp3FileEnding)).ToList();

        if (!nonMp3Files.IsEmptyOrNull())
        {
            Log.Warning("There are some invalid files {@Files} in the folder {FolderPath}", nonMp3Files, folderPath);
        }

        // Check all mp3 files.
        var mp3Files = files.Where(f => f.EndsWith(mp3FileEnding)).ToList();

        foreach (var file in mp3Files)
        {
            CheckFile(file, artistNameFromFolder, null, null);
        }
    }

    /// <summary>
    /// Checks the files per album folder.
    /// </summary>
    /// <param name="files">The files.</param>
    /// <param name="folderPath">The folder path.</param>
    private static void CheckFilesPerAlbum(string[] files, string folderPath)
    {
        // Get an check the artist name from the folder path.
        var artistNameFromFolder = ArtistHelper.GetArtistNameFromFolder(folderPath, true);

        if (!ArtistHelper.IsValid(artistNameFromFolder))
        {
            return;
        }

        // Get an check the album name from the folder path.
        var albumNameFromFolder = AlbumHelper.GetAlbumNameFromFolder(folderPath);

        if (!AlbumHelper.IsValid(artistNameFromFolder))
        {
            return;
        }

        // Check all mp3 files with the non mp3 files as possible album covers.
        var nonMp3Files = files.Where(f => !f.EndsWith(mp3FileEnding)).ToList();
        var mp3Files = files.Where(f => f.EndsWith(mp3FileEnding)).ToList();

        foreach (var file in mp3Files)
        {
            CheckFile(file, artistNameFromFolder, albumNameFromFolder, nonMp3Files);
        }
    }

    /// <summary>
    /// Checks the MP3 file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="artistNameFromFolder">The artist name from the folder.</param>
    /// <param name="albumNameFromFolder">The album name from the folder (If any).</param>
    /// <param name="nonMp3Files">The files in the folder that are not MP3 files (Might be images for covers, etc...).</param>
    private static void CheckFile(string filePath, string artistNameFromFolder, string? albumNameFromFolder, List<string>? nonMp3Files)
    {
        if (!File.Exists(filePath))
        {
            Log.Error("The file doesn't exist (anymore): {FilePath}", filePath);
            return;
        }

        // Get a tag file.
        var fileName = Path.GetFileName(filePath) ?? string.Empty;
        var tagFile = TagLibFile.Create(filePath);
        var needsUpdate = false;

        // Global checks that should be done on all files (Album or not):
        // ---------------------------------------------------------------------------------------------------
        // Title.
        // 1. Is the title set?
        if (string.IsNullOrWhiteSpace(tagFile.Tag.Title))
        {
            Log.Error("The title {Title} for the file {FilePath} is not set", tagFile.Tag.Title, filePath);
        }

        // 2. Is the title trimmed?
        if (tagFile.Tag.Title.NeedsTrimming())
        {
            Log.Information("Trimming title {Title} for file {FilePath}", tagFile.Tag.Title, filePath);
            tagFile.Tag.Title = tagFile.Tag.Title.Trim();
            needsUpdate = true;
        }

        // 3. Does the title only contain valid chars?
        if (!tagFile.Tag.Title.All(allowedTitleChars.Contains))
        {
            Log.Error("The title {Title} for the file {FilePath} contains not allowed characters", tagFile.Tag.Title, filePath);
        }

        // ---------------------------------------------------------------------------------------------------
        // Artist.
        // 4. Are the artists set?
        if (tagFile.Tag.Performers.IsEmptyOrNull())
        {
            Log.Error("The artists {@Artists} for the file {FilePath} are not set", tagFile.Tag.Performers, filePath);
        }

        // 5. Is only one artist set?
        if (tagFile.Tag.Performers.Count() != 1)
        {
            Log.Error("Multiple artists {@Artists} are set for the file {FilePath}", tagFile.Tag.Performers, filePath);
        }

        // 6. Is the (first and only) artist trimmed?
        if (tagFile.Tag.Performers.FirstOrDefault().NeedsTrimming())
        {
            // Just trim if there is really only 1 artist.
            if (tagFile.Tag.Performers.Count() == 1)
            {
                Log.Information("Trimming artist {Title} for file {FilePath}", tagFile.Tag.Performers[0], filePath);
                tagFile.Tag.Performers[0] = tagFile.Tag.Performers[0].Trim();
                needsUpdate = true;
            }
        }

        // 7. Does the (first and only) artist only contain valid chars?
        if (!ArtistHelper.IsValid(tagFile.Tag.Performers.FirstOrDefault()))
        {
            Log.Error("The artist {Artist} for the file {FilePath} contains not allowed characters", tagFile.Tag.Performers.FirstOrDefault(), filePath);
        }

        // 8. Does the (first and only) artist equal the artist name from the folder?
        if (artistNameFromFolder != tagFile.Tag.Performers.FirstOrDefault())
        {
            Log.Error("The artist {Artist} for the file {FilePath} doesn't equal the artist tag {ArtistTag}",
                artistNameFromFolder,
                filePath,
                tagFile.Tag.Performers.FirstOrDefault());
        }

        // ---------------------------------------------------------------------------------------------------
        // Genre.
        // 9. Are the genres set?
        if (tagFile.Tag.Genres.IsEmptyOrNull())
        {
            Log.Error("The genres {@Genres} for the file {FilePath} are not set", tagFile.Tag.Genres, filePath);
        }

        // 10. Is only one genre set?
        if (tagFile.Tag.Genres.Count() != 1)
        {
            Log.Error("Multiple genres {@Genres} are set for the file {FilePath}", tagFile.Tag.Genres, filePath);
        }

        // 11. Is the (first and only) genre trimmed?
        if (tagFile.Tag.Genres.FirstOrDefault().NeedsTrimming())
        {
            // Just trim if there is really only 1 genre.
            if (tagFile.Tag.Genres.Count() == 1)
            {
                Log.Information("Trimming genre {Genre} for file {FilePath}", tagFile.Tag.Genres[0], filePath);
                tagFile.Tag.Genres[0] = tagFile.Tag.Genres[0].Trim();
                needsUpdate = true;
            }
        }

        // 12. Does the (first and only) genre only contain valid chars?
        if (tagFile.Tag.Genres.FirstOrDefault() is not null && !tagFile.Tag.Genres.First().All(allowedGenreChars.Contains))
        {
            Log.Error("The genre {Genre} for the file {FilePath} contains not allowed characters", tagFile.Tag.Genres.FirstOrDefault(), filePath);
        }

        // ---------------------------------------------------------------------------------------------------
        // Comment.
        // 13. Is the comment set? --> Erase it.
        if (!string.IsNullOrWhiteSpace(tagFile.Tag.Comment))
        {
            Log.Information("Removing comment {Comment} for file {FilePath}", tagFile.Tag.Comment, filePath);
            tagFile.Tag.Comment = string.Empty;
            needsUpdate = true;
        }

        // ---------------------------------------------------------------------------------------------------
        // Year.
        // 14. Is the year set? --> Erase it.
        if (tagFile.Tag.Year > 0)
        {
            Log.Information("Removing year {Year} for file {FilePath}", tagFile.Tag.Year, filePath);
            tagFile.Tag.Year = 0;
            needsUpdate = true;
        }

        // ---------------------------------------------------------------------------------------------------
        // Album artists.
        // 15. Are the album artists set? --> Erase them.
        if (!tagFile.Tag.AlbumArtists.IsEmptyOrNull())
        {
            Log.Information("Removing album artists {@AlbumArtists} for file {FilePath}", tagFile.Tag.AlbumArtists, filePath);
            tagFile.Tag.AlbumArtists = Array.Empty<string>();
            needsUpdate = true;
        }

        // ---------------------------------------------------------------------------------------------------
        // Composers.
        // 16. Are the composers set? --> Erase them.
        if (!tagFile.Tag.Composers.IsEmptyOrNull())
        {
            Log.Information("Removing composers {@Composers} for file {FilePath}", tagFile.Tag.Composers, filePath);
            tagFile.Tag.Composers = Array.Empty<string>();
            needsUpdate = true;
        }

        // ---------------------------------------------------------------------------------------------------
        // Disc.
        // 17. Is the disc set? --> Erase it.
        if (tagFile.Tag.Disc > 0)
        {
            Log.Information("Removing disc {Disc} for file {FilePath}", tagFile.Tag.Disc, filePath);
            tagFile.Tag.Disc = 0;
            needsUpdate = true;
        }

        // ---------------------------------------------------------------------------------------------------
        // File name.
        // 18. Does the file name equal the form {Title}-{Performer}?
        if (!tagFile.Tag.Performers.IsEmptyOrNull() && !$"{tagFile.Tag.Title}-{tagFile.Tag.Performers[0]}{mp3FileEnding}".Equals(fileName))
        {
            Log.Error("The file name {FileName} from {FilePath} doesn't match the convention for file names \"{Title}-{Artist}.mp3\"",
               fileName,
               filePath,
               tagFile.Tag.Title,
               tagFile.Tag.Performers.FirstOrDefault());
        }

        // ---------------------------------------------------------------------------------------------------
        // Album or not?
        // If the files are not within an album folder, we need to check that the album isn't set.
        if (string.IsNullOrWhiteSpace(albumNameFromFolder))
        {
            // ---------------------------------------------------------------------------------------------------
            // Album.
            // 19. Is the album set? --> Erase it.
            if (!string.IsNullOrWhiteSpace(tagFile.Tag.Album))
            {
                Log.Information("Removing album {Album} for file {FilePath}", tagFile.Tag.Album, filePath);
                tagFile.Tag.Album = string.Empty;
                needsUpdate = true;
            }

            // ---------------------------------------------------------------------------------------------------
            // Cover.
            // 20. Are some pictures set? --> Erase them.
            if (!tagFile.Tag.Pictures.IsEmptyOrNull())
            {
                Log.Information("Removing pictures {Pictures} for file {FilePath}", tagFile.Tag.Pictures, filePath);
                tagFile.Tag.Pictures = Array.Empty<TagLibIPicture>();
                needsUpdate = true;
            }
        }
        else
        {
            // If the files are within the album folder, we need to check that the album is set.
            // ---------------------------------------------------------------------------------------------------
            // Album.
            // 19. Is the album set?
            if (string.IsNullOrWhiteSpace(tagFile.Tag.Album))
            {
                Log.Error("The album {Album} for the file {FilePath} is not set", tagFile.Tag.Album, filePath);
            }

            // 20. Is the album trimmed?
            if (tagFile.Tag.Album.NeedsTrimming())
            {
                Log.Information("Trimming album {Album} for file {FilePath}", tagFile.Tag.Album, filePath);
                tagFile.Tag.Album = tagFile.Tag.Album.Trim();
                needsUpdate = true;
            }

            // 21. Does the album only contain valid chars?
            if (!AlbumHelper.IsValid(tagFile.Tag.Album))
            {
                Log.Error("The album {Album} for the file {FilePath} contains not allowed characters", tagFile.Tag.Album, filePath);
            }

            // ---------------------------------------------------------------------------------------------------
            // Track.
            // 22. Is the track set?
            if (tagFile.Tag.Track == 0)
            {
                Log.Warning("The track {Track} for the file {FilePath} is not set", tagFile.Tag.Track, filePath);
            }

            // ---------------------------------------------------------------------------------------------------
            // Cover.
            // 23. If the cover is not set, add it from an image found from the non MP3 files.
            //     If the cover is set, check if it fits any of the non MP3 files.
            // Todo: Implement this!
        }

        // Only update when not in test mode.
        if (needsUpdate && !useTestMode)
        {
            Log.Information("Updating file {FilePath}", filePath);
            tagFile.Save();
        }
    }
}
