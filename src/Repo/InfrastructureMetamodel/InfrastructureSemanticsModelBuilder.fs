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

namespace Repo.InfrastructureMetamodel

open Repo.DataLayer

/// Record with all information required to create an attribute.
[<Struct>]
type AttributeInfo =
    { Name: string;
      Type: IDataNode;
      DefaultValue: IDataNode
    }

/// Class that allows to instantiate new models based on Attribute Metamodel semantics.
type InfrastructureSemanticsModelBuilder 
        (
        repo: IDataRepository, 
        modelName: string, 
        ontologicalMetamodel: IDataModel
        ) =
    let infrastructureSemantics = InfrastructureMetamodelSemantics(repo)
    let elementSemantics = ElementSemantics(repo)
    let infrastructureMetamodel = repo.Model Consts.infrastructureMetametamodel
    let infrastructureMetametamodel = repo.Model Consts.infrastructureMetametamodel

    let metamodelNode = infrastructureMetamodel.Node "Node"
    let metamodelString = infrastructureMetamodel.Node "String"
    let metamodelAssociation = infrastructureMetamodel.Node "Association"
    let metamodelGeneralization = infrastructureMetamodel.Node "Generalization"

    let metametamodelNode = infrastructureMetametamodel.Node "Node"
    let metametamodelAttribute = infrastructureMetametamodel.Node "Attribute"
    let metametamodelSlot = infrastructureMetametamodel.Node "Slot"

    let languageMetamodel = repo.Model "LanguageMetamodel"
    let languageMetamodelNode = languageMetamodel.Node "Node"
    let languageMetamodelGeneralization = languageMetamodel.Node "Generalization"
    let languageMetamodelAssociation = languageMetamodel.Node "Association"
    let languageSemantics = Repo.LanguageMetamodel.LanguageMetamodelSemantics repo

    let model = repo.CreateModel(modelName, ontologicalMetamodel, infrastructureMetamodel)

    /// Helper function that creates a copy of a given edge in a current model (assuming that source and target are 
    /// already in a model).
    let reinstantiateEdge (edge: IDataEdge) =
        if edge.OntologicalType = (languageMetamodelGeneralization :> IDataElement)
            || edge.OntologicalType = (languageMetamodelAssociation :> IDataElement)
        then
            let isExactlyOneWithThisName (elementOption: IDataElement option) = 
                Repo.CoreMetamodel.ModelSemantics.FindNodes model (elementOption.Value :?> IDataNode).Name 
                |> Seq.length = 1
            if isExactlyOneWithThisName edge.Source && isExactlyOneWithThisName edge.Target then
                Repo.CoreMetamodel.CoreSemanticsHelpers.reinstantiateEdge edge model infrastructureMetamodel

    /// Helper function that adds a new attribute to a node.
    let addAttribute (node: IDataNode) (ontologicalType: IDataNode) (defaultValue: IDataNode) (name: string) =
        elementSemantics.AddAttribute node name ontologicalType defaultValue

    /// Helper function that instantiates a new node of a given ontological type hoping that this type does not have
    /// attributes to instantiate.
    let addNodeWithOntologicalType (name: string) (ontologicalType: IDataElement) (attributes: AttributeInfo list) =
        if not (Seq.isEmpty <| elementSemantics.Attributes ontologicalType) then
            failwithf 
                "Can not add node %s with ontological type %s because it has attributes" 
                name 
                (Repo.CoreMetamodel.NodeSemantics.Name ontologicalType)
        
        let newNode = infrastructureSemantics.Instantiate model ontologicalType
        
        attributes 
        |> List.iter (fun attr -> addAttribute (newNode :?> IDataNode) attr.Type attr.DefaultValue attr.Name)
        
        newNode

    /// Helper function that creates a proper instance of a given node in a current model. All instances of Node
    /// will remain instances of Node, all attributes will become the instances of Attribute, all values will become
    /// instances of corresponding type. Infrastructure Metamodel instantiation semantics will apply --- all attributes
    /// will get corresponding slots. But original attributes will be retained since it is reinstantiation.
    let reinstantiateNode (node: IDataNode) =

        let languageElementSemantics = Repo.LanguageMetamodel.ElementSemantics repo

        let addSlot (element: IDataElement) (attribute: IDataNode) (value: IDataNode) =
            let model = Repo.CoreMetamodel.ElementSemantics.ContainingModel element
            let slotNode = model.CreateNode(attribute.Name, attribute, metametamodelSlot)
            let attributeAssociation = attribute.IncomingEdges |> Seq.exactlyOne
            let attributeTypeAssociation = Repo.CoreMetamodel.ElementSemantics.OutgoingAssociation attribute "type"

            let slotAssociation = infrastructureMetametamodel.Association "slots"
            let slotValueAssociation = infrastructureMetametamodel.Association "value"

            model.CreateAssociation
                    (
                    attributeAssociation,
                    slotAssociation,
                    element,
                    slotNode,
                    attribute.Name
                    ) |> ignore

            model.CreateAssociation
                    (
                    attributeTypeAssociation,
                    slotValueAssociation,
                    slotNode,
                    value,
                    "value"
                    ) |> ignore

        let instantiateAttribute element ontologicalType name value =
            if languageElementSemantics.HasAttribute ontologicalType name then
                let attribute = languageElementSemantics.Attribute ontologicalType name
                addSlot element attribute value
            else
                failwithf "Invalid attribute reinstantiation, node: %s, attribute: %s" (node.ToString ()) name

        let instantiateAttributes (element: IDataElement) (ontologicalType: IDataElement) attributeValues =
            attributeValues |> Map.iter (instantiateAttribute element ontologicalType)

        let instantiateNode
                (name: string)
                (ontologicalType: IDataNode)
                (attributeValues: Map<string, IDataNode>) =
            let instance = model.CreateNode(name, ontologicalType, metametamodelNode)
            instantiateAttributes instance ontologicalType attributeValues
            instance

        if node.OntologicalType = (languageMetamodelNode :> IDataElement) then
            let toAttributeInfo attribute =
                { Name = Repo.AttributeMetamodel.AttributeSemantics.Name attribute;
                  Type = Repo.AttributeMetamodel.AttributeSemantics.Type attribute;
                  DefaultValue = Repo.AttributeMetamodel.AttributeSemantics.DefaultValue attribute}

            let attributes = languageElementSemantics.Attributes metametamodelNode
            let slots = 
                attributes
                |> Seq.map toAttributeInfo
                |> Seq.map 
                    (fun attr -> 
                        (attr.Name, model.CreateNode(attr.DefaultValue.Name, attr.Type, metametamodelSlot))
                     )
                |> Map.ofSeq

            let instance = instantiateNode node.Name metametamodelNode slots

            languageElementSemantics.OwnAttributes node
            |> Seq.iter 
                (fun attr ->
                    elementSemantics.AddAttribute 
                        instance
                        (Repo.AttributeMetamodel.AttributeSemantics.Name attr)
                        (Repo.AttributeMetamodel.AttributeSemantics.Type attr)
                        (Repo.AttributeMetamodel.AttributeSemantics.DefaultValue attr)
                )
            ()

    /// Creates a new model in existing repository with Attribute Metamodel as its metamodel.
    new (repo: IDataRepository, modelName: string) =
        InfrastructureSemanticsModelBuilder(repo, modelName, repo.Model Consts.infrastructureMetametamodel)

    /// Creates a new repository with Core Metamodel and creates a new model in it.
    new (modelName: string) =
        let repo = DataRepo() :> IDataRepository
        let (~+) (builder: IModelCreator) = builder.CreateIn repo

        +Repo.CoreMetamodel.CoreMetamodelCreator()
        +Repo.AttributeMetamodel.AttributeMetamodelCreator()
        +Repo.LanguageMetamodel.LanguageMetamodelCreator()
        +InfrastructureMetametamodelCreator()

        InfrastructureSemanticsModelBuilder(repo, modelName, repo.Model Consts.infrastructureMetametamodel)

    /// Adds a node (an instance of InfrastructureMetamodel.Node) with given attributes 
    /// (of type InfrastructureMetamodel.String) into a model.
    member this.AddNode (name: string) (attributes: AttributeInfo list) =
        addNodeWithOntologicalType name metamodelNode attributes :?> IDataNode

    /// Instantiates a node of a given class into a model. Uses provided list to instantiate attributes into slots.
    member this.InstantiateNode
            (name: string)
            (ontologicalType: IDataNode)
            (slotsList: List<string * string>) =
        let attributeValues = 
            Map.ofList slotsList
            |> Map.map (fun _ value -> model.CreateNode(value, metamodelString, metamodelString))
        let node = infrastructureSemantics.Instantiate model ontologicalType // attributeValues
        node :?> IDataNode
    
    (*
    /// Adds a new attribute to a node with AttributeMetamodel.String as a type.
    member this.AddAttribute (node: IDataNode) (name: string) =
        if not <| AttributeSemanticsHelpers.isAttributeAddingPossible elementSemantics node name stringNode then
            failwith <| "Attribute is already present in an element (including its generalization hierarchy) and has "
                + "different type"
        addAttribute node stringNode emptyString name 

    /// Adds a new attribute with given type to a node.
    member this.AddAttributeWithType 
            (node: IDataNode) 
            (ontologicalType: IDataNode) 
            (defaultValue: IDataNode) 
            (name: string) =
        addAttribute node ontologicalType defaultValue name
        *)
    /// Adds an association between two elements. Association will be an instance of an AttributeMetamodel.Association
    /// node.
    member this.AddAssociation (source: IDataElement) (target: IDataElement) (name: string) =
        model.CreateAssociation(metamodelAssociation, metamodelAssociation, source, target, name)

    /// Adds an association between two elements. Association will be an instance of 
    /// an AttributeMetamodel.Generalization node.
    member this.AddGeneralization (child: IDataNode) (parent: IDataNode) =
        model.CreateGeneralization(metamodelGeneralization, metamodelGeneralization, child, parent) |> ignore
        (*
    /// Instantiates an association between two given elements using supplied association class as a type of 
    /// resulting edge.
    member this.InstantiateAssociation 
            (source: IDataNode) 
            (target: IDataNode) 
            (ontologicalType: IDataElement) 
            (slotsList: List<string * string>) =
        let slots = 
            Map.ofList slotsList
            |> Map.map (fun _ value -> model.CreateNode(value, stringNode, stringNode))
        attributeSemantics.InstantiateAssociation model source target ontologicalType slots
    *)
    /// Creates model builder that uses Attribute Metamodel semantics and has current model as its
    /// ontological metamodel and Attribute Metamodel as its linguistic metamodel. So instantiations will use 
    /// this model for instantiated classes, but Node, String and Association will be from Attribute Metamodel.
    member this.CreateInstanceModelBuilder (name: string) =
        InfrastructureSemanticsModelBuilder(repo, name, model)

    /// Returns model which this builder builds.
    member this.Model with get () = model

    /// Returns repository in which the model is being built.
    member this.Repo with get () = repo

    /// Helper operator that adds a linguistic extension node to a model.
    static member (+) (builder: InfrastructureSemanticsModelBuilder, name) = builder.AddNode name []

    /// Helper operator that adds a generalization between given elements to a model.
    static member (+--|>) (builder: InfrastructureSemanticsModelBuilder, (source, target)) = 
        builder.AddGeneralization source target

    /// Helper operator that adds an association between given elements to a model.
    static member (+--->) (builder: InfrastructureSemanticsModelBuilder, (source, target, name)) = 
        builder.AddAssociation source target name |> ignore

    /// Instantiates an exact copy of Infrastructure Metametamodel in a current model. Supposed to be used to 
    /// reintroduce metatypes at a new metalevel.
    member this.ReinstantiateInfrastructureMetametamodel () =
        infrastructureMetametamodel.Nodes |> Seq.iter reinstantiateNode
        infrastructureMetametamodel.Edges |> Seq.iter reinstantiateEdge
        ()

    /// Returns node in current model by name, if it exists.
    member this.Node name =
        model.Node name

    /// Returns node in ontological metamodel by name, if it exists.
    member this.MetamodelNode name =
        ontologicalMetamodel.Node name

    /// Returns association from current model by name, if it exists.
    member this.Association name =
        model.Association name

    /// Returns association from ontological metamodel by name, if it exists.
    member this.MetamodelAssociation name =
        ontologicalMetamodel.Association name

    /// Returns a node for Boolean type in Infrastructure metamodel.
    member this.Boolean =
        infrastructureMetamodel.Node Consts.boolean

    /// Returns a node for Int type in Infrastructure metamodel.
    member this.Int =
        infrastructureMetamodel.Node Consts.int

    /// Returns a node for String type in Infrastructure metamodel.
    member this.String =
        infrastructureMetamodel.Node Consts.string
