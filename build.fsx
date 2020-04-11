#r "paket: groupref build //"
#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

Target.initEnvironment ()

let root = __SOURCE_DIRECTORY__
System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let packageDir = root </> "out"
let release = ReleaseNotes.load "RELEASE_NOTES.adoc"


Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ packageDir
    |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
    !! "src/**/*.fsproj"
    |> Seq.iter (DotNet.build id)
)

Target.create "Pack" (fun _ ->
    Paket.pack (fun p ->
        { p with
            OutputPath = packageDir
            Version = release.NugetVersion
            ReleaseNotes = String.concat "\n" release.Notes
            MinimumFromLockFile = false }
    )
)

Target.create "Push" (fun _ ->
    Paket.push (fun p -> { p with PublishUrl = "https://www.nuget.org"; WorkingDir = packageDir })
)

Target.create "Default" ignore
Target.create "Release" ignore

"Clean"
    ==> "Build"
    ==> "Pack"
    ==> "Default"

"Default"
    ==> "Push"
    ==> "Release"

Target.runOrDefault "Default"