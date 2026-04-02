using System;
using TBaltaks.FMODManagement;
using UnityEngine;

namespace PDT
{
    public class StickyCellState : BaseCellState<StickyCellStateData>
    {
        public StickyCellState(StickyCellStateData data) : base(data)
        {

        }

        public override void CleanUpEffect(CellStateInstance instance)
        {
            if(!ServiceLocator.FindService(out GridManagementService gridManagementService))
                return;
            
            GridCell cell = gridManagementService.GetCell(instance.cellPosition);
            if(cell == null)
                return;

            cell.dangerCost = (uint)cellStateData.defaultExitMovementCost;
            base.CleanUpEffect(instance);
        }

        public override void ImmediateEffect(CellStateInstance instance)
        {
            if(!ServiceLocator.FindService(out GridManagementService gridManagementService))
                return;
            
            GridCell cell = gridManagementService.GetCell(instance.cellPosition);
            if(cell == null)
                return;
            
            instance.PublishCellStateInvocation();
            cell.exitCost = (uint)cellStateData.tempModifiedExitMovementCost;
        }
    }
}
