using Architect.Placeables.Presets;
using Buildings;
using Grid;
using UI;
using UnityEngine;

namespace Architect.Placeables
{
    public class ConstructionSite : Building
    {
        public BuildingPreset PresetToConstruct;
        public bool isReceiving = false;

        public ConstructionSite(BuildingPreset preset, BuildingPreset toConstruct) : base(preset)
        {
            PresetToConstruct = toConstruct;
            ProductionCost = toConstruct.buildCost;
        }

        //TODO: better Building hierarchy to reduce duplicate code
        public override void InitializeAfterInstantiation(Tile hostingTile)
        {
            Tile = hostingTile;
            BuildingController.Instance.SubscribeBuilding(this, true, false);
            BuildingConnectionsRenderer = Tile.transform.Find("Recipient Lines").GetComponent<BuildingConnectionsRenderer>();
        }

        protected override void Fabricate()
        {
            bool HasRequiredResources = Input.HasResourcesRequired(ProductionCost);
            OnFabricate?.Invoke(HasRequiredResources);
            if (!HasRequiredResources)
            {
                //Debug.Log("Did not have enough resources to produce!");
                return;
            }
            Tile.RemoveContent();
            ArchitectController.Instance.PlaceBuildingAt(Tile, PresetToConstruct);
        }
    }
}
