using System;
using TBaltaks.FMODManagement;
using UnityEngine;

namespace PDT
{
    public class BaseCellState
    {
        public virtual CellStateData GenericCellStateDataData => default;
        public CellStateInstance baseCellStateInstance;

        public virtual void OnCellStateLifeEnd(CellStateInstance baseCellStateInstance)
        {

        }

        public virtual void CleanUpEffect(CellStateInstance baseCellStateInstance)
        {
            if (!ServiceLocator.FindService(out GridManagementService gridManagementService))
                return;

            GridCell cell = gridManagementService.GetCell(baseCellStateInstance.cellPosition);
            if (cell == null)
                return;

            cell.dangerCost = 0;
            cell.currentCellStates.Remove(baseCellStateInstance);
        }

        public virtual void OnTurnStart(CellStateInstance baseCellStateInstance, OnTurnStart e)
        {

        }

        public virtual void OnTurnEnd(CellStateInstance baseCellStateInstance, OnTurnEnd e)
        {

        }

        public virtual void OnCellEnter(CellStateInstance baseCellStateInstance, OnCellEnter e)
        {

        }

        public virtual void OnCellExit(CellStateInstance baseCellStateInstance, OnCellExit e)
        {

        }

        public virtual void OnStatusEffectApplied(OnStatusEffectGained e)
        {

        }

        public virtual void OnUnitMovingBetweenCells(CellStateInstance baseCellStateInstance, CellStateEvents.OnUnitMoveToCell moveToCellData)
        {

        }

        public virtual void ImmediateEffect(CellStateInstance baseCellStateInstance)
        {

        }
    }

    public class BaseCellState<T> : BaseCellState where T : CellStateData
    {
        public T cellStateData;

        public BaseCellState(T data) : base()
        {
            cellStateData = data;
        }

        public override CellStateData GenericCellStateDataData => cellStateData;
    }
}
