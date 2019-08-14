module Library

type prop = 
    // does useless stuff
    static member f() = 
      [ 1 .. 100 ]
      |> List.map (fun x -> x * 20)
      |> List.collect (fun n -> [n; n])
      |> List.fold (+) 0

    // does useless stuff
    static member inline k() = 
      [ 1 .. 100 ]
      |> List.map (fun x -> x * 20)
      |> List.collect (fun n -> [n; n])
      |> List.fold (+) 0


type AppMain =
    static member g() = 1
