using System;
using System.Collections.Generic;
using UnityEngine;

namespace PDT
{
    public class CellStateRegistry : IDisposable
    {
        private const string CEll_STATE_SETTINGS_KEY = "6bd965b1-e6bd-4dee-a882-71da50e9f595";
        public Dictionary<uint, List<CellStateInstance>>  cellInstancesByOwner;
        public Dictionary <Vector2Int, List<CellStateInstance>> cellInstancesByPosition;
        public Dictionary<ECellStateType, List<CellStateInstance>> cellStatesByType;
        public Dictionary<ECellStateType, BaseCellState> cellStatesDefinitions;
        
        CellStateSettings cellStateSettings;

        public CellStateRegistry()
        {
             cellInstancesByOwner = new Dictionary<uint, List<CellStateInstance>>();
             cellStatesDefinitions = new Dictionary<ECellStateType, BaseCellState>();
             cellInstancesByPosition = new Dictionary<Vector2Int, List<CellStateInstance>>();
             cellStatesByType = new Dictionary<ECellStateType, List<CellStateInstance>>
             {
                 { ECellStateType.Burning, new List<CellStateInstance>() },
                 { ECellStateType.Foggy, new List<CellStateInstance>() },
                 { ECellStateType.Oily, new List<CellStateInstance>() },
                 { ECellStateType.Slippery, new List<CellStateInstance>() },
                 { ECellStateType.Sticky, new List<CellStateInstance>() },
                 { ECellStateType.Danger , new List<CellStateInstance>()}
             };

             if (!InitalizeCellStateData())
                 Debug.LogWarning("CellState Settings not properly initialized!");
        }

        private bool InitalizeCellStateData()
        {
            if(!ServiceLocator.FindService(out AssetCatalogueSystem assetCatalogueSystem))
            {
                Debug.LogWarning("Asset Catalogue system not properly initialized!");
                return false;
            }

            cellStateSettings = assetCatalogueSystem.GetCatalogue("GRID_SETTINGS_CONFIG").GetAsset(CEll_STATE_SETTINGS_KEY) as CellStateSettings;
            if(cellStateSettings == null)
            {
               Debug.LogError("Could not find CellStateSettings in the AssetCatalogue!");
                return false;
            }

            cellStatesDefinitions = new Dictionary<ECellStateType, BaseCellState>();

            if (cellStateSettings.HasEntry(ECellStateType.Burning, out CellStateSettingsEntry burningSettings))
            {
                BurningCellStateData burningCellStateData = new BurningCellStateData()
                {
                    duration = burningSettings.duration,
                    dangerCost = burningSettings.dangerRating,
                    overridingCellStates = burningSettings.overridingCellStates,
                    burnDamagePerTurn = cellStateSettings.BurningDamagePerTurn
                };
                cellStatesDefinitions.Add(ECellStateType.Burning, new BurningCellState(burningCellStateData));
            }
             

            if (cellStateSettings.HasEntry(ECellStateType.Foggy, out CellStateSettingsEntry foggySettings))
            {
                FoggyCellStateData foggyCellStateData = new FoggyCellStateData()
                {
                    duration = foggySettings.duration,
                    dangerCost = foggySettings.dangerRating,
                    overridingCellStates = foggySettings.overridingCellStates,
                };
                cellStatesDefinitions.Add(ECellStateType.Foggy, new FoggyCellState(foggyCellStateData));
            }

            if (cellStateSettings.HasEntry(ECellStateType.Oily, out CellStateSettingsEntry oilySettings))
            {
                OilyCellStateData oilyCellStateData = new OilyCellStateData()
                {
                    duration = oilySettings.duration,
                    dangerCost = oilySettings.dangerRating,
                    overridingCellStates = oilySettings.overridingCellStates,
                    additionalBurnStacks = cellStateSettings.BurnStack
                };
                cellStatesDefinitions.Add(ECellStateType.Oily, new OilyCellState(oilyCellStateData));
            }
            
            if (cellStateSettings.HasEntry(ECellStateType.Slippery, out CellStateSettingsEntry slipperySettings))
            {
                SlipperyCellStateData slipperyCellStateData = new SlipperyCellStateData()
                {
                    duration = slipperySettings.duration,
                    dangerCost = slipperySettings.dangerRating,
                    slipDistance = cellStateSettings.SlipDistance,
                    overridingCellStates = slipperySettings.overridingCellStates,
                    slipDamage = cellStateSettings.SlipDamage
                };
                cellStatesDefinitions.Add(ECellStateType.Slippery, new SlipperyCellState(slipperyCellStateData));
            }
            
            if (cellStateSettings.HasEntry(ECellStateType.Sticky, out CellStateSettingsEntry stickySettings))
            {
                StickyCellStateData stickyCellStateData = new StickyCellStateData()
                {
                    duration = stickySettings.duration,
                    dangerCost = stickySettings.dangerRating,
                    overridingCellStates = stickySettings.overridingCellStates,
                    tempModifiedExitMovementCost = cellStateSettings.NewExitCost,
                    defaultExitMovementCost = cellStateSettings.DefaultExitCost,
                };
                cellStatesDefinitions.Add(ECellStateType.Sticky, new StickyCellState(stickyCellStateData));
            }
            
            if (cellStateSettings.HasEntry(ECellStateType.Danger, out CellStateSettingsEntry delayedAttack))
            {
                DangerCellStateData dangerCellStateData = new DangerCellStateData()
                {
                    duration = delayedAttack.duration,
                    dangerCost = delayedAttack.dangerRating,
                    overridingCellStates = delayedAttack.overridingCellStates,
                    Damage = cellStateSettings.DangerDamange
                };
                cellStatesDefinitions.Add(ECellStateType.Danger, new DangerCellState(dangerCellStateData));
            }
            
            return true;
        }

        public void Dispose()
        {
            cellInstancesByOwner.Clear();
            cellInstancesByPosition.Clear();
            cellStatesByType.Clear();
            cellStatesDefinitions.Clear();
        }
    }
    
    
}
