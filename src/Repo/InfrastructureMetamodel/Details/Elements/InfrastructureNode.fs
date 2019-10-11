﻿(* Copyright 2019 REAL.NET group
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

namespace Repo.InfrastructureMetamodel.Details.Elements

open Repo.InfrastructureMetamodel
open Repo.LanguageMetamodel

/// Implementation of Node.
type InfrastructureNode(node: ILanguageNode, pool: InfrastructurePool, repo: ILanguageRepository) =
    inherit InfrastructureElement(node, pool, repo)
    interface IInfrastructureNode with
        member this.Name
            with get () = node.Name
            and set v = node.Name <- v