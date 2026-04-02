using System;
using TBaltaks.FMODManagement;
using UnityEngine;

namespace PDT
{
    public class BurningCellState : BaseCellState<BurningCellStateData>
    {
        public BurningCellState(BurningCellStateData data) : base(data)
        {
        }

        public override void OnTurnStart(CellStateInstance instance, OnTurnStart e)
        {
            Burn(instance);
        }

        public override void OnTurnEnd(CellStateInstance instance, OnTurnEnd e)
        {
            Burn(instance);
        }

        public void Burn(CellStateInstance instance)
        {
            if(!ServiceLocator.FindService(out GridManagementService gridManagementService))
                return;
            
            GridCell grid = gridManagementService.GetCell(instance.cellPosition);
            if (grid == null)
                return;
            
            uint occupantId = grid.occupyingEntityId;
            //if occupantId is uint max value, then the cell is not occupied, and we can skip the rest of the method
            if(occupantId == UInt32.MaxValue)
                return;
            IBattleEntity entity = EntityRegistry.GetEntityById(occupantId);
            if (entity == null)
            {
                Debug.LogWarning(
                    $"Entity with ID {occupantId} not found for BurningCellState. Cannot apply damage.");
                return;
            }
            Debug.Log($"Burning State place by entity {instance.sourceEntityID} burnt entity {occupantId} for {cellStateData.burnDamagePerTurn} damage.");

            EventBus.Publish(new OnHit()
            {
                damage = cellStateData.burnDamagePerTurn,
                damageSourceId = instance.sourceEntityID, 
                damageTargetId = occupantId,
                notifyOtherProcs = true,
                damageSourceData = new ActionSourceData
                {
                    actionSourceType = EActionSourceType.ENVIRONMENT,
                    actionEffectType = EActionEffectType.DAMAGE,
                    actionSourceGUID = string.Empty
                }
            });
            
            instance.PublishCellStateInvocation();
            AudioManager.Instance.PlayOneShot(FMODEvents.burn);
        }
    }
 }
