namespace UniStream.Domain.AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitleAttribute("UniStream.Domain")>]
[<assembly: AssemblyProductAttribute("UniStream.Domain")>]
[<assembly: AssemblyCopyrightAttribute("Copyright 2019")>]
[<assembly: AssemblyDescriptionAttribute("Domain layer library for UniStream")>]
[<assembly: AssemblyVersionAttribute("0.1.2")>]
[<assembly: AssemblyFileVersionAttribute("0.1.2")>]
[<assembly: InternalsVisibleToAttribute("Tests")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] AssemblyTitle = "UniStream.Domain"
    let [<Literal>] AssemblyProduct = "UniStream.Domain"
    let [<Literal>] AssemblyCopyright = "Copyright 2019"
    let [<Literal>] AssemblyDescription = "Domain layer library for UniStream"
    let [<Literal>] AssemblyVersion = "0.1.2"
    let [<Literal>] AssemblyFileVersion = "0.1.2"