﻿(* Copyright 2017 Yurii Litvinov
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. *)

namespace Repo.DataLayer

/// Implementation of model interface in data layer. Contains nodes and edges in list, implements
/// CRUD operations and keeps consistency.
type DataModel private 
        (
        name: string, 
        ontologicalMetamodel: IDataModel option, 
        linguisticMetamodel: IDataModel option
        ) =

    let mutable nodes = []
    let mutable edges = []

    let mutable properties = Map.empty

    /// Helper function to choose only associatinons from sequence of edges.
    let chooseAssociation: IDataEdge -> IDataAssociation option = 
        function
        | :? IDataAssociation as a -> Some a
        | _ -> None

    override this.ToString () = name

    new(name: string) = DataModel(name, None, None)
    new(name: string, ontologicalMetamodel: IDataModel, linguisticMetamodel: IDataModel) = 
        DataModel(name, Some ontologicalMetamodel, Some linguisticMetamodel)

    interface IDataModel with
        member this.CreateNode name =
            let node = DataNode(name, this) :> IDataNode
            nodes <- node :: nodes
            node

        member this.CreateNode(name, (ontologicalType: IDataElement), (linguisticType: IDataElement)) =
            let node = DataNode(name, ontologicalType, linguisticType, this) :> IDataNode
            nodes <- node :: nodes
            node

        member this.CreateAssociation(ontologicalType, linguisticType, source, target, targetName) =
            let edge = 
                new DataAssociation(ontologicalType, linguisticType, source, target, targetName, this) 
                :> IDataAssociation
            edges <- (edge :> IDataEdge) :: edges
            if source.IsSome then
                source.Value.AddOutgoingEdge edge
            if target.IsSome then
                target.Value.AddIncomingEdge edge
            edge

        member this.CreateAssociation(ontologicalType, linguisticType, source, target, targetName) =
            let edge = 
                new DataAssociation(ontologicalType, linguisticType, Some source, Some target, targetName, this) 
                :> IDataAssociation
            edges <- (edge :> IDataEdge) :: edges
            source.AddOutgoingEdge edge
            target.AddIncomingEdge edge
            edge

        member this.CreateGeneralization(ontologicalType, linguisticType, source, target) =
            let edge = 
                new DataGeneralization(ontologicalType, linguisticType, source, target, this) 
                :> IDataGeneralization
            if source.IsSome then
                source.Value.AddOutgoingEdge edge
            if target.IsSome then
                target.Value.AddIncomingEdge edge
            edges <- (edge :> IDataEdge) :: edges
            edge

        member this.CreateGeneralization(ontologicalType, linguisticType, source, target) =
            let edge = 
                new DataGeneralization(ontologicalType, linguisticType, Some source, Some target, this) 
                :> IDataGeneralization
            source.AddOutgoingEdge edge
            target.AddIncomingEdge edge
            edges <- (edge :> IDataEdge) :: edges
            edge

        member this.DeleteElement(element: IDataElement): unit =
            match element with
            | :? IDataNode as n ->
                nodes <- nodes |> List.except [n]
            | :? IDataEdge as e -> 
                edges <- edges |> List.except [e]
                
                match e.Source with
                | Some element -> element.DeleteOutgoingEdge e
                | _ -> ()

                match e.Target with
                | Some element -> element.DeleteIncomingEdge e
                | _ -> ()

            | _ -> failwith "Unknown descendant of IDataElement"

            edges |> List.iter (fun e ->
                if e.Source = Some element then e.Source <- None
                if e.Target = Some element then e.Target <- None
                )

        member this.Elements: IDataElement seq =
            let nodes = (nodes |> Seq.cast<IDataElement>)
            let edges = (edges |> Seq.cast<IDataElement>)
            Seq.append nodes edges

        member this.OntologicalMetamodel
            with get(): IDataModel =
                match ontologicalMetamodel with
                | Some v -> v
                | None -> this :> IDataModel

        member this.LinguisticMetamodel
            with get(): IDataModel =
                match linguisticMetamodel with
                | Some v -> v
                | None -> this :> IDataModel

        member val Name = name with get, set

        member this.Nodes: seq<IDataNode> =
            nodes |> Seq.ofList

        member this.Edges: seq<IDataEdge> =
            edges |> Seq.ofList
        
        member this.Node (name: string): IDataNode = 
            let filtered = nodes |> List.filter (fun x -> x.Name = name)
            match filtered with
            | [] -> raise (Repo.ElementNotFoundException name)
            | _::_::_ -> raise (Repo.MultipleElementsException name)
            | [x] -> x

        member this.HasNode (name: string): bool =
            nodes |> List.exists (fun x -> x.Name = name)

        member this.Properties
            with get () = properties
            and set v = properties <- v

        member this.Association (name: string): IDataAssociation =
            Repo.Helpers.getExactlyOne 
                (edges |> Seq.choose chooseAssociation)
                (fun a -> a.TargetName = name)
                (fun () -> Repo.ElementNotFoundException name)
                (fun () -> Repo.MultipleElementsException name)

        member this.HasAssociation (name: string) =
            edges |> Seq.choose chooseAssociation |> Seq.tryFind (fun a -> a.TargetName = name) |> Option.isSome

        member this.PrintContents () =
            printfn "%s:" <| this.ToString ()
            printfn "Nodes:"
            nodes 
                |> List.map (fun n -> n.ToString())
                |> List.iter (printfn "    %s")
            printfn ""
            printfn "Edges:"
            edges 
                |> List.map (fun n -> n.ToString())
                |> List.iter (printfn "    %s")
                