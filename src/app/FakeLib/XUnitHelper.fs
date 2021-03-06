﻿[<AutoOpen>]
/// Contains tasks to run [xUnit](http://xunit.codeplex.com/) unit tests.
module Fake.XUnitHelper

open System
open System.IO
open System.Text

/// The xUnit parameter type
type XUnitParams = { 
      /// The path to the xunit.console.exe - FAKE will scan all subfolders to find it automatically.
      ToolPath: string
      /// The file name of the config file (optional).
      ConfigFile :string
      /// If set to true a HTML output file will be generated.
      HtmlOutput: bool
      /// If set to true a HTML output file will be generated in NUnit format.
      NUnitXmlOutput: bool
      /// If set to true XML output will be generated.
      XmlOutput: bool
      /// The working directory (optional).
      WorkingDir:string
      /// If set to true xUnit will run in ShadowCopy mode.
      ShadowCopy :bool
      /// If set to true xUnit will generate verbose output.
      Verbose:bool
      /// If the timeout is reached the xUnit task will be killed. Default is 5 minutes.
      TimeOut: TimeSpan
      /// The output directory. It's the current directoy if nothing else is specified.
      OutputDir: string }

/// The xUnit default parameters
let XUnitDefaults =
    { ToolPath = findToolInSubPath "xunit.console.exe" (currentDirectory @@ "tools" @@ "xUnit")
      ConfigFile = null;
      HtmlOutput = false;
      NUnitXmlOutput = false;
      WorkingDir = null;
      ShadowCopy = true;
      Verbose = true;
      XmlOutput = false;
      TimeOut = TimeSpan.FromMinutes 5.
      OutputDir = null}

/// Runs xUnit unit tests via the given xUnit runner.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default XUnitParams value.
///  - `assemblies` - Sequence of one or more assemblies containing xUnit unit tests.
/// 
/// ## Sample usage
///
///     Target "Test" (fun _ ->
///         !! (testDir + @"\xUnit.Test.*.dll") 
///           |> xUnit (fun p -> {p with OutputDir = testDir })
///     )
let xUnit setParams assemblies = 
    let details = separated ", " assemblies
    traceStartTask "xUnit" details
    let parameters = setParams XUnitDefaults
    assemblies
      |> Seq.iter (fun assembly ->
          let commandLineBuilder =          
              let fi = fileInfo assembly
              let name = fi.Name

              let dir = 
                if isNullOrEmpty parameters.OutputDir then String.Empty else
                Path.GetFullPath parameters.OutputDir

              new StringBuilder()
                |> appendFileNamesIfNotNull [assembly]
                |> appendIfFalse parameters.ShadowCopy "/noshadow"
                |> appendIfTrue (buildServer = TeamCity) "/teamcity"
                |> appendIfFalse parameters.Verbose "/silent" 
                |> appendIfTrue parameters.XmlOutput (sprintf "/xml\" \"%s%s.xml" dir name) 
                |> appendIfTrue parameters.HtmlOutput (sprintf "/html\" \"%s%s.html" dir name) 
                |> appendIfTrue parameters.NUnitXmlOutput (sprintf "/nunit\" \"%s%s.xml" dir name)                                
      
          if not (execProcess3 (fun info ->  
              info.FileName <- parameters.ToolPath
              info.WorkingDirectory <- parameters.WorkingDir
              info.Arguments <- commandLineBuilder.ToString()) parameters.TimeOut)
          then
              failwith "xUnit test failed.")
                  
    traceEndTask "xUnit" details