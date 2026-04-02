using System.Collections.Generic;
using UnityEngine;

namespace PDT
{
    public class CellStateTestScript : MonoBehaviour
    {
        public Vector2Int gridPosition;
        public ECellStateType cellStateType;
        [Tooltip("Give participant ID/ owner of a unit")]public uint participantID;
        [Tooltip("Give battle entity ID ")]public uint sourceEntityID;
        
        private void Start()
        {
            if(!ServiceLocator.FindService(out ParticipantSystem participantSystem))
                return;

            List<BaseParticipant> list = participantSystem.GetParticipantsList();
            foreach (var item in list)
                Debug.LogWarning($"Participant: {item.Name} with id: {item.ParticipantId}.");
        }
        
        [ContextMenu("Add Cell States to grid position")]
        private void TestCreateCellState()
        {
            Debug.Log($"State type: {cellStateType} being added to grid position: {gridPosition} by owner: {participantID}");
            EventBus.Publish(new CellStateEvents.OnCellStateCreated()
            {
                gridCellPosition = gridPosition,
                sourceEntityID = sourceEntityID,
                participantID = participantID,
                cellStateType = cellStateType
            });
            
        }

        [ContextMenu("Clear all States at position")]
        private void RemoveAllCellStatesAtPosition()
        {
            EventBus.Publish(new CellStateEvents.ClearCellState()
            {
                gridCellPosition = gridPosition,
            });
        }

        [ContextMenu("Clear all States at position by type")]
        private void RemoveCellStateAtPositionByType()
        {
            EventBus.Publish(new CellStateEvents.ClearCellStatsAtPositionByType()
            {
                gridCellPosition = gridPosition,
                cellStateType = this.cellStateType
            });
        }
    }
    
}
