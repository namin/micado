#light

/// Field converters factored out of the schema compiler
module BioStream.Micado.Common.Import.FieldConverters

open System
open System.Collections.Generic

type FieldConverter = string -> obj

type FieldConverterRegistry() =
    let map = new Dictionary<Type, FieldConverter>()
    let registerFieldConverter t f =
        map.Add(t,f)
    member v.Put = registerFieldConverter
    member v.Get t = 
        let ok, res = map.TryGetValue(t)
        if ok then Some res else None

let Registry = new FieldConverterRegistry()
    
Registry.Put (typeof<string>) (fun (s:string) -> box s)
Registry.Put (typeof<int>) (System.Int32.Parse >> box)
Registry.Put (typeof<DateTime>) (System.DateTime.Parse >> box)
    
