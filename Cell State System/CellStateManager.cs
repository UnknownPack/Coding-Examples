using System.Collections.Generic;
using UnityEngine;

namespace PDT
{
    public class CellStateManager : MonoBehaviour, IService
    {
        CellStateRegistry cellStateRegistry;
        GridManagementService gridManagementService;
        
        public void Initialize()
        {
            ServiceLocator.FindService(out gridManagementService);
            cellStateRegistry = new CellStateRegistry();
        } 
        
        private void Awake()
        {
            ServiceLocator.Register(this);
            EventBus.Subscribe<OnTurnStart>(OnTurnStart_Callback);
            EventBus.Subscribe<OnTurnEnd>(OnTurnEnd_Callback);
            EventBus.Subscribe<OnCellEnter>(OnCellEnter_Callback);
            EventBus.Subscribe<OnCellExit>(OnCellExit_Callback);
            EventBus.Subscribe<CellStateEvents.OnCellStateCreated>(AddCellStateToGridCell);
            EventBus.Subscribe<OnStatusEffectGained>(OnStatusEffectApplied_Callback);
            EventBus.Subscribe<CellStateEvents.OnUnitMoveToCell>(OnUnitMovingBetweenCells_Callback);
            EventBus.Subscribe<CellStateEvents.ClearCellState>(ClearAllCellStatesAtPosition_Callback);
            EventBus.Subscribe<CellStateEvents.ClearCellStatsAtPositionByType>(ClearAllInstancesFromPositionByType_Callback);
        }


        private void OnDestroy()
        {
            ServiceLocator.Remove<CellStateManager>();
            cellStateRegistry.Dispose();
            EventBus.Unsubscribe<OnTurnStart>(OnTurnStart_Callback);
            EventBus.Unsubscribe<OnTurnEnd>(OnTurnEnd_Callback);
            EventBus.Unsubscribe<OnCellEnter>(OnCellEnter_Callback);
            EventBus.Unsubscribe<OnCellExit>(OnCellExit_Callback);
            EventBus.Unsubscribe<CellStateEvents.OnCellStateCreated>(AddCellStateToGridCell);
            EventBus.Unsubscribe<OnStatusEffectGained>(OnStatusEffectApplied_Callback);
            EventBus.Unsubscribe<CellStateEvents.OnUnitMoveToCell>(OnUnitMovingBetweenCells_Callback);
            EventBus.Unsubscribe<CellStateEvents.ClearCellState>(ClearAllCellStatesAtPosition_Callback);
            EventBus.Unsubscribe<CellStateEvents.ClearCellStatsAtPositionByType>(ClearAllInstancesFromPositionByType_Callback);
        }

        #region Callbacks

        private void OnTurnStart_Callback(OnTurnStart e)
        {
            TickStatesByOwner(e.participantId);
            if(!cellStateRegistry.cellInstancesByOwner.TryGetValue(e.participantId, out List<CellStateInstance> cellStateInstances))
            {
                cellStateRegistry.cellInstancesByOwner[e.participantId] = new List<CellStateInstance>();
                return;
            }
            
            if(cellStateInstances == null || cellStateInstances.Count == 0)
                return;
            
            foreach (var stateInstance in cellStateInstances)
                stateInstance.cellState.OnTurnStart(stateInstance, e);
        }

        private void OnTurnEnd_Callback(OnTurnEnd e)
        {
            if(!cellStateRegistry.cellInstancesByOwner.TryGetValue(e.participantId, out List<CellStateInstance> cellStateInstances))
            {
                cellStateRegistry.cellInstancesByOwner[e.participantId] = new List<CellStateInstance>();
                return;
            }
            
            if(cellStateInstances == null || cellStateInstances.Count == 0)
                return;
            
            foreach (var stateInstance in cellStateInstances)
                stateInstance.cellState.OnTurnEnd(stateInstance, e);
        }
        
        private void OnCellEnter_Callback(OnCellEnter e)
        {
            if(!cellStateRegistry.cellInstancesByPosition.TryGetValue(e.cellPosition, out List<CellStateInstance> cellStateInstances))
                return;
            
            if(cellStateInstances == null || cellStateInstances.Count == 0)
                return;

            foreach (CellStateInstance instance in cellStateInstances)
                instance.cellState.OnCellEnter(instance, e);
        }

        private void OnCellExit_Callback(OnCellExit e)
        {
            if(!cellStateRegistry.cellInstancesByPosition.TryGetValue(e.cellPosition, out List<CellStateInstance> cellStateInstances))
                return;
            
            if(cellStateInstances == null || cellStateInstances.Count == 0)
                return;

            foreach (CellStateInstance instance in cellStateInstances)
                instance.cellState.OnCellExit(instance, e);
        }
        
