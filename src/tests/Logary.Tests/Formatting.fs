﻿module Logary.Tests.Formatting

open System

open Fuchu
open NodaTime

open Logary
open Logary.Formatting

open Logary.Tests.TestDSL

let private sampleMessage : Message =
  { name      = []
    value     = Event "this is bad"
    fields    = Map.empty
    session   = Object Map.empty
    context   = Map [("service", String "a.b.c.d")]
    timestamp = Instant.FromSecondsSinceUnixEpoch(3L).PlusTicks(1234567L).Ticks
    level     = LogLevel.Error }

[<Tests>]
let tests =
  testList "formatting" [
    testCase "StringFormatter.Verbatim" <| fun _ ->
      (because "formatting the message verbatim" <| fun _ ->
        Message.error "hello world"
        |> StringFormatter.Verbatim.format)
      |> should equal "hello world"
      |> thatsIt

    testCase "StringFormatter.VerbatimNewline" <| fun _ ->
      (because "logging verbatim with newline" <| fun () ->
        Message.info "hi there"
        |> StringFormatter.VerbatimNewline.format)
      |> should equal (sprintf "hi there%s" Environment.NewLine)
      |> thatsIt

    testCase "StringFormatter.VerbatimNewlineTemplated" <| fun _ ->
      (because "logging verbatim with newline, templated" <| fun () ->
        {(Message.info "what's {@direction}") with fields = [(["direction"], Field (String "up", None))] |> Map.ofList}
        |> StringFormatter.VerbatimNewline.format)
      |> should equal (sprintf "what's up%s" Environment.NewLine)
      |> thatsIt

    testCase "StringFormatter.LevelDatetimePathMessageNl no exception" <| fun _ ->
      (because "logging with LevelDatetimePathMessageNl" <| fun () ->
        sampleMessage |> StringFormatter.LevelDatetimeMessagePathNl.format)
      |> should equal (
          sprintf "E 1970-01-01T00:00:03.1234567+00:00: this is bad [a.b.c.d]%s"
            Environment.NewLine)
      |> thatsIt

    testCase "StringFormatter.LevelDatetimePathMessageNl no exception, data" <| fun _ ->
      (because "logging with LevelDatetimePathMessageNl" <| fun () ->
        { sampleMessage with fields = [ ["a"], Field (String "b", None); ["a2"], Field (Int64 24L, None) ] |> Map.ofList }
        |> StringFormatter.LevelDatetimeMessagePathNl.format)
      |> should equal (
          sprintf "E 1970-01-01T00:00:03.1234567+00:00: this is bad [a.b.c.d]%s  a => \"b\"%s  a2 => 24%s"
            Environment.NewLine Environment.NewLine Environment.NewLine)
      |> thatsIt

    testCase "StringFormatter.LevelDatetimePathMessageNl no exception, data, list with map in it" <| fun _ ->
      (because "logging with LevelDatetimePathMessageNl" <| fun () ->
        { sampleMessage with
            fields = [ PointName.ofSingle "a", (Field (String "b", None))
                       PointName.ofSingle "a2", (Field (Int64 24L, None))
                       PointName.ofSingle "things",
                         (Field (Array
                           [ Int64 1L
                             Int64 2L
                             Object <| Map ["1", String "hello"] ], None))
                     ] |> Map.ofList
        }
        |> StringFormatter.LevelDatetimeMessagePathNl.format)
      |> should equal (
          String.Format("E 1970-01-01T00:00:03.1234567+00:00: this is bad [a.b.c.d]" +
                        "{0}  a => \"b\"{0}  a2 => 24{0}  things => {0}    - 1{0}    - 2{0}    - {0}      1 => \"hello\"{0}",
                        Environment.NewLine))
      |> thatsIt

    testCase "StringFormatter.LevelDatetimePathMessageNl no exception, nested data" <| fun _ ->
      (because "logging with LevelDatetimePathMessageNl" <| fun () ->
        { sampleMessage with
            fields =
              [ ["a"], (Field (["b", Int64 1L] |> Map.ofList |> Object, None))
                ["c"], (Field (Int64 2L, None))
              ] |> Map.ofList
        }
        |> StringFormatter.LevelDatetimeMessagePathNl.format)
      |> should equal (
          String.Format("E 1970-01-01T00:00:03.1234567+00:00: this is bad [a.b.c.d]" +
                        "{0}  a => {0}    b => 1{0}  c => 2{0}", Environment.NewLine))
      |> thatsIt

    testCase "StringFormatter.LevelDatetimePathMessageNl with exception" <| fun _ ->
      let e = new Exception("Gremlings in the machinery")
      (because "logging with exception attached" <| fun () ->
        sampleMessage
        |> Message.addExn e
        |> StringFormatter.LevelDatetimeMessagePathNl.format)
      |> should equal (
        String.Format("E 1970-01-01T00:00:03.1234567+00:00: this is bad [a.b.c.d]" +
                      "{0}  errors => {0}    - {0}      message => \"{1}\"{0}      type => \"{2}\"{0}",
                      Environment.NewLine, e.Message, (e.GetType ()).FullName))
      |> thatsIt
    testCase "StringFormatter.LevelDatetimePathMessageNl with exception, data" <| fun _ ->
      let e = new Exception("Gremlings in the machinery")
      (because "logging with exception attached" <| fun () ->
        { sampleMessage with fields = [ ["a"], Field (String "b", None); ["a2"], Field (Int64 24L, None) ] |> Map.ofList }
        |> Message.addExn e
        |> StringFormatter.LevelDatetimeMessagePathNl.format)
      |> should equal (
        String.Format("E 1970-01-01T00:00:03.1234567+00:00: this is bad [a.b.c.d]"+
                      "{0}  a => \"b\"{0}  a2 => 24{0}" +
                      "  errors => {0}    - {0}      message => \"{1}\"{0}      type => \"{2}\"{0}",
                      Environment.NewLine, e.Message, (e.GetType ()).FullName))
      |> thatsIt

    testCase "``JsonFormatter has no newline characters``" <| fun _ ->
      (because "logging message with newline in it" <| fun () ->
          { sampleMessage with value = Event "here\n  we\ngo!" } |> JsonFormatter.Default.format)
      |> should equal ("""{"context":{"service":"a.b.c.d"},"level":"error","message":"here\n  we\ngo!",""" +
                       """"timestamp":"1970-01-01T00:00:03.1234567+00:00","type":"event"}""")
      |> thatsIt

    ]

    // TODO: Re-enable after we've figured out a proper place for templateFromFormat

    (*testCase "Formatting.templateFromFormat, simple case" <| fun _ ->
      let format = "This {0} contains {1} words."
      let args : obj[] = [|"sentence"; 4|]
      (because "converting a String.Format into a message template" <| fun () ->
        Formatting.templateFromFormat format args)
      |> should equal ("This {arg0} contains {arg1} words.",
                       [(["arg0"], Field (String "sentence", None)); (["arg1"], Field (Int64 4L, None))])
      |> thatsIt*)