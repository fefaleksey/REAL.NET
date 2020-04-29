﻿module Interpreters.Logo.LogoParser

open Interpreters.Parser

open Interpreters.Logo.TurtleCommand

open Repo

open Interpreters
open Interpreters.Expressions
open Repo.FacadeLayer

type Context = { Commands: LCommand list} 

module Context =
    let createContext = {Commands = []}

module private Helper =
    let findAttributeValueByName (element: IElement) (name: string) =
        ElementHelper.getAttributeValue name element

    let distanceName = "Distance"
    
    let degreesName = "Degrees"

    let tryExprToDouble env expr =
        let parser = StringExpressionParser.Create()
        match parser.TryParse env expr with
        | Ok (_, v) -> match TypeConversion.tryDouble v with
                       | Some x -> Ok x
                       | _ ->
                           match TypeConversion.tryInt v with
                           | Some x -> (Ok (double x))
                           | _ -> TypeException("Type is not double") :> exn |> Error
        | Error e -> Error e
    
    let tryExprToInt env expr =
        let parser = StringExpressionParser.Create()
        match parser.TryParse env expr with
        | Ok (_, v) -> match TypeConversion.tryInt v with
                       | Some x -> Ok x
                       | _ -> TypeException("Type is not int") :> exn |> Error
        | Error e -> Error e
        
    let findAllEdgesFrom (model: IModel) (element: IElement) = ElementHelper.outgoingEdges model element
        
    let next (model: IModel) (element: IElement) = ElementHelper.next model element
    
    let findAllEdgesTo (model: IModel) (element: IElement) = ElementHelper.incomingEdges model element      
    
    let hasAttribute name (element: IElement) = ElementHelper.hasAttribute name element
    
