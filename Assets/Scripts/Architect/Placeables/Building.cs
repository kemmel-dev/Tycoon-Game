using System;
using System.Collections.Generic;
using Agency;
using Architect.Placeables;
using Architect.Placeables.Presets;
using Buildings.Resources;
using Grid;
using UI;
using UnityEngine;
using static Grid.GridManager;

public class Building : Placeable
{
    public Storage Input;
    public Storage Output;
    private int _range;

    public Resource[] ProductionCost;
    public Resource[] Produces;

    public BuildingPreset LocalPreset { get; private set; }

    public Tile Tile { get; set; }

    public BuildingConnectionsRenderer BuildingConnectionsRenderer { get; protected set; }


    /// <summary>
    /// List of providers that this building imports resources from.
    /// </summary>
    public List<Building> providers = new();

    /// <summary>
    /// List of recipients that this building exports to.
    /// </summary>
    public List<Building> recipients = new();

    private AgentSpawner _agentSpawner;

    /// <summary>
    /// Creates a new building with a given BuildingPreset.
    /// </summary>
    /// <param name="preset">The preset for this building.</param>
    public Building(BuildingPreset preset)
    {
        Preset = preset;
        LocalPreset = preset;
        Input = new Storage(preset.initialStorage);
        Output = new Storage(Array.Empty<Resource>());
        ProductionCost = LocalPreset.productionCost;
        Produces = LocalPreset.produces;
        _range = LocalPreset.range;
    }

    public virtual void InitializeAfterInstantiation(Tile hostingTile)
    {
        Tile = hostingTile;
        bool producesItems = Produces.Length > 0;
        Buildings.BuildingController.SubscribeBuilding(this, producesItems, producesItems);
        _agentSpawner = Tile.PlaceableHolder.GetComponentInChildren<AgentSpawner>();
        BuildingConnectionsRenderer =
            Tile.transform.Find("Recipient Lines").GetComponent<BuildingConnectionsRenderer>();
    }

    public void RefreshRecipients()
    {
        Debug.Log(Tile.transform.name);
        this.recipients = new();

        //Check if there is a road in the surrounding tiles
        Neighbour[] neighbours = GridManager.Instance.GetNeighboursFor(Tile);
        bool hasRoadNeighbour = false;
        foreach (Neighbour neighbour in neighbours)
        {
            if (neighbour.Tile.Content is Road)
            {
                hasRoadNeighbour = true;
                break;
            }
        }

        if (!hasRoadNeighbour) return;

        //Get all the buildings in range
        Building[] recipientsInRange = GetBuildingsInRange();
        foreach (Building recipient in recipientsInRange)
        {
            InsertAtFrontOfQueue(recipient);
            recipient.AddProvider(this);
        }

        BuildingConnectionsRenderer.SetRecipients(recipientsInRange);
    }

    internal void OnDeselect()
    {
        throw new NotImplementedException();
    }

    private void AddProvider(Building building)
    {
        if (!providers.Contains(building))
        {
            providers.Add(building);
        }

        BuildingConnectionsRenderer.SetProviders(providers.ToArray());
    }

    private void RemoveProvider(Building building)
    {
        providers.Remove(building);
        BuildingConnectionsRenderer.SetProviders(providers.ToArray());
    }

    private Building GetClosestBuilding()
    {
        Building[] buildings = GetBuildingsInRange();
        float minDistance = float.PositiveInfinity;
        int maxDistanceIndex = -1;
        for (int i = 0; i < buildings.Length; i++)
        {
            float currentDistance = Vector3.Distance(buildings[i].Tile.PlaceableHolder.transform.position,
                Tile.PlaceableHolder.transform.position);
            if (currentDistance < minDistance)
            {
                minDistance = currentDistance;
                maxDistanceIndex = i;
            }
        }

        if (maxDistanceIndex < 0)
        {
            return null;
        }

        return buildings[maxDistanceIndex];
    }

    private Building[] GetBuildingsInRange()
    {
        Collider[] overlappedColliders =
            Physics.OverlapSphere(Tile.PlaceableHolder.position, _range, LayerMask.GetMask("Building"));
        List<(Building building, float dist)> buildingsByDistance = new();
        foreach (Collider overlap in overlappedColliders)
        {
            Building other = overlap.GetComponent<Tile>().Content as Building;
            if (other == this)
                continue;

            bool addedBuildings = false;
            foreach (Resource needed in other.ProductionCost)
            {
                if (addedBuildings)
                    break;
                foreach (Resource resource in Produces)
                {
                    if (needed.type == resource.type)
                    {
                        buildingsByDistance.Add((other,
                            Vector3.Distance(other.Tile.PlaceableHolder.transform.position,
                                Tile.PlaceableHolder.transform.position)));
                        addedBuildings = true;
                        break;
                    }
                }
            }
        }

        buildingsByDistance.Sort(new BuildingByDistanceComparer());

        Building[] buildings = new Building[buildingsByDistance.Count];
        for (int i = 0; i < buildings.Length; i++)
        {
            buildings[i] = buildingsByDistance[i].building;
        }

        return buildings;
    }

    /// <summary>
    /// Add recipient to this building
    /// </summary>
    /// <param name="building">the recipient to add.</param>
    public void InsertAtFrontOfQueue(Building building)
    {
        recipients.Insert(0, building);
    }

    /// <summary>
    /// Enqueue recipient add end of list
    /// </summary>
    /// <param name="recipient">the recipient to add.</param>
    public void EnqueueRecipient(Building recipient)
    {
        recipients.Add(recipient);
    }

