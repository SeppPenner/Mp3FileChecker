# Project rules for Claude

## What this is

Mp3FileChecker is a console application that walks a music folder tree and checks the ID3 tags of
the MP3 files in it against a fixed naming and tagging convention. Findings are written to the log,
a part of them is corrected in the file itself unless the test mode is on. The repository is an
application, it is **not** published as a NuGet package: no `GeneratePackageOnBuild`, no push
script, no installer. The release artifact is a zip of the publish output below `Published`.

One solution `src/Mp3FileChecker.sln` with exactly two projects:

- `src/Mp3FileChecker/Mp3FileChecker.csproj`, `OutputType` `Exe`, target framework `net10.0`, the
  actual tool.
- `src/Mp3FileChecker.Tests/Mp3FileChecker.Tests.csproj`, MSTest, added in version 1.0.3.0.

Layout inside `src/Mp3FileChecker`:

- `Program.cs`: everything that is specific to this tool. `Main` configures Serilog and starts
  `IterateFolder`, which walks the tree and dispatches by folder depth. `CheckFilesPerArtist` and
  `CheckFilesPerAlbum` pick the rule set for a folder, `CheckFile` holds the numbered checks 1 to 23
  that are applied to a single file. New checks belong into that numbered list, keep the numbering
  and the section comment blocks intact.
- `Extensions/StringExtensions.cs`: `NeedsTrimming`, null safe by contract.
  `Extensions/ObjectExtensions.cs`: `IsEmptyOrNull` with `[NotNullWhen(false)]`.
- `Helper/ArtistHelper.cs`: `IsValid` and `GetArtistNameFromFolder`, which turns the folder name
  `Beatles_The` into the artist `The Beatles`. `Helper/AlbumHelper.cs`: `IsValid` and
  `GetAlbumNameFromFolder`.
- `GlobalUsings.cs`: all usings of the project, including the aliases `TagLibFile` and
  `TagLibIPicture`.

Layout inside `src/Mp3FileChecker.Tests`:

- `FolderIterationTests.cs`: the folder walk, that is the depth of every level, the depth of the
  music folder after its sub folders have been walked, an invalid and a valid album folder name and
  a folder that does not exist. Each test builds its tree below `Path.GetTempPath()` and deletes it
  afterwards, so a test run leaves the working tree untouched.
- `ArtistHelperTests.cs` and `AlbumHelperTests.cs`: the allowed characters and the names read from a
  folder path, including the underscore rule and the paths that are rejected.
- `StringExtensionsTests.cs` and `ObjectExtensionsTests.cs`: the two extension methods.
- `LogCollector.cs`: a Serilog sink that keeps the log events in memory. The checked code has no
  return values, it reports through the static `Log` class, so the tests set `Log.Logger` to this
  sink and assert on the message templates. Match a finding by its template text, not by the
  rendered message.
- `GlobalUsings.cs`: all usings of the test project.

Repository root: `README.md` (uppercase, the sibling repositories use `Readme.md`), `Changelog.md`,
`License.txt` (MIT), `buildForWindows.bat`, the `Published` folder with one `publish.zip` per
released version, `.gitattributes` and `.gitignore`. There is no `.github` folder, no
`Updating.md`, no `HowToUse.md` and no screenshots.

## The convention that is checked

`IterateFolder` counts the depth relative to the folder passed on the command line and decides by
that number alone what a folder is:

| Depth | Folder | Expectation |
| --- | --- | --- |
| 0 | the music folder from the command line | contains no files |
| 1 | the level below it | contains no files |
| 2 | artist folder | MP3 files without an album, `CheckFilesPerArtist` |
| 3 | album folder | MP3 files with an album, `CheckFilesPerAlbum` |

The `switch` has no `default` case, so folders deeper than 3 are still walked but nothing in them is
checked. Both check methods `return` instead of `break`, which is what stops the recursion result
from falling through to the file check of the parent.

Name rules that follow from the checks: an artist folder may be named `Lastname_Firstname` and is
read as `Firstname Lastname`, the album folder name is the album, and a file must be named
`{Title}-{Artist}.mp3`. Inside an album the tags `Album` and `Track` have to be set, outside of one
`Album` and the pictures are removed. `Comment`, `Year`, `AlbumArtists`, `Composers` and `Disc` are
always removed.

## Build

```powershell
dotnet build src/Mp3FileChecker.sln
```

```powershell
dotnet test src/Mp3FileChecker.sln
```

Running it, the parameters of `Main` are the command line options, see the quirks:

```powershell
dotnet run --project src/Mp3FileChecker/Mp3FileChecker.csproj -- --music-folder "D:\Music" --test-mode
```

- Single target framework `net10.0`, no multi-targeting, no `RuntimeIdentifiers` in the project file.
  `buildForWindows.bat` pins `win-x64` and `--self-contained true` on the command line, so the
  shipped tool needs no installed runtime. The publish is around 214 files and 78 MB, the zip of it
  around 34 MB. The batch stops with an error instead of printing "Build successful" when the
  publish fails, otherwise a failed publish would be zipped into a release as the old output.