module AvailableParsers =

    let parseInitialNode (parsing: Parsing<_> Option) =
        match parsing with
        | None -> None
        | Some ({ Model = model; Element = element } as p) ->
            if (element.Class.Name = "InitialNode") then
                match ElementHelper.tryNext model element with
                | None -> ParserException.raiseWithPlace "Can't determine next element from initial node" (PlaceOfCreation(Some model, Some element))
                | Some nextElement -> Some { p with Element = nextElement }
            else None
            
    
    let parseForward (parsing: Parsing<Context> option) : Parsing<Context> option =
        match parsing with
        | None -> None
        | Some {Variables = set; Context = context; Model = model; Element = element} -> 
            if (element.Class.Name = "Forward") 
                then let distanceString = Helper.findAttributeValueByName element Helper.distanceName
                     let env = EnvironmentOfExpressions.initWithSetAndPlace set (PlaceOfCreation(Some model, Some element))
                     let distanceRes = distanceString |> Helper.tryExprToDouble env 
                     match distanceRes with
                     | Ok distance ->
                         let command = LForward distance
                         let newContext = {context with Commands = command :: context.Commands}
                         let edges = Helper.findAllEdgesFrom model element
                         if Seq.length edges > 1 then None
                         else let edge = Seq.exactlyOne edges
                              let parsed = {Variables = set; Context = newContext; Model = model; Element = edge.To} 
                              parsed |> Some
                     | Error e -> ParserException(e.Message, PlaceOfCreation(Some model, Some element), e) |> raise
                else None

    let parseBackward (parsing: Parsing<Context> option) : Parsing<Context> option =
        match parsing with
        | None -> None
        | Some {Variables = set; Context = context; Model = model; Element = element} -> 
            if (element.Class.Name = "Backward") 
                then let distanceString = Helper.findAttributeValueByName element Helper.distanceName
                     let env = EnvironmentOfExpressions.initWithSetAndPlace set (PlaceOfCreation(Some model, Some element))
                     let distanceRes = distanceString |> Helper.tryExprToDouble env
                     match distanceRes with
                     | Ok distance ->
                         let command = LBackward distance
                         let newContext = {context with Commands = command :: context.Commands}
                         let edges = Helper.findAllEdgesFrom model element
                         if Seq.length edges > 1 then None
                         else let edge = Seq.exactlyOne edges
                              let parsed = {Variables = set; Context = newContext; Model = model; Element = edge.To} 
                              parsed |> Some
                     | Error e -> ParserException.raiseAll e.Message (PlaceOfCreation(Some model, Some element)) e
                else None

    let parseRight (parsing: Parsing<Context> option) : Parsing<Context> option =
        match parsing with
        | None -> None
        | Some {Variables = set; Context = context; Model = model; Element = element} -> 
            if (element.Class.Name = "Right")
                then let degreesString = Helper.findAttributeValueByName element Helper.degreesName
                     let env = EnvironmentOfExpressions.initWithSetAndPlace set (PlaceOfCreation(Some model, Some element))
                     let degreesRes = degreesString |> Helper.tryExprToDouble env 
                     match degreesRes with
                     | Ok degrees ->
                         let command = LRight degrees
                         let newContext = {context with Commands = command :: context.Commands}
                         let edges = Helper.findAllEdgesFrom model element
                         if Seq.length edges > 1 then None
                         else let edge = Seq.exactlyOne edges
                              let parsed = {Variables = set; Context = newContext; Model = model; Element = edge.To} 
                              parsed |> Some
                     | Error e -> ParserException.raiseAll e.Message (PlaceOfCreation(Some model, Some element)) e
            else None
    
    let parseLeft (parsing: Parsing<Context> option) : Parsing<Context> option =
        match parsing with
        | None -> None
        | Some {Variables = set; Context = context; Model = model; Element = element} -> 
            if (element.Class.Name = "Left")
                then let degreesString = Helper.findAttributeValueByName element Helper.degreesName
                     let env = EnvironmentOfExpressions.initWithSetAndPlace set (PlaceOfCreation(Some model, Some element))
                     let degreesRes = degreesString |> Helper.tryExprToDouble env 
                     match degreesRes with
                     | Ok degrees ->
                         let command = LLeft degrees
                         let newContext = {context with Commands = command :: context.Commands}
                         let edges = Helper.findAllEdgesFrom model element
                         if Seq.length edges > 1 then None
                         else let edge = Seq.exactlyOne edges
                              let parsed = {Variables = set; Context = newContext; Model = model; Element = edge.To} 
                              parsed |> Some
                     | Error e -> ParserException.raiseAll e.Message (PlaceOfCreation(Some model, Some element)) e 
            else None

    let parseRepeat (parsing: Parsing<Context> option) : Parsing<Context> option =
        match parsing with
        | None -> None
        | Some ({Variables = set; Context = context; Model = model; Element = element} as p) ->
            if (element.Class.Name = "Repeat") then
                let filter (var: Variable) =
                    match var.Meta.PlaceOfCreation with
                    | PlaceOfCreation(_, Some e) when e = element -> true
                    | _ -> false
                let edges = Helper.findAllEdgesFrom model element
                let exitOption = edges |> Seq.filter (Helper.hasAttribute "Tag") |> Seq.tryExactlyOne
                match exitOption with
                    | None -> ParserException.raiseWithPlace "No exit found" (PlaceOfCreation(Some model, Some element))
                    | Some exitEdge ->
                        let exit = exitEdge.To
                        let nextElementOption = edges |> Seq.except [exitEdge] |> Seq.tryExactlyOne
                        match nextElementOption with
                        | None -> ParserException.raiseWithPlace "No next element found" (PlaceOfCreation(Some model, Some element))
                        | Some nextElementEdge ->
                            let nextElement = nextElementEdge.To
                            let vars = set.Filter filter
                            if vars.IsEmpty then
                                let countString = Helper.findAttributeValueByName element "Count"
                                let env = EnvironmentOfExpressions.initWithSetAndPlace set (PlaceOfCreation(Some model, Some element))
                                let countRes = countString |> Helper.tryExprToInt env 
                                match countRes with
                                | Ok count ->
                                    if (count = 0) then
                                        Some {p with Element = exit}
                                    else
                                        let i = Variable.createInt "repeatI" count (Some model, Some element)
                                        let newSet = set.Add i
                                        Some {p with Variables = newSet; Element = nextElement}
                                | Error e -> ParserException.raiseAll e.Message (PlaceOfCreation(Some model, Some element)) e
                            else
                                let countVarOption = vars |> Seq.filter (fun x -> x.Name = "repeatI") |> Seq.tryExactlyOne
                                match countVarOption with
                                | None -> ParserException.raiseWithPlace "No correct count variable found" (PlaceOfCreation(Some model, Some element))
                                | Some ({Value = value} as countVar) ->
                                    match value with
                                    | RegularValue (Int intVal) ->
                                        if (intVal = 1) then
                                            let newSet = set.Remove countVar
                                            Some {p with Element = exit; Variables = newSet}
                                        else
                                            let newVal = ExpressionValue.createInt (intVal - 1)
                                            let newSet = set.ChangeValue countVar newVal
                                            Some {p with Element = nextElement; Variables = newSet}
                                    | _ -> None
                else None
                
    let parseExpression = Interpreters.Parser.AvailableParsers.parseExpression
                        
open AvailableParsers

let parseMovement: Parser<Context> = parseForward >>+ parseRight >>+ parseBackward >>+ parseLeft

let parseLogo = parseInitialNode >>+ parseMovement >>+ parseRepeat >>+ parseExpression