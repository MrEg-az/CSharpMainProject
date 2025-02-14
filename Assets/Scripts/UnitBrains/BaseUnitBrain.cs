﻿using System.Collections.Generic;
using System.Linq;
using Model;
using Model.Runtime.Projectiles;
using Model.Runtime.ReadOnly;
using UnityEngine;
using Utilities;
using Unit = Model.Runtime.Unit;

namespace UnitBrains
{
    public abstract class BaseUnitBrain
    {
        public virtual string TargetUnitName => string.Empty;
        public virtual bool IsPlayerUnitBrain => true;
        
        protected Unit unit { get; private set; }
        protected IReadOnlyRuntimeModel runtimeModel => ServiceLocator.Get<IReadOnlyRuntimeModel>();

        public virtual Vector2Int GetNextStep()
        {
            if (HasTargetsInRange())
                return unit.Pos;

            var target = runtimeModel.RoMap.Bases[
                IsPlayerUnitBrain ? RuntimeModel.BotPlayerId : RuntimeModel.PlayerId];
                
            return CalcNextStepTowards(target);
        }

        public virtual List<BaseProjectile> GetProjectiles()
        {
            var shotTargets = 0;
            var attackRangeSqr = unit.Config.AttackRange * unit.Config.AttackRange;
            List<BaseProjectile> result = new ();
            foreach (var possibleTarget in GetPossibleTargets())
            {
                var diff = possibleTarget - unit.Pos;
                if (diff.sqrMagnitude < attackRangeSqr)
                {
                    for (int i = 0; i < unit.Config.ShotsPerTarget; i++)
                    {
                        var projectile = BaseProjectile.Create(unit.Config.ProjectileType, unit, unit.Pos,
                            possibleTarget, unit.Config.Damage);
                        result.Add(projectile);
                    }
                    shotTargets++;
                }
                
                if (shotTargets >= unit.Config.TargetsInVolley)
                    break;
            }

            return result;
        }

        public void SetUnit(Unit unit)
        {
            this.unit = unit;
        }

        public virtual void Update(float deltaTime, float time)
        {
        }

        protected Vector2Int CalcNextStepTowards(Vector2Int target)
        {
            var diff = target - unit.Pos;
            var stepDiff = diff.SignOrZero();
            var nextStep = unit.Pos + stepDiff;

            if (runtimeModel.IsTileWalkable(nextStep))
                return nextStep;

            if (stepDiff.sqrMagnitude > 1)
            {
                var partStep0 = unit.Pos + new Vector2Int(stepDiff.x, 0);
                if (runtimeModel.IsTileWalkable(partStep0))
                    return partStep0;
                
                var partStep1 = unit.Pos + new Vector2Int(0, stepDiff.y);
                if (runtimeModel.IsTileWalkable(partStep1))
                    return partStep1;
            }

            var sideStep0 = unit.Pos + new Vector2Int(stepDiff.y, -stepDiff.x);
            var shiftedStep0 = unit.Pos + (sideStep0 + stepDiff).SignOrZero();
            if (runtimeModel.IsTileWalkable(shiftedStep0))
                return shiftedStep0;
            
            var sideStep1 = unit.Pos + new Vector2Int(-stepDiff.y, stepDiff.x);
            var shiftedStep1 = unit.Pos + (sideStep1 + stepDiff).SignOrZero();
            if (runtimeModel.IsTileWalkable(shiftedStep1))
                return shiftedStep1;
            
            if (runtimeModel.IsTileWalkable(sideStep0))
                return sideStep0;
            
            if (runtimeModel.IsTileWalkable(sideStep1))
                return sideStep1;
            
            return unit.Pos;
        }
        
        protected List<IReadOnlyUnit> GetUnitsInRadius(float radius, bool enemies)
        {
            var units = new List<IReadOnlyUnit>();
            var pos = unit.Pos;
            var distanceSqr = radius * radius;
            
            foreach (var otherUnit in runtimeModel.RoUnits)
            {
                if (otherUnit == unit)
                    continue;

                if (enemies != (otherUnit.Config.IsPlayerUnit == unit.Config.IsPlayerUnit))
                    continue;

                var otherPos = otherUnit.Pos;
                var diff = otherPos - pos;
                var distance = diff.sqrMagnitude;
                if (distance <= distanceSqr)
                    units.Add(otherUnit);
            }

            return units;
        }

        protected bool HasTargetsInRange()
        {
            var attackRangeSqr = unit.Config.AttackRange * unit.Config.AttackRange;
            foreach (var possibleTarget in GetPossibleTargets())
            {
                var diff = possibleTarget - unit.Pos;
                if (diff.sqrMagnitude < attackRangeSqr)
                    return true;
            }

            return false;
        }

        protected virtual IEnumerable<Vector2Int> GetPossibleTargets()
        {
            return runtimeModel.RoUnits
                .Where(u => u.Config.IsPlayerUnit != IsPlayerUnitBrain)
                .Select(u => u.Pos)
                .Append(runtimeModel.RoMap.Bases[IsPlayerUnitBrain ? RuntimeModel.BotPlayerId : RuntimeModel.PlayerId]);
        }
    }
}