- All build properties live directly in `src/Mp3FileChecker/Mp3FileChecker.csproj`. There is **no**
  `Directory.Build.props` in this repository.
- `TreatWarningsAsErrors` is enabled, so every warning breaks the build, NuGet warnings (`NU****`)
  from restore included. A clean build reports zero warnings, keep it that way.
- `NU1803` (HTTP source usage during restore) is the one warning suppressed via `NoWarn`. Fix
  warnings instead of extending that list. `NuGetAudit` and `NuGetAuditMode=all` are on, so a
  vulnerable transitive package fails the build too.
- Versions come from GitVersion.MsBuild out of the git tags, for example `1.0.3-1` for the first
  commit after tag `1.0.2`. Never edit a version property or an assembly version by hand.
- Restore needs nuget.org. Several private feeds are configured globally on this machine. When one
  of them is unreachable (no VPN) or answers 404 for a public package, restore fails with `NU1900`
  or `NU1301`, and `TreatWarningsAsErrors` turns that into a build error. Then build with an
  explicit source:
  `dotnet build src/Mp3FileChecker.sln --source https://api.nuget.org/v3/index.json`.
- Tests are MSTest, in the single test project `src/Mp3FileChecker.Tests`, which follows the same
  package set as the sibling repositories: `Microsoft.NET.Test.Sdk`, `MSTest.TestAdapter`,
  `MSTest.TestFramework`, `coverlet.collector` and `GitVersion.MsBuild`. `dotnet test` runs 26 tests,
  they need no network, no MP3 file and no fixture outside the repository. Never claim a test run
  happened without running it.
- `Program.IterateFolder` is `internal` and the project file grants
  `<InternalsVisibleTo Include="Mp3FileChecker.Tests" />` so that the folder walk can be driven
  without a command line. Everything below it stays private, a new test reaches it through
  `IterateFolder`.
- Beyond the tests, a behaviour change is verified by running the tool with `--test-mode` against a
  small folder tree, which logs everything and writes nothing.

## Code conventions

Follow the surrounding code, it is consistent throughout every file:

- File header comment block with `<copyright file="..." company="Hämmer Electronics">` and a
  `<summary>`, then the file-scoped namespace.
- XML doc comments on every type and every member, private members included, no exceptions. This is
  not only style here, it is required by the build, see the quirks.
- `Nullable` and `ImplicitUsings` are enabled.
- New `using` directives go into `GlobalUsings.cs`, inside the existing `#pragma warning disable
  IDE0065` block, never at the top of a file. The editorconfig requires usings inside the namespace
  (`csharp_using_directive_placement=inside_namespace:warning`), which global usings cannot satisfy,
  that is what the pragma is for. Do not add other pragmas. The comment text in that block is German
  because Visual Studio generated it, leave it alone.
- Types from TagLib# are only used through the aliases `TagLibFile` and `TagLibIPicture`.
- Fields, properties, methods and events are always accessed with `this.` qualification
  (`dotnet_style_qualification_for_*` at severity `warning`). All members in this project are
  static, so that rule currently has nothing to bite on.
- Private constants are named in camel case (`mp3FileEnding`, `allowedTitleChars`), the one constant
  in `StringExtensions` is not (`EmptyChar`). Follow the file you are in.
- Log messages use Serilog message templates with named placeholders (`{FilePath}`), never string
  interpolation, and `{@Files}` for collections. The message text itself has no trailing period.
- `src/.editorconfig` also enforces braces everywhere, no multiple blank lines, four spaces, CRLF,
  UTF-8, file scoped namespaces, `System` usings sorted first and `IDE0005` as warning. Analyzer
  warnings are fixed, not silenced.

## Known quirks

Do not silently "clean up" these, they are existing behaviour:

- **`Main` is not the entry point.** `System.CommandLine.DragonFruit` sets `AutoGenerateEntryPoint`,
  generates an `AutoGeneratedProgram` class into `obj` and makes that the `StartupObject`. It then
  finds `Program.Main(string musicFolder, bool testMode)` by reflection and binds the parameters to
  the options `--music-folder` and `--test-mode`. The signature of `Main` therefore **is** the
  command line contract, renaming a parameter renames an option.
- **XML doc comments are mandatory because of DragonFruit.** The same package forces
  `GenerateDocumentationFile` to `true`, because it reads the `<param>` texts to build the help
  output. Together with `TreatWarningsAsErrors` that turns every missing doc comment (`CS1591`) into
  a build error. That is why the documentation is complete everywhere, and why the publish output
  contains `Mp3FileChecker.xml`.
- **DragonFruit is a dead package.** `0.4.0-alpha.22272.1` from 2022 is its last release and the
  only version there ever was on nuget.org, `dotnet list package --outdated` reports it as "Not
  found at the sources". That line is expected, it is not a broken feed. There is no drop-in
  successor, `System.CommandLine` 2.x has a different model and would mean writing the entry point
  by hand.
