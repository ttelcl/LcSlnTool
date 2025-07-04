// (c) 2025  ttelcl / ttelcl

open System

open CommonTools
open ColorPrint
open ExceptionTool

let rec run arglist =
  // For subcommand based apps, split based on subcommand here
  match arglist with
  | "-v" :: rest ->
    verbose <- true
    rest |> run
  | "--help" :: _
  | "-h" :: _
  | [] ->
    Usage.usage ""
    0  // program return status code to the operating system; 0 == "OK"
  | "sln" :: rest ->
    rest |> AppSln.run
  | x :: _ ->
    cp $"\frUnrecognized subcommand '\fy{x}\fr'\f0."
    cp ""
    Usage.usage ""
    1

[<EntryPoint>]
let main args =
  try
    args |> Array.toList |> run
  with
  | ex ->
    ex |> fancyExceptionPrint verbose
    resetColor ()
    1



