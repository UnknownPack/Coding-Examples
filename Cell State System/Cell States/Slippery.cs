using System;
using TBaltaks.FMODManagement;
using UnityEngine;

namespace PDT
{
    public class SlipperyCellState : BaseCellState<SlipperyCellStateData>
    {
        public SlipperyCellState(SlipperyCellStateData data) : base(data)
        {
        }

        public override void OnUnitMovingBetweenCells(CellStateInstance instance, CellStateEvents.OnUnitMoveToCell e)
        {
            if(!ServiceLocator.FindService(out GridManagementService gridManagementService))
                return;
            
            IBattleEntity entity = EntityRegistry.GetEntityById(e.unitId);
            if (entity == null)
                return;
            
            if (!entity.entityGO.TryGetComponent(out BaseUnit unit))
                return;    

            gridManagementService.PushUnitFromPoint(unit, 
                e.fromCellPosition,
                cellStateData.slipDistance, 
                cellStateData.slipDamage, 
                instance.sourceEntityID); 
            
            instance.PublishCellStateInvocation();
            //TODO: Play sfx here
        }
    }
}
