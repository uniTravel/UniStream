#r "paket: groupref build //"
#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators


let projectDescription = "Lightweight framework for CQRS"
let authors = [ "Eric" ]
let root = __SOURCE_DIRECTORY__
let packageDir = root @@ "out"
let release = ReleaseNotes.load "RELEASE_NOTES.adoc"

let genAssemblyInfo (projectPath: string) =
    let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
    let folderName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(projectPath))
    let basePath = "src" @@ folderName
    let fileName = basePath @@ "AssemblyInfo.fs"
    AssemblyInfoFile.createFSharp fileName
        [ AssemblyInfo.Title projectName
          AssemblyInfo.Description projectDescription
          AssemblyInfo.Product "UniStream"
          AssemblyInfo.Company <| String.separated ";" authors
          AssemblyInfo.Version release.AssemblyVersion
          AssemblyInfo.FileVersion release.NugetVersion
          AssemblyInfo.InformationalVersion release.NugetVersion
          AssemblyInfo.InternalsVisibleTo "Note.Tests" ]


Target.initEnvironment ()

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ packageDir
    |> Shell.cleanDirs)

Target.create "Debug" (fun _ ->
    !! "sample/**/bin"
    ++ "sample/**/obj"
    |> Shell.cleanDirs
    !! "sample/Note.Tests/*.fsproj"
    |> Seq.iter (DotNet.build (fun b -> { b with Configuration = DotNet.BuildConfiguration.Debug })))

Target.create "Infrastructure.EventStore.Test" (fun _ ->
    !! "test/Infrastructure.EventStore.Tests/bin"
    ++ "test/Infrastructure.EventStore.Tests/obj"
    ++ "src/Infrastructure.EventStore/bin"
    ++ "src/Infrastructure.EventStore/obj"
    |> Shell.cleanDirs
    !! "test/Infrastructure.EventStore.Tests/*.fsproj"
    |> Seq.iter (DotNet.build (fun b -> { b with Configuration = DotNet.BuildConfiguration.Debug })))

Target.create "Domain.Test" (fun _ ->
    !! "test/Domain.Tests/bin"
    ++ "test/Domain.Tests/obj"
    ++ "src/Domain/bin"
    ++ "src/Domain/obj"
    |> Shell.cleanDirs
    !! "test/Domain.Tests/*.fsproj"
    |> Seq.iter (DotNet.build (fun b -> { b with Configuration = DotNet.BuildConfiguration.Debug })))

Target.create "AssemblyInfo" (fun _ ->
    !! "src/**/*.fsproj"
    |> Seq.iter genAssemblyInfo)

Target.create "Build" (fun _ ->
    Environment.setEnvironVar "GenerateDocumentationFile" "true"
    Environment.setEnvironVar "GenerateAssemblyInfo" "false"
    !! "src/**/*.fsproj"
    |> Seq.iter (DotNet.build id))

Target.create "Pack" (fun _ ->
    Paket.pack (fun p ->
        { p with
            OutputPath = packageDir
            Version = release.NugetVersion
            IncludeReferencedProjects = true
            ReleaseNotes = release.Notes |> String.toLines }))

Target.create "Push" (fun _ ->
    Paket.push (fun p -> { p with PublishUrl = "https://www.nuget.org"; WorkingDir = packageDir }))

Target.create "Default" ignore
Target.create "Release" ignore

"Clean"
    ==> "AssemblyInfo"
    ==> "Build"
    ==> "Pack"
    ==> "Push"
    ==> "Release"

"Clean"
    ==> "Debug"
    ==> "Default"

Target.runOrDefault "Default"