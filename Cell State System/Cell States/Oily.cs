using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PDT
{
    public class OilyCellState : BaseCellState<OilyCellStateData>
    {
        private const string BURNING_GUID = "f52a89b8-e719-4da3-bde7-a369f3a86766";

        public OilyCellState(OilyCellStateData data) : base(data)
        {
        }

        public override void OnStatusEffectApplied(OnStatusEffectGained e)
        {
            if (!ServiceLocator.FindService(out GridManagementService gridManagementService))
                return;

            if (e.statusId != BURNING_GUID)
                return;

            uint sourceID = e.sourceId;
            IBattleEntity entity = EntityRegistry.GetEntityById(sourceID);
            if (entity == null)
                return;

            if (!entity.entityGO.TryGetComponent(out BaseUnit unit))
                return;

            System.Collections.Generic.List<GridCell> aoeCells = gridManagementService.GetCellsInDirectionFromOrigin(EDirectionType.Cardinal,
                gridManagementService.GetEntityPosition(e.targetId), 1, out GridCell originCell);
            aoeCells.Add(originCell);

            foreach (GridCell cell in aoeCells)
            {
                if (!IsValidPosition(cell.gridPosition, out List<CellStateInstance> instances))
                    continue;

                //replaces 'Oily' CellState with 'Burning' CellState
                EventBus.Publish(new CellStateEvents.OnCellStateCreated()
                {
                    participantID = unit.GetOwnerParticipant().ParticipantId,
                    sourceEntityID = e.sourceId,
                    cellStateType = ECellStateType.Burning,
                    gridCellPosition = cell.gridPosition
                });

                foreach (CellStateInstance oilyInstance in instances)
                {
                    EventBus.Publish(new CellStateEvents.OnCellStateEffectInvoked()
                    {
                        gridCellPosition = oilyInstance.cellPosition,
                        sourceEntityID = oilyInstance.sourceEntityID,
                        participantID = oilyInstance.sourceEntityOwnerID,
                        cellStateType = oilyInstance.cellStateType,
                    });
                }

                if (!cell.isOccupied)
                    continue;

                EventBus.Publish(new OnStatusEffectGained()
                {
                    sourceId = e.sourceId,
                    targetId = cell.occupyingEntityId,
                    statusId = BURNING_GUID,
                    stackAmount = (uint)cellStateData.additionalBurnStacks * (uint)instances.Count, //TODO: Check on Xander if this is cool ~ NB
                    notifyOtherProcs = true
                });
            }
        }

        private bool IsValidPosition(Vector2Int gridPosition, out List<CellStateInstance> instances)
        {
            instances = null;
            if (!ServiceLocator.FindService(out CellStateManager cellStateManager))
                return false;

            instances = cellStateManager.GetAllInstancesOfTypeAtPosition(ECellStateType.Oily, gridPosition).ToList();
            return instances != null && instances.Count > 0;
        }
    }
}
