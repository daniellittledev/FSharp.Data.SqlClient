namespace FSharp.Data.SqlClient

open System
open System.IO
//open System.Runtime.Caching

type ChangeMonitor =
   inherit IDisposable
   abstract member NotifyOnChanged : (obj -> unit) -> unit
   abstract member UniqueId : string

[<CompilerMessageAttribute("This API supports the FSharp.Data.SqlClient infrastructure and is not intended to be used directly from your code.", 101, IsHidden = true)>]
type internal SingleFileChangeMonitor(path) as this = 
    //inherit ChangeMonitor()

    let file = new FileInfo(path)
    let watcher = new FileSystemWatcher( Path.GetDirectoryName(path) )

    do
        watcher.NotifyFilter <- NotifyFilters.LastWrite ||| NotifyFilters.FileName
        watcher.Changed.Add <| fun args -> this.TriggerOnFileChange(args.Name)
        watcher.Deleted.Add <| fun args -> this.TriggerOnFileChange(args.Name)
        watcher.Renamed.Add <| fun args -> this.TriggerOnFileChange(args.OldName)
        watcher.Error.Add <| fun _ -> this.TriggerOnChange()
        watcher.EnableRaisingEvents <- true

    let onChangedEvent = new Event<_>()
    
    [<CLIEvent>]
    member this.OnChanged = onChangedEvent.Publish

    member private this.TriggerOnChange() =
        onChangedEvent.Trigger null

    member private __.TriggerOnFileChange(fileName) = 
        if String.Compare(file.Name, fileName, StringComparison.OrdinalIgnoreCase) = 0  
        then 
            this.TriggerOnChange()

    interface ChangeMonitor with
        member __.NotifyOnChanged(callback) =
            this.OnChanged.Add callback

        member __.UniqueId = path + string file.LastWriteTimeUtc.Ticks + string file.Length;

    interface IDisposable with
        member this.Dispose() = watcher.Dispose()
