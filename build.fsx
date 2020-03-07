#r "paket: groupref build //"
#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.DotNet.NuGet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

Target.initEnvironment ()

let root = __SOURCE_DIRECTORY__
System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let packageDir = root </> "out"


Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ packageDir
    |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
    !! "src/Domain/*.fsproj"
    ++ "src/Infrastructure/*.fsproj"
    |> Seq.iter (DotNet.build id)
)

Target.create "Pack" (fun _ ->
    Environment.setEnvironVar "Authors" "Eric"
    !! "src/Domain"
    ++ "src/Infrastructure"
    |> Seq.iter (fun dir ->
        let release = ReleaseNotes.load (dir </> "RELEASE_NOTES.adoc")
        Environment.setEnvironVar "Version" release.NugetVersion
        Environment.setEnvironVar "PackageReleaseNotes" (release.Notes |> String.toLines)
        DotNet.pack (fun p -> { p with OutputPath = Some packageDir }) dir
    )
)

Target.create "Push" (fun _ ->
    Paket.push (fun p -> { p with PublishUrl = "https://www.nuget.org"; WorkingDir = packageDir })
)

Target.create "Default" ignore
Target.create "Release" ignore

"Clean"
    ==> "Pack"
    ==> "Default"

"Default"
    ==> "Push"
    ==> "Release"

Target.runOrDefault "Default"