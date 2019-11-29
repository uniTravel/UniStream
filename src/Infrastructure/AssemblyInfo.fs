namespace UniStream.Infrastructure.AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitleAttribute("UniStream.Infrastructure")>]
[<assembly: AssemblyProductAttribute("UniStream.Infrastructure")>]
[<assembly: AssemblyCopyrightAttribute("Copyright 2019")>]
[<assembly: AssemblyDescriptionAttribute("Infrastructure layer library for UniStream")>]
[<assembly: AssemblyVersionAttribute("0.0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.0.1")>]
[<assembly: InternalsVisibleToAttribute("Tests")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] AssemblyTitle = "UniStream.Infrastructure"
    let [<Literal>] AssemblyProduct = "UniStream.Infrastructure"
    let [<Literal>] AssemblyCopyright = "Copyright 2019"
    let [<Literal>] AssemblyDescription = "Infrastructure layer library for UniStream"
    let [<Literal>] AssemblyVersion = "0.0.1"
    let [<Literal>] AssemblyFileVersion = "0.0.1"