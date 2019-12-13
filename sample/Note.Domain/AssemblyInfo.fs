namespace Note.Domain.AssemblyInfo

open System.Reflection

[<assembly: AssemblyTitleAttribute("Note.Domain")>]
[<assembly: AssemblyProductAttribute("Note.Domain")>]
[<assembly: AssemblyCopyrightAttribute("Copyright 2019")>]
[<assembly: AssemblyDescriptionAttribute("Domain layer library for Note")>]
[<assembly: AssemblyVersionAttribute("0.0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.0.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] AssemblyTitle = "Note.Domain"
    let [<Literal>] AssemblyProduct = "Note.Domain"
    let [<Literal>] AssemblyCopyright = "Copyright 2019"
    let [<Literal>] AssemblyDescription = "Domain layer library for Note"
    let [<Literal>] AssemblyVersion = "0.0.1"
    let [<Literal>] AssemblyFileVersion = "0.0.1"