        private void OnStatusEffectApplied_Callback(OnStatusEffectGained e)
        {
            uint sourceID = e.sourceId;
            IBattleEntity entity = EntityRegistry.GetEntityById(sourceID);
            if(entity == null)
                return;
                    
            if(!entity.entityGO.TryGetComponent(out BaseUnit unit))
                return;

            if(!cellStateRegistry.cellInstancesByPosition.TryGetValue(unit.GetCurrentPosition(), out List<CellStateInstance> cellStateInstances))
                return;
            
            if(cellStateInstances == null || cellStateInstances.Count == 0)
                return;
            
            foreach (CellStateInstance instance in cellStateInstances)
                instance.cellState.OnStatusEffectApplied(e);
        }
        
        private void OnUnitMovingBetweenCells_Callback(CellStateEvents.OnUnitMoveToCell e)
        {
            if(!cellStateRegistry.cellInstancesByPosition.TryGetValue(e.toCellPosition, out List<CellStateInstance> cellStateInstances))
                return;
            
            if(cellStateInstances == null || cellStateInstances.Count == 0)
                return;
            
            foreach (CellStateInstance instance in cellStateInstances)
                instance.cellState.OnUnitMovingBetweenCells(instance, e);
            
        }

        private void ClearAllCellStatesAtPosition_Callback(CellStateEvents.ClearCellState e)
        {
            HashSet<CellStateInstance> cellStateInstances = new HashSet<CellStateInstance>(GetInstancesByPosition(e.gridCellPosition));
            if(cellStateInstances.Count == 0)
                return;
            
            ClearAllInstancesFrom(cellStateInstances);
        }

        private void ClearAllInstancesFromPositionByType_Callback(CellStateEvents.ClearCellStatsAtPositionByType e)
        {
            HashSet<CellStateInstance> instances = GetAllInstancesOfTypeAtPosition(e.cellStateType, e.gridCellPosition);
            if(instances.Count == 0)
                return;
            
            ClearAllInstancesFrom(instances);
        }


        #endregion

        void TickStatesByOwner(uint ownerID)
        {
            if(!cellStateRegistry.cellInstancesByOwner.TryGetValue(ownerID, out List<CellStateInstance> cellStateInstances))
                return;

            List<CellStateInstance> cellStatesCopy = new List<CellStateInstance>(cellStateInstances);
            if(cellStatesCopy.Count == 0)
                return;
            
            foreach (CellStateInstance state in cellStatesCopy)
            {
                state.DecrementDuration();
                if (!state.isActive)
                {
                    state.cellState.OnCellStateLifeEnd(state);
                    ClearInstance(state);
                }
            }
        }
        
        void AddCellStateToGridCell(CellStateEvents.OnCellStateCreated e)
        {
            BaseCellState baseCellState = cellStateRegistry.cellStatesDefinitions[e.cellStateType];
            CellStateInstance instance = new CellStateInstance()
            {
                cellStateType = e.cellStateType,
                cellPosition = e.gridCellPosition,
                sourceEntityID = e.sourceEntityID,
                sourceEntityOwnerID = e.participantID,
                cellState = baseCellState,
                duration = baseCellState.GenericCellStateDataData.duration,
                dangerCost = baseCellState.GenericCellStateDataData.dangerCost,
            };
            baseCellState.baseCellStateInstance = instance;

            // check if participant has a cell state list, if not create one and add the instance to it
            if (!cellStateRegistry.cellInstancesByPosition.ContainsKey(e.gridCellPosition))
                cellStateRegistry.cellInstancesByPosition[e.gridCellPosition] = new List<CellStateInstance>();
            
            // check if participant has a cell state list, if not create one and add the instance to it
            if (!cellStateRegistry.cellInstancesByOwner.ContainsKey(e.participantID))
                cellStateRegistry.cellInstancesByOwner[e.participantID] = new List<CellStateInstance>();
            
            // check if there is a list of cell type, if not create one and add the instance to it
            if (!cellStateRegistry.cellStatesByType.ContainsKey(e.cellStateType))
                cellStateRegistry.cellStatesByType[e.cellStateType] = new List<CellStateInstance>();
            
            // check and/or clear any cell states that can be overriden by incoming cell type
            CheckAndClearAtPosition(e.gridCellPosition, e.cellStateType);
            
            // Add instance to all the dictionaries 
            cellStateRegistry.cellInstancesByPosition[e.gridCellPosition].Add(instance);
            cellStateRegistry.cellInstancesByOwner[e.participantID].Add(instance);
            cellStateRegistry.cellStatesByType[e.cellStateType].Add(instance);
            
            // Apply immediate effect of the cell state and set the cell's current cell state to the new instance
            instance.cellState.ImmediateEffect(instance);
            
            // Set GridCell's Cell State reference 
            GridCell cell = gridManagementService.GetCell(e.gridCellPosition);
            if (cell != null)
                cell.currentCellStates.Add(instance);
        }


