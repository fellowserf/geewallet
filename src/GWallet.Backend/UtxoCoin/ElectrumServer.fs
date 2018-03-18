﻿namespace GWallet.Backend.UtxoCoin

open System
open System.IO
open System.Linq
open System.Reflection

open FSharp.Data
open FSharp.Data.JsonExtensions

open GWallet.Backend

type internal ElectrumServer =
    {
        Fqdn: string;
        Pruning: string;
        PrivatePort: Option<int>;
        UnencryptedPort: Option<int>;
        Version: string;
    }

module internal ElectrumServerSeedList =

    let private ExtractServerListFromEmbeddedResource resourceName =
        let assembly = Assembly.GetExecutingAssembly()
        let allEmbeddedResources = assembly.GetManifestResourceNames()
        let resourceList = String.Join(",", allEmbeddedResources)
        let assemblyResourceName = allEmbeddedResources.FirstOrDefault(fun r -> r.EndsWith resourceName)
        if (assemblyResourceName = null) then
            failwithf "Embedded resource %s not found in %s. Resource list: %s"
                      resourceName
                      (assembly.ToString())
                      resourceList
        use stream = assembly.GetManifestResourceStream assemblyResourceName
        if (stream = null) then
            failwithf "Assertion failed: Embedded resource %s not found in %s. Resource list: %s"
                      resourceName
                      (assembly.ToString())
                      resourceList
        use reader = new StreamReader(stream)
        let list = reader.ReadToEnd()
        let serversParsed = JsonValue.Parse list
        let servers =
            seq {
                for (key,value) in serversParsed.Properties do
                    let maybeUnencryptedPort = value.TryGetProperty "t"
                    let unencryptedPort =
                        match maybeUnencryptedPort with
                        | None -> None
                        | Some portAsString -> Some (Int32.Parse (portAsString.AsString()))
                    let maybeEncryptedPort = value.TryGetProperty "s"
                    let encryptedPort =
                        match maybeEncryptedPort with
                        | None -> None
                        | Some portAsString -> Some (Int32.Parse (portAsString.AsString()))
                    yield { Fqdn = key;
                            Pruning = value?pruning.AsString();
                            PrivatePort = encryptedPort;
                            UnencryptedPort = unencryptedPort;
                            Version = value?version.AsString(); }
            }
        servers |> List.ofSeq

    let defaultBtcList =
        ExtractServerListFromEmbeddedResource "btc-servers.json"

    let defaultLtcList =
        ExtractServerListFromEmbeddedResource "ltc-servers.json"

    let Randomize currency =
        let serverList =
            match currency with
            | BTC -> defaultBtcList
            | LTC -> defaultLtcList
            | _ -> failwithf "Currency %s is not UTXO" (currency.ToString())
        Shuffler.Unsort serverList