    /// <summary>
    /// Dequeue recipient.
    /// </summary>
    public Building DequeueRecipient()
    {
        Building firstInQueue = recipients[0];
        recipients.RemoveAt(0);
        return firstInQueue;
    }

    /// <summary>
    /// Production cycle for this building.
    /// </summary>
    public void Produce()
    {
        if (Buildings.BuildingController.Tick % LocalPreset.productionTime == 0)
        {
            Fabricate();
        }
    }

    /// <summary>
    /// Fabricates the resources from productionCost into the resources from produces.
    /// </summary>
    protected virtual void Fabricate()
    {
        if (!Input.HasResourcesRequired(ProductionCost))
        {
            //Debug.Log("Did not have enough resources to produce!");
            return;
        }

        RemoveFromStorage(Input, ProductionCost);
        AddToStorage(Output, Produces);
    }

    public void AddToStorage(Storage storage, Resource[] resources)
    {
        foreach (Resource resource in resources)
        {
            storage.Add(resource);
        }
    }

    public void RemoveFromStorage(Storage storage, Resource[] resources)
    {
        foreach (Resource resource in resources)
        {
            storage.Remove(resource);
        }
    }

    public void Transport()
    {
        if (Buildings.BuildingController.Tick % LocalPreset.transportTime == 0)
        {
            TransportToRecipients();
        }
    }

    private void TransportToRecipients()
    {
        if (recipients.Count > 0)
        {
            Building recipient = DequeueRecipient();
            List<Resource> resourcesToSend = new();
            // For each requested resource
            foreach (Resource requestedResource in recipient.ProductionCost)
            {
                // If this building's output contains that requested resource
                if (Output.HasResourceRequired(requestedResource))
                {
                    resourcesToSend.Add(requestedResource);
                }
                else
                {
                    // Did not have that resource
                }
            }

            // Send resources that were requested by recipient
            Resource[] resourcesToSendArray = resourcesToSend.ToArray();

            if (recipient is ConstructionSite c && c.isReceiving)
            {
                TransportToRecipients();
                return;
            }

            if (resourcesToSendArray.Length == 0)
            {
                InsertAtFrontOfQueue(recipient);
                return;
            }
            //Check if it can spawn an agent

            //TODO: why is this nullcheck nessecary
            if (_agentSpawner == null)
            {
                _agentSpawner = Tile.PlaceableHolder.GetComponentInChildren<AgentSpawner>();
            }

            AgentBehaviour agent = _agentSpawner.SpawnAgent();
            if (agent != null)
            {
                RemoveFromStorage(Output, resourcesToSendArray);
                (agent as DeliveryAgent).SetDeliveryTarget(resourcesToSendArray, recipient);
                //(agent as DeliveryAgent).AddTarget(this);
                if (recipient is ConstructionSite c2) c2.isReceiving = true;
            }

            // Put recipient back into queue
            EnqueueRecipient(recipient);
        }
    }

    public override void OnDestroy()
    {
        Buildings.BuildingController.UnsubscribeBuilding(this);
        providers.Clear();
        BuildingConnectionsRenderer.SetProviders(providers.ToArray());
        foreach (Building recipient in recipients)
        {
            recipient.RemoveProvider(this);
        }

        BuildingConnectionsRenderer.ShowLines(false);
        GameObject.Destroy(_agentSpawner);
    }


    public class BuildingByDistanceComparer : Comparer<(Building val, float dist)>
    {
        public override int Compare((Building val, float dist) a, (Building val, float dist) b)
        {
            return (a.dist > b.dist) ? 1 : (a.dist == b.dist) ? 0 : -1;
        }
    }


    /// <summary>
    /// A storage holds a collection of resources.
    /// </summary>
    [Serializable]
    public class Storage
    {
        public Dictionary<ResourceType, int> Contents { get; set; } = new();

        /// <summary>
        /// Creates a storage, and adds the contents of initialStorage to it.
        /// </summary>
        /// <param name="initialStorage">The initial resources in this storage.</param>
        public Storage(Resource[] initialStorage)
        {
            foreach (Resource resource in initialStorage)
            {
                Add(resource);
            }
        }

        /// <summary>
        /// Does the storage contain the required resources for this production cycle?
        /// </summary>
        /// <param name="required">The required resources for this production cycle.</param>
        /// <returns>Whether enough resources are contained in the storage.</returns>
        public bool HasResourcesRequired(Resource[] required)
        {
            foreach (Resource resource in required)
            {
                if (!HasResourceRequired(resource))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool HasResourceRequired(Resource resource)
        {
            if (Contents.ContainsKey(resource.type))
            {
                if (Contents[resource.type] - resource.amount < 0)
                    return false;
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds a resource item to the storage
        /// </summary>
        /// <param name="resource">Adds a resource to the storage.</param>
        public void Add(Resource resource)
        {
            if (Contents.ContainsKey(resource.type))
            {
                Contents[resource.type] += resource.amount;
            }
            else
            {
                Contents[resource.type] = resource.amount;
            }
        }

        /// <summary>
        /// Removes a resource item from the storage.
        /// </summary>
        /// <param name="resource">Removes a resource to the storage.</param>
        public void Remove(Resource resource)
        {
            Contents[resource.type] -= resource.amount;
        }

        public int Get(ResourceType type)
        {
            if (Contents.ContainsKey(type))
            {
                return Contents[type];
            }

            return 0;
        }
    }
}