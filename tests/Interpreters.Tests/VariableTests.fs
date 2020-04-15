﻿module VariableTests

open NUnit.Framework
open FsUnit

open Interpreters

// Regular type & module tests.
[<Test>]
let ``isTypesEqual for RegularType should be correct``() =
    RegularType.isTypesEqual (Int 1) (Int 2) |> should be True    
    RegularType.isTypesEqual (Double 1.0) (Double 2.0) |> should be True   
    RegularType.isTypesEqual (Bool true) (Bool false) |> should be True
    RegularType.isTypesEqual (Int 1) (Double 1.0) |> should be False

// VariableValue type & module tests.
[<Test>]
let ``isTypesEqual for VariableValue should be correct``() =
    VariableValue.isTypesEqual (RegularValue (Int 1)) (RegularValue (Int 2)) |> should be True
    VariableValue.isTypesEqual (RegularValue (Int 1)) (RegularValue (Double 1.0)) |> should be False

[<Test>]
let ``isRegular should be correct``() =
    VariableValue.isRegular (RegularValue (Int 1)) |> should be True
    VariableValue.isRegular (RegularValue (Double 1.0)) |> should be True

// Variable type & module tests. 
[<Test>]
let ``isTypesEqual for Variable sould be correct``() =
    let doubleVar1 = Variable.createDouble "doubleVar1" 1.0 (None, None)
    let doubleVar2 = Variable.createDouble "doubleVar2" 2.0 (None, None)
    let intVar = Variable.createInt "intVar" 1 (None, None)
    Variable.isTypesEqual doubleVar1 doubleVar2 |> should be True
    Variable.isTypesEqual doubleVar1 intVar |> should be False
    
