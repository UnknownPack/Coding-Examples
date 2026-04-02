using System;
using TBaltaks.FMODManagement;
using UnityEngine;

namespace PDT
{
    public class DangerCellState : BaseCellState<DangerCellStateData>
    {
        public DangerCellState(DangerCellStateData data) : base(data)
        {
        }

        public override void OnCellStateLifeEnd(CellStateInstance instance)
        {
            if(!ServiceLocator.FindService(out GridManagementService gridManagementService))
                return;
            
            GridCell cell = gridManagementService.GetCell(instance.cellPosition);
            if(cell == null)
                return;

            uint occupantId = cell.occupyingEntityId;
            //if occupantId is uint max value, then the cell is not occupied, and we can skip the rest of the method
            if(occupantId == UInt32.MaxValue)
                return;
            IBattleEntity entity = EntityRegistry.GetEntityById(occupantId);
            if (entity == null)
            {
                Debug.LogWarning(
                    $"Entity with ID {occupantId} not found for Delayed Attack Cell State. Cannot apply damage.");
                return;
            }
            Debug.Log($"Damage applied at end of Cell State life. {instance.sourceEntityID} damaged entity {occupantId} for {cellStateData.Damage} damage.");

            EventBus.Publish(new OnHit()
            {
                damage = cellStateData.Damage,
                damageSourceId = instance.sourceEntityID, 
                damageTargetId = occupantId,
                notifyOtherProcs = true,
                damageSourceData = new ActionSourceData
                {
                    actionSourceType = EActionSourceType.ABILITY,
                    actionEffectType = EActionEffectType.DAMAGE, 
                    actionSourceGUID = string.Empty
                }
            });
            instance.PublishCellStateInvocation();
            AudioManager.Instance.PlayOneShot(FMODEvents.burn);
        }
    }
}
