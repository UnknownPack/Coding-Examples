using UnityEngine;

namespace PDT
{
    public class CellStateEvents
    {
        public struct OnCellStateCreated : IEvent, IProc
        {
            public Vector2Int gridCellPosition;
            public uint sourceEntityID; // ID of the battle entity that invoked this cell state
            public uint participantID; // ID of owner of said battle entity
            public ECellStateType cellStateType;
        }
        
        public struct OnCellStateDestroyed : IEvent, IProc
        {
            public Vector2Int gridCellPosition;
            public uint sourceEntityID; // ID of the battle entity that invoked this cell state
            public uint participantID; // ID of owner of said battle entity
            public ECellStateType cellStateType;
        }

        public struct OnCellStateEffectInvoked : IEvent, IProc
        {
            public Vector2Int gridCellPosition;
            public uint sourceEntityID; // ID of the battle entity that invoked this cell state
            public uint participantID; // ID of owner of said battle entity
            public ECellStateType cellStateType;
        }
        
        public struct ClearCellState : IEvent, IProc
        {
            public Vector2Int gridCellPosition;
        }
        
        public struct ClearCellStatsAtPositionByType : IEvent, IProc
        {
            public Vector2Int gridCellPosition;
            public ECellStateType cellStateType;
        }
        
        public struct OnUnitMoveToCell : IEvent, IProc
        {
            public uint unitId;
            public Vector2Int fromCellPosition;
            public Vector2Int toCellPosition;
        }
    }
}
