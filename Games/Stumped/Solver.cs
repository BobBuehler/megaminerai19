using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joueur.cs.Games.Stumped
{
    static class Solver
    {
        public static void Move(Beaver mover, IEnumerable<Point> targets)
        {
            var targetPoints = targets.ToHashSet<Point>();

            if (mover.Moves > 0)
            {
                var search = new AStar<Point>(
                    new[] { mover.ToPoint() },
                    p => targetPoints.Contains(p),
                    (p1, p2) => GetMoveCost(p1.ToTile(), p2.ToTile()),
                    p => 0,
                    p => p.ToTile().GetReachableNeighbors(mover.Job.Moves).Select(t => t.ToPoint())
                );

                var path = search.Path.ToArray();
                var steps = path.Skip(1).ToQueue();
                while (steps.Count > 0 && GetMoveCost(mover.Tile, steps.Peek().ToTile()) <= mover.Moves)
                {
                    mover.Move(steps.Dequeue().ToTile());
                }
            }
        }

        public static void Attack(Beaver attacker, IEnumerable<Beaver> targets)
        {
            var targettables = targets.Where(t => t.Recruited && t.Health > 0);
            var target = targettables.FirstOrDefault(t => attacker.Tile.HasNeighbor(t.Tile));
            if (target != null)
            {
                while (attacker.Actions > 0 && target.Health > 0)
                {
                    attacker.Attack(target);
                }
            }
        }

        public static void MoveAndAttack(Beaver attacker, IEnumerable<Beaver> targets)
        {
            if (attacker.Moves > 0)
            {
                var targetPoints = targets
                    .Where(t => t.Health > 0)
                    .SelectMany(t => t.Tile.GetNeighbors().Select(n => n.ToPoint()));

                Move(attacker, targetPoints);
            }

            Attack(attacker, targets);
        }

        public static void Harvest(Beaver harvester, IEnumerable<Spawner> targets)
        {
            if (harvester.OpenCarryCapacity() == 0)
            {
                return;
            }

            var targettables = targets.Where(t => t.Health > 0);
            var target = targettables.FirstOrDefault(t => harvester.Tile.HasNeighbor(t.Tile));
            if (target != null)
            {
                while (harvester.Actions > 0 && target.Health > 0)
                {
                    harvester.Harvest(target);
                }
            }
        }

        public static void MoveAndHarvest(Beaver harvester, IEnumerable<Spawner> spawners)
        {
            if (harvester.OpenCarryCapacity() == 0)
            {
                return;
            }
            
            if (harvester.Moves > 0)
            {
                var targetPoints = spawners
                    .SelectMany(s => s.Tile.GetNeighbors().Select(n => n.ToPoint()));

                Move(harvester, targetPoints);
            }

            Harvest(harvester, spawners);
        }

        public static void Pickup(Beaver picker, IEnumerable<Tile> targets, string resource)
        {
            if (picker.Actions <= 0 || picker.OpenCarryCapacity() == 0)
            {
                return;
            }

            var targettables = targets.Where(t => t.GetCount(resource) > 0 && picker.Tile.HasNeighbor(t));
            if (targettables.Any())
            {
                var target = targettables.MaxByValue(t => t.GetCount(resource));
                picker.Pickup(target, resource, Math.Min(target.GetCount(resource), picker.OpenCarryCapacity()));
            }
        }

        public static void MoveAndPickup(Beaver picker, IEnumerable<Tile> targets, string resource)
        {
            if (picker.Actions <= 0 || picker.OpenCarryCapacity() == 0)
            {
                return;
            }

            var targetPoints = targets.Where(t => t.GetCount(resource) > 0);
            if (picker.Moves > 0)
            {
                Move(picker, targetPoints.Select(t => t.ToPoint()));
            }

            Pickup(picker, targets, resource);
        }

        public static void Drop(Beaver dropper, IEnumerable<Tile> targets, string resource)
        {
            if (dropper.Actions <= 0 || dropper.GetCount(resource) == 0)
            {
                return;
            }

            var target = targets.FirstOrDefault(t => dropper.Tile.HasNeighbor(t));
            if (target != null)
            {
                dropper.Drop(target, resource, dropper.GetCount(resource));
            }
        }

        public static void MoveAndDrop(Beaver dropper, IEnumerable<Tile> targets, string resource)
        {
            if (dropper.Actions <= 0 || dropper.GetCount(resource) == 0)
            {
                return;
            }
            
            if (dropper.Moves > 0)
            {
                Move(dropper, targets.Select(t => t.ToPoint()));
            }

            Drop(dropper, targets, resource);
        }

        public static int GetMoveCost(Tile source, Tile dest)
        {
            if (source.GetNeighbor(source.FlowDirection) == dest)
            {
                return 1;
            } else if (dest.GetNeighbor(dest.FlowDirection) == source)
            {
                return 3;
            }
            return 2;
        }

        public static string InvertDirection(string direction)
        {
            switch (direction)
            {
                case "North":
                    return "South";
                case "East":
                    return "West";
                case "South":
                    return "North";
                case "West":
                    return "East";
                default:
                    return direction;
            }
        }

        public static IEnumerable<Tile> GetReachableNeighbors(this Tile tile, int jobMoves)
        {
            return tile.GetNeighbors().Where(t => t.IsPathable() && GetMoveCost(tile, t) <= jobMoves);
        }
    }
}