        void ClearInstance(CellStateInstance instance)
        {
            if(instance!=null)
            {
                uint cellStateOwner = instance.sourceEntityOwnerID;
                ECellStateType eCellStateType = instance.cellStateType;
                Vector2Int cellStateInstancePosition = instance.cellPosition;
                instance.cellState.CleanUpEffect(instance);
                
                // remove cell state instance from dictionaries
                cellStateRegistry.cellInstancesByOwner[cellStateOwner].Remove(instance);
                cellStateRegistry.cellStatesByType[eCellStateType].Remove(instance);
                cellStateRegistry.cellInstancesByPosition[cellStateInstancePosition].Remove(instance);
                
                EventBus.Publish(new CellStateEvents.OnCellStateDestroyed()
                {
                    gridCellPosition = instance.cellPosition,
                    sourceEntityID = instance.sourceEntityID,
                    participantID = instance.sourceEntityOwnerID,
                    cellStateType = instance.cellStateType
                });
                
                GridCell cell = gridManagementService.GetCell(instance.cellPosition);
                if (cell != null)
                    cell.currentCellStates.Remove(instance);
                
                Debug.Log($"Cleared Cell State {eCellStateType} at {cellStateInstancePosition}");
            }
            else
                Debug.LogWarning($"Attempted to Clear a null refrence of cell state instance!");
        }

        void CheckAndClearAtPosition(Vector2Int gridCellPosition, ECellStateType incomingCellState)
        {
            ClearAllInstancesFrom(InstancesToRemoveAt(gridCellPosition, incomingCellState));
        }

        void ClearAllInstancesFrom(HashSet<CellStateInstance> instancesToRemove)
        {
            if(instancesToRemove == null || instancesToRemove.Count == 0)
                return;

            foreach (CellStateInstance cellState in instancesToRemove)
            {
                if(cellState == null)
                    continue;
                
                ClearInstance(cellState);
            }
        }

        public HashSet<CellStateInstance> GetAllInstancesOfTypeAtPosition(ECellStateType type, Vector2Int gridCellPosition)
        {
            List<CellStateInstance> cellStateInstances = cellStateRegistry.cellStatesByType[type];
            if (cellStateInstances == null || cellStateInstances.Count == 0)
            {
                Debug.LogWarning($"Attempted to get all instances of type {type} but none exist at {gridCellPosition}!");
                return new HashSet<CellStateInstance>();
            }

            HashSet<CellStateInstance> results = new HashSet<CellStateInstance>();
            foreach (CellStateInstance instance in cellStateInstances)
            {
                if(instance == null) continue;
                if(instance.cellPosition == gridCellPosition)
                    results.Add(instance);
            }
            return results;
        }

        HashSet<CellStateInstance> InstancesToRemoveAt( Vector2Int gridCellPosition, ECellStateType overrideType)
        {
            List<CellStateInstance> instancesAtPosition = GetInstancesByPosition(gridCellPosition);
            if (instancesAtPosition == null || instancesAtPosition.Count == 0)
            {
                if(UserConfigManager.DebugMode)
                    Debug.LogWarning($"There aren't any cell state instances at {gridCellPosition} to remove! Returning Empty List!");
                return new HashSet<CellStateInstance>();
            }
            
            HashSet<CellStateInstance> filteredList = new HashSet<CellStateInstance>();
            foreach (CellStateInstance cellState in instancesAtPosition)
            {
                if(CanStateBeOverridenBy(cellState.cellStateType, overrideType))
                {
                    if(UserConfigManager.DebugMode)
                        Debug.Log($"{cellState.cellStateType} can be overridden by {overrideType}!");
                    filteredList.Add(cellState);
                }
            }
            return filteredList;
        }
        

        bool CanStateBeOverridenBy(ECellStateType cellStateType, ECellStateType overrideType)
        {
            CellStateData cellStateData = cellStateRegistry.cellStatesDefinitions[cellStateType].GenericCellStateDataData;
            if (cellStateData == null)
            {
                Debug.LogError($"Cell State type cannot be found in cellStatesDefinitions Returning false!");
                return false;
            }

            List<ECellStateType> overridingCellStates = cellStateData.overridingCellStates;
            if (overridingCellStates == null)
            {
                Debug.LogError("Failed to get cell sate data on overriding CellStateTypes because list is null! Returning false!");
                return false;
            }

            if (overridingCellStates.Count == 0)
            {
                Debug.Log($"{cellStateType} cannot be overriden by any cell state! Returning false");
                return false;
            }
            return cellStateData.overridingCellStates.Contains(overrideType);
        }
        
        public List<CellStateInstance> GetInstancesByPosition(Vector2Int position)
        {
            cellStateRegistry.cellInstancesByPosition.TryGetValue(position, out List<CellStateInstance> instances);
            return instances;
        }

    }
    
 
    [System.Serializable]
    public enum ECellStateType
    {
        None,
        Burning,
        Slippery,
        Sticky,
        Oily,
        Foggy,
        Danger
    }
    
    
}
