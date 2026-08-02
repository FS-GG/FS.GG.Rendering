// GENERATED — do not edit. Emitted with the scaffold and renamed with the Product.
// Build first, then load the Product and its transitive dependencies in one step:
//   ./build.sh build
//   dotnet fsi load-Product.fsx
//
// This script references and opens only the app; .NET resolves its dependencies from
// the same build output. It launches nothing, so a missing assembly is a normal load
// failure and host-warning classification is unaffected. Loading the app does not make
// its dependency namespaces available for a later script to open directly; such a script
// must carry explicit #r directives for the framework assemblies it uses.
#r "src/Product/bin/Debug/net10.0/Product.dll"
open Product
