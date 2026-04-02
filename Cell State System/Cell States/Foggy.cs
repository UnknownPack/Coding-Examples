using System;
using UnityEngine;
using UnityEngine.UI;

namespace PDT
{
    public class FoggyCellState : BaseCellState<FoggyCellStateData>
    {
        private const string INVISIBLE_GUID = "a62dfd12-ff3c-4580-8504-f9724d541e3e";
        public FoggyCellState(FoggyCellStateData data) : base(data)
        {
        }
        
        public override void CleanUpEffect(CellStateInstance instance)
        {
            SetCellVisibility(instance.cellPosition, false);
            base.CleanUpEffect(instance); 
        }

        public override void OnCellEnter(CellStateInstance instance, OnCellEnter e)
        {
            //Nestor (14/02/2026): Invisibility not yet implemented, but this is where we would apply the invisibility status effect to the entity in the cell.
            EventBus.Publish(new OnStatusEffectGained()
            {
                statusId = INVISIBLE_GUID,
                stackAmount = 23092003, // Infinite stacks until removed
                sourceId = instance.sourceEntityID,
                targetId = e.unitId,
                notifyOtherProcs = true,
            });
            instance.PublishCellStateInvocation();
        }

        public override void OnCellExit(CellStateInstance instance, OnCellExit e)
        {
            IBattleEntity entity = EntityRegistry.GetEntityById(e.unitId);
            if (entity == null)
            {
                Debug.LogWarning($"Entity with ID {e.unitId} not found for SlipperCellState. Cannot apply evade.");
                return;
            }
            if (entity.entityGO.TryGetComponent(out BaseUnit unit))
                unit.RemoveStatusEffect(INVISIBLE_GUID);
        }
        

        public override void ImmediateEffect(CellStateInstance instance)
        {
            SetCellVisibility(instance.cellPosition, true);
        }

        private void SetCellVisibility(Vector2Int position, bool visibility)
        {
            if(!ServiceLocator.FindService(out GridManagementService gridManagementService))
                return;
            
            GridCell cell = gridManagementService.GetCell(position);
            if (cell == null)
                return;

            cell.isCovered = !visibility;
        }
    }
}
