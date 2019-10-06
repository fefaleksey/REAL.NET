﻿(* Copyright 2019 Yurii Litvinov
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

namespace Repo.CoreMetamodel

open Repo
open Repo.CoreMetamodel
open Repo.CoreMetamodel.Details
open Repo.CoreMetamodel.Details.Elements

/// Factory that creates core metamodel repository.
[<AbstractClass; Sealed>]
type RepoFactory =
    /// Method that returns repository with Core Metamodel.
    static member Create() = 
        let basicRepo = BasicMetamodel.RepoFactory.Create ()
        let factory = CoreFactory(basicRepo)
        let pool = CorePool(factory)
        let repo = CoreRepository(pool, basicRepo)
        CoreMetametamodelCreator.createIn repo
        CoreMetamodelCreator.createIn repo
        repo :> ICoreRepository