- **No character set contains a space.** `allowedTitleChars`, `allowedGenreChars`,
  `allowedArtistChars` and `allowedAlbumChars` are letters and digits only (the title additionally
  allows `'?!`). Every multi word title, album or genre is therefore reported as "contains not
  allowed characters", and an artist folder `Beatles_The` produces `The Beatles`, which
  `ArtistHelper.IsValid` then rejects, so the whole folder is skipped with a warning. This was
  reviewed on 2026-08-13 and deliberately left as it is. Do not add the space without being asked.
- **The folder parsing is Windows only.** `GetArtistNameFromFolder` and `GetAlbumNameFromFolder`
  split on `\` literally instead of using `Path.DirectorySeparatorChar`, and the artist helper
  additionally requires at least three path segments, so a music folder handed in as a relative path
  or with forward slashes silently yields an empty name. Nothing else in the code is Windows
  specific.
- **The MP3 detection is case sensitive.** `f.EndsWith(".mp3")` uses the current culture and the
  exact spelling, so a file named `Song.MP3` counts as a non MP3 file and is reported as an invalid
  file in the folder instead of being checked.
- **Test mode only stops the write.** With `--test-mode` the checks still run, the trimming and the
  removals are still applied to the in memory tag and still logged as "Trimming ..." and
  "Removing ...", only the final `tagFile.Save()` is skipped. The log of a test run therefore reads
  like a run that changed files.
- **Check 23 is a Todo.** The cover handling inside an album folder is not implemented,
  `CheckFilesPerAlbum` collects the non MP3 files and passes them to `CheckFile`, where the parameter
  is currently unused. Leave the parameter, it is the hook for that check.
- **The log file is written to the current directory.** `WriteTo.File($"log{...:yyyyMMdd_HHmmss}.txt")`
  builds a new file name per start, relative to wherever the tool was started, not next to the
  executable and not below the music folder.
- **The "Available for" section of `README.md`** claimed Net 8.0 while the project already targeted
  `net9.0`. It is the only place besides the project file that names the framework, update it in the
  same commit as a framework change instead of leaving it behind again.
- **AppVeyor badge without CI in the repository.** `README.md` links an AppVeyor build that is
  configured outside of this repository. There is no `.github` folder and no pipeline file here.
- **The publish folder is ignored, the zip is not.** `buildForWindows.bat` writes into
  `src/Mp3FileChecker/publish`, which `.gitignore` covers since version 1.0.3.0. Before that only
  `*.exe` and `*.pdb` in it were ignored and a `git add -A` after a publish would have swallowed 200
  DLLs. The rule is `publish/`, it does not touch the `Published` folder, whose zips are the release
  artifacts and belong into the repository.
- **`.gitattributes` sets `* text=auto`** and every rule of the Visual Studio template below it is
  commented out. The `publish.zip` files are only treated as binary because git guesses right on
  their content. Any further binary file needs its own rule.

## Releasing

The history of this repository shows the pattern, follow it:

1. Make the change.
2. Add an entry at the top of `Changelog.md` in the existing format:
   `* **Version 1.0.3.0 (2026-08-13)** : Short description.`
3. Commit that.
4. Tag that commit with the plain version number, no `v` prefix (`1.0.2`, `1.0.1`, ...). The existing
   tags are lightweight tags, create new ones the same way. The tag has to exist **before** the
   publish, otherwise GitVersion burns a prerelease version such as `1.0.3-1+Branch.master.Sha...`
   into the shipped executable.
5. Run `buildForWindows.bat`, zip the resulting `src/Mp3FileChecker/publish` folder to
   `Published/<version>/publish.zip` (the zip keeps the `publish` folder as its top level entry) and
   commit that as a follow-up commit, the way `51fb0f3` and `15cd886` did it.
6. Push the commits and the tag.

Two things about step 5 on this machine. `cmd` runs with `NoDefaultCurrentDirectoryInExePath`, so
the batch has to be started as `call .\buildForWindows.bat` from the repository root, the `cd
src\Mp3FileChecker` inside it is relative to that. And when a private feed is unreachable, add
`--source https://api.nuget.org/v3/index.json` to the `dotnet publish` line of that run, do not pin
the source in the batch, it has to keep working on other machines.

The version in the `Changelog.md` has four parts (`1.0.3.0`), the tag has three (`1.0.3`), the folder
below `Published` uses the three part form as well.

## Git

- **Never amend a commit.** No `git commit --amend`, not for a typo in the message, not to add a
  forgotten file, not even when the commit is still local. Write a follow-up commit instead. The
  release versions come from tags on exact commits, an amended commit leaves its tag pointing at a
  commit that no longer exists in the branch.

## Writing style

- Commit messages are written **in English only**: short, precise subject line, explanatory body
  when needed.
- Code comments and comments in project files such as `.csproj` are **always English**, regardless
  of the language used in the conversation.
- **No em dashes or en dashes**, neither in prose, commit messages, code comments nor documentation.
  Use a regular hyphen, comma, colon, parentheses or a separate sentence.
- German texts (documentation, chat replies) always use real umlauts and ß, never ASCII
  transliterations such as `ae`, `oe`, `ue` or `ss`. Identifiers, file names and configuration keys
  stay unchanged where umlauts are technically undesirable.
