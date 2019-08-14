// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------
#r "paket: groupref Build //"
#load "./.fake/build.fsx/intellisense.fsx"
#nowarn "0044"
#if !FAKE
//#r "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\2.2.0\\netstandard.dll" // vscode bug
#r "Facades/netstandard" // mono
#r "netstandard"
#endif

open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Tools
open System
open System.IO

let project = "Test-Tree-Shaking"
let summary = "Test-Tree-Shaking"
let description = summary

let clientPath = "./src/Client" |> Path.getFullName
let clientTestsPath = "./test/UITests" |> Path.getFullName

let dotnetcliVersion = DotNet.getSDKVersionFromGlobalJson()
let dotnetExePath = "dotnet"

let deployDir = "./deploy"

// Pattern specifying assemblies to be tested using expecto
let clientTestExecutables = "test/UITests/**/bin/**/*Tests*.exe"

let asyncNull = (async { return "" })

let asyncCombine fn1 fn2 = async {
    // start both computations simultaneously
    let! rfn1 = Async.StartChild fn1
    let! rfn2 = Async.StartChild fn2

    // retrieve results
    let! result1 = rfn1
    let! result2 = rfn2

    return sprintf "%s, %s" (result1.ToString()) (result2.ToString())
}

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg

let nodeTool = platformTool "node" "node.exe"
let npmTool  = platformTool "npm" "npm.cmd"
let yarnTool = platformTool "yarn" "yarn.cmd"

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let runDotnet workingDir cmd =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let openBrowser url =
    //https://github.com/dotnet/corefx/issues/10361
    Command.ShellCommand url
    |> CreateProcess.fromCommand
    |> CreateProcess.ensureExitCodeWithMessage "opening browser failed"
    |> Proc.run
    |> ignore

if not <| Environment.isWindows then
    // We have to set the FrameworkPathOverride so that dotnet sdk invocations know
    // where to look for full-framework base class libraries
    let mono = platformTool "mono" "mono"
    let frameworkPath = IO.Path.GetDirectoryName(mono) </> ".." </> "lib" </> "mono" </> "4.5"
    Environment.setEnvironVar "FrameworkPathOverride" frameworkPath

let vsCodeSession () = Environment.hasEnvironVar "vsCodeSession"

// --------------------------------------------------------------------------------------
// Clean build results

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [
        "bin"; "temp"; "docs/output";
        Path.Combine(clientPath, "public/bundle")
        Path.Combine(clientPath, "deploy")
    ]

    let clientDir = deployDir
    let fontsDir = clientDir </> "fonts"
    let cssDir = clientDir </> "css"

    !! (clientDir </> "*.*")
    ++ (fontsDir </> "*.*")
    ++ (cssDir </> "*.*")
    |> Seq.iter Shell.rm

    !! "src/**/bin"
    ++ "test/**/bin"
    |> Shell.cleanDirs

    !! "src/**/obj/*.nuspec"
    ++ "test/**/obj/*.nuspec"
    |> Seq.iter Shell.rm
)

Target.create "InstallDotNetCore" (fun _ ->
    let setParams (options : DotNet.CliInstallOptions) =
        { options with
            Version = DotNet.Version dotnetcliVersion }

    DotNet.install setParams |> ignore
)

// --------------------------------------------------------------------------------------
// Build client & its test project

// Target.create "RestoreClientTests" (fun _ ->
//     runDotnet clientTestsPath "restore"
// )

// Target.create "BuildClientTests" (fun _ ->
//     runDotnet clientTestsPath "build"
// )

Target.create "RestoreClient" (fun _ ->
    printfn "Node version:"
    runTool nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    runTool yarnTool "--version" __SOURCE_DIRECTORY__
    runTool yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
    runDotnet clientPath "restore"
)

Target.create "BuildClient" (fun _ ->
    runDotnet clientPath "build -c Release"
    runTool yarnTool "webpack-cli -p --mode production" __SOURCE_DIRECTORY__
)

// Rename driver for macOS or Linux
Target.create "RenameDrivers" (fun _ ->
    if not Environment.isWindows then
        runTool npmTool "install phantomjs" ""
    let driver = "test/UITests/bin/Debug/net461/chromedriver"
    try
        if Environment.isMacOS && not <| File.Exists driver then
            Shell.rename driver (driver + "_macOS")
        elif Environment.isLinux then
            Shell.rename driver (driver + "_linux64")
    with
    | exn -> failwithf "Could not rename chromedriver at %s. Message: %s" driver exn.Message
)

Target.create "RunClientTests" (fun _ ->

    !! clientTestExecutables
    |> Expecto.run (fun p -> { p with Parallel = false } )
    |> ignore

)

// --------------------------------------------------------------------------------------
// Run the WebServer and WebApp

Target.create "Run" (fun _ ->
    let client = async {
        runTool yarnTool "webpack-dev-server" __SOURCE_DIRECTORY__
    }
    let browser = async {
        do! Async.Sleep 2000
        openBrowser "http://localhost:8888"
    }
    
    let tasks =
        [ if not <| vsCodeSession () then yield browser
          yield client ]

    tasks
    |> Seq.fold asyncCombine asyncNull
    |> Async.RunSynchronously
    |> ignore
)


// --------------------------------------------------------------------------------------
// Test the WebServer and WebApp

Target.create "Test" (fun _ ->

    let client = async {
        runDotnet clientTestsPath "watch run"
    }

    let tasks =
        [ yield client ]

    tasks
    |> Seq.fold asyncCombine asyncNull
    |> Async.RunSynchronously
    |> ignore
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target.create "BundleClient" (fun _ ->
    let clientDir = deployDir
    !! "src/Client/deploy/*.*" |> Shell.copyFiles clientDir
    let fontsDir = clientDir </> "fonts"
    let cssDir = clientDir </> "css"
    !! "src/Client/public/fonts/*.*" |> Shell.copyFiles fontsDir
    !! "src/Client/public/css/*.*" |> Shell.copyFiles cssDir
)

// -------------------------------------------------------------------------------------
open Fake.Core.TargetOperators

// Stage targets.
Target.create "Start" ignore
Target.create "ClientTask" ignore
Target.create "Runnable" ignore
Target.create "Tested" ignore
Target.create "Prepared" ignore

// Intermediate targets.
Target.create "PreClient" ignore

// Final targets.
Target.create "Client" ignore
Target.create "Deploy" ignore
Target.create "All" ignore

"Start"
  ==> "InstallDotNetCore"
  ==> "RestoreClient"
//==> "RestoreClientTests"
  ==> ("Runnable")
  ==> "PreClient"
  ==> "Client"
  ==> ("All")
  ==> ("Prepared")

"ClientTask"
  ==> "BuildClient"
  ==> ("PreClient")
//==> "BuildClientTests"
//==> "RenameDrivers"
//==> "RunClientTests"
  ==> ("Tested")
  ==> "BundleClient"
  ==> ("Client")

"Run" <== [ "Clean"; "Runnable" ]
"Test" <== [ "Runnable" ]
"Deploy" <== [ "Clean"; "Prepared" ]

Target.runOrDefault "All"
