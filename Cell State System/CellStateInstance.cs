using System;
using UnityEngine;

namespace PDT
{
    public class CellStateInstance
    {
        public uint sourceEntityID;
        public uint sourceEntityOwnerID;
        public BaseCellState cellState;
        public Vector2Int cellPosition;
        public ECellStateType cellStateType;
        public int duration;
        public float dangerCost = 0;
        public bool isActive => duration >= 0;

        public void DecrementDuration() 
        {
            int oldDuration = duration;
            duration--;
        }

        public void PublishCellStateInvocation()
        {
            EventBus.Publish(new CellStateEvents.OnCellStateEffectInvoked()
            {
                gridCellPosition = cellPosition,
                sourceEntityID = sourceEntityID,
                participantID = sourceEntityOwnerID,
                cellStateType = cellStateType
            });
        }
    }
}
