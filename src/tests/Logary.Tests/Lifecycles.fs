﻿module Logary.Tests.Lifecycles

open NodaTime
open Swensen.Unquote
open Fuchu

open Logary.Configuration

open Logary
open Logary.DataModel
open Logary.Logging
open Logary.Targets
open Logary.Metrics

open TestDSL

[<Tests>]
let tests =
  testList "lifecycles" [

    testCase "logary" <| fun _ ->
      Config.confLogary "tests"
      |> Config.validate
      |> Config.runLogary
      |> Config.shutdown
      |> run |> ignore

    testCase "target" <| fun _ ->
      Target.confTarget "tw" (TextWriter.create (TextWriter.TextWriterConf.Create(Fac.textWriter(), Fac.textWriter())))
      |> Target.validate
      |> Target.init Fac.emptyRuntime
      |> Target.send (Message.debug "Hello")
      |> Target.shutdown
      |> run |> ignore

    testCase "metric" <| fun _ ->
      Metric.confMetric
        "no op metric"
        (Duration.FromMilliseconds 500L)
        (Noop.create { Noop.NoopConf.isHappy = true })
      |> Metric.validate
      |> Metric.init Fac.emptyRuntime
      |> Metric.update (Message.metric' ["tests"] (Float 1.0M))
      |> Metric.shutdown
      |> run |> ignore
    ]
