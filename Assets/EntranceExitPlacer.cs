using Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Grid.GridManager;

public class EntranceExitPlacer : MonoBehaviour
{

    private Building selectedBuilding;

    [SerializeField]
    private TextMeshProUGUI textMeshProUGUI;

    private bool b_placingEntrance;

    [SerializeField]
    private bool PlacingEntrance
    {
        get
        {
            return b_placingEntrance;
        }
        set
        {
            b_placingEntrance = value;
            textMeshProUGUI.text = string.Format("Place an {0} for this building!", b_placingEntrance ? "entrypoint" : "exitpoint");
        }
    }

    public void SelectBuilding(Building newSelection)
    {
        selectedBuilding = newSelection;
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            Tile tile = GridManager.Instance.TryGetTileFromMousePos();
            if (tile != null)
            {
                bool isNeighbour = false;
                Neighbour[] neighbours = selectedBuilding.Tile.Neighbours;
                foreach (Neighbour neighbour in neighbours)
                {
                    if (neighbour.Tile == tile)
                    {
                        isNeighbour = true;
                        break;
                    }
                }

                if (isNeighbour)
                {
                    if (PlacingEntrance)
                        SetEntrance(tile);
                    else
                        SetExit(tile);
                }
            }
        }
    }

    private void SetEntrance(Tile tile)
    {
        if (selectedBuilding.SetEntrance(tile))
        {
            PlacingEntrance = false;
        }
    }

    private void SetExit(Tile tile)
    {
        if (selectedBuilding.SetExit(tile))
        {
            this.enabled = false;
        }

    }

    private void OnEnable()
    {
        textMeshProUGUI.gameObject.SetActive(true);
        PlacingEntrance = true;
    }

    private void OnDisable()
    {
        if (selectedBuilding.HasExitAndEntrance)
        {
            textMeshProUGUI.gameObject.SetActive(false);
            selectedBuilding = null;
        }
        else
        {
            Debug.LogWarning("Building has no entry/exit points!");
        }
    }
}