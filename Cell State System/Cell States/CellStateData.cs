using System.Collections.Generic;
using UnityEngine;

namespace PDT
{
    public class CellStateData
    {
        public float dangerCost;
        public int duration;
        public List<ECellStateType> overridingCellStates = new List<ECellStateType>();
    }
    
    public class FoggyCellStateData : CellStateData
    {
    }
    
    public class OilyCellStateData : CellStateData
    {
        public int additionalBurnStacks;
    }
    
    public class StickyCellStateData : CellStateData
    {
        public int tempModifiedExitMovementCost;
        public int defaultExitMovementCost;
    }
    
    public class SlipperyCellStateData : CellStateData
    {
        public int slipDistance;
        public int slipDamage;
    }
    
    public class BurningCellStateData : CellStateData
    {
        public int burnDamagePerTurn;
    }
    
    public class DangerCellStateData : CellStateData
    {
        public int Damage;
    }
}
