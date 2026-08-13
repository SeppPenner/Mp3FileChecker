# Project rules for Claude

## What this is

Mp3FileChecker is a console application that walks a music folder tree and checks the ID3 tags of
the MP3 files in it against a fixed naming and tagging convention. Findings are written to the log,
a part of them is corrected in the file itself unless the test mode is on. The repository is an
application, it is **not** published as a NuGet package: no `GeneratePackageOnBuild`, no push
script, no installer. The release artifact is a zip of the publish output below `Published`.

One solution `src/Mp3FileChecker.sln` with exactly one project:

- `src/Mp3FileChecker/Mp3FileChecker.csproj`, `OutputType` `Exe`, target framework `net9.0`.

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

Running it, the parameters of `Main` are the command line options, see the quirks:

```powershell
dotnet run --project src/Mp3FileChecker/Mp3FileChecker.csproj -- --music-folder "D:\Music" --test-mode
```

- Single target framework `net9.0`, no multi-targeting, no `RuntimeIdentifiers` in the project file.
  The publish in `buildForWindows.bat` pins `win-x64` on the command line.
- All build properties live directly in `src/Mp3FileChecker/Mp3FileChecker.csproj`. There is **no**
  `Directory.Build.props` in this repository.
- `TreatWarningsAsErrors` is enabled, so every warning breaks the build, NuGet warnings (`NU****`)
  from restore included. A clean build reports zero warnings, keep it that way.
- `NU1803` (HTTP source usage during restore) is the one warning suppressed via `NoWarn`. Fix
  warnings instead of extending that list. `NuGetAudit` and `NuGetAuditMode=all` are on, so a
  vulnerable transitive package fails the build too.
- Versions come from GitVersion.MsBuild out of the git tags, for example `1.0.3-1` for the first
  commit after tag `1.0.2`. Never edit a version property or an assembly version by hand.
- Restore needs nuget.org. Several private feeds are configured globally on this machine, a public
  package that one of them answers with 404 fails restore with `NU1301`. Then build with an explicit
  source: `dotnet build src/Mp3FileChecker.sln --source https://api.nuget.org/v3/index.json`.
- There is no test project in this repository. A behaviour change is verified by building and by
  running the tool with `--test-mode` against a small folder tree, which logs everything and writes
  nothing. Never claim a run happened without running it.

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
- **`README.md` claims Net 8.0** while the project targets `net9.0`. The "Available for" section has
  been behind before, fix it together with a framework change instead of leaving it.
- **AppVeyor badge without CI in the repository.** `README.md` links an AppVeyor build that is
  configured outside of this repository. There is no `.github` folder and no pipeline file here.
- **`.gitignore` does not cover the publish output.** `buildForWindows.bat` writes into
  `src/Mp3FileChecker/publish`, and only `*.exe` and `*.pdb` inside it are ignored, the DLLs are not.
  After a publish that folder shows up as untracked, a `git add -A` would swallow it. Only the zip
  below `Published` belongs into the repository.
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
