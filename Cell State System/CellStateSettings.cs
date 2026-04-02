using System.Collections.Generic;
using UnityEngine;

namespace PDT
{
    [CreateAssetMenu(fileName = "CellStateSettings", menuName = "Scriptable Objects/CellStateSettings")]
    public class CellStateSettings : AssetIdentifierBaseSO
    {
        [Header("General Cell State Settings")]
        public List<CellStateSettingsEntry> Settings;
        
        [Header("Burning Cell State Settings")]
        public int BurningDamagePerTurn;

        [Header("Slippery Cell State Settings")]
        public int SlipDistance;
        public int SlipDamage = 0;
        
        [Header("Sticky Cell State Settings")]
        [Tooltip("The cost of exiting a cell whilst cell state is active")]public int NewExitCost;
        public int DefaultExitCost;
        
        [Header("Oily Cell State Settings")]
        [Tooltip("The number of 'Burn' is staked on entity")]public int BurnStack;

        [Header("Danger State Settings")]
        public int DangerDamange;
        public bool HasEntry(ECellStateType cellStateType, out CellStateSettingsEntry result)
        {
            result = default;
            foreach (CellStateSettingsEntry entry in Settings)
                if(entry.cellStateType == cellStateType)
                {
                    result = entry;
                    return true;
                }
            
            return false;
        }
    }
    
    
    [System.Serializable]
    public class CellStateSettingsEntry
    {
        public ECellStateType cellStateType = ECellStateType.None;
        public float dangerRating = 6.7f;
        public int duration = 2;

        [Tooltip("A list of cell state types that will override (replace) this cell state if they are present in the same grid cell")]
        public List<ECellStateType> overridingCellStates = new List<ECellStateType>()
        {
            ECellStateType.Burning,
            ECellStateType.Foggy,
            ECellStateType.Oily,
            ECellStateType.Slippery,
            ECellStateType.Sticky,
        };
    }
    
}
