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

                MoveAlong(mover, search.Path);
            }
        }

        public static void MoveAlong(Beaver beaver, IEnumerable<Point> steps)
        {
            var queue = steps.SkipWhile(p => p.Equals(beaver.ToPoint())).ToQueue();
            while (queue.Count > 0 && GetMoveCost(beaver.Tile, queue.Peek().ToTile()) <= beaver.Moves)
            {
                beaver.Move(queue.Dequeue().ToTile());
            }
        }

        public static IEnumerable<Point> GetClosestPath(IEnumerable<Beaver> beavers, Func<Point, bool> isGoal, int moves)
        {
            var search = new AStar<Point>(
                beavers.Select(b => b.ToPoint()),
                isGoal,
                (p1, p2) => GetMoveCost(p1.ToTile(), p2.ToTile()),
                p => 0,
                p => p.ToTile().GetReachableNeighbors(moves).Select(t => t.ToPoint())
            );

            return search.Path;
        }

        public static void Attack(Beaver attacker, IEnumerable<Beaver> targets)
        {
            var targettables = targets.Where(t => t.Recruited && t.Health > 0);
            var target = targettables.FirstOrDefault(t => attacker.Tile._HasNeighbor(t.Tile));
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
            var target = targettables.FirstOrDefault(t => harvester.Tile._HasNeighbor(t.Tile));
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
                var movePoints = spawners
                    .SelectMany(s => s.Tile.GetNeighbors().Select(n => n.ToPoint()));

                Move(harvester, movePoints);
            }

            Harvest(harvester, spawners);
        }

        public static void Pickup(Beaver picker, IEnumerable<Tile> targets, string resource)
        {
            if (picker.Actions <= 0 || picker.OpenCarryCapacity() == 0)
            {
                return;
            }

            var targettables = targets.Where(t => t.GetCount(resource) > 0 && picker.Tile._HasNeighbor(t));
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
                var movePoints = targetPoints.Concat(targetPoints.SelectMany(t => t.GetNeighbors()))
                    .Select(n => n.ToPoint());
                Move(picker, movePoints);
            }

            Pickup(picker, targets, resource);
        }

        public static void Drop(Beaver dropper, IEnumerable<Tile> targets, string resource)
        {
            if (dropper.Actions <= 0 || dropper.GetCount(resource) == 0)
            {
                return;
            }

            var target = targets.FirstOrDefault(t => dropper.Tile._HasNeighbor(t));
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
                var movePoints = targets.Concat(targets.SelectMany(t => t.GetNeighbors()))
                    .Select(n => n.ToPoint());
                Move(dropper, movePoints);
            }

            Drop(dropper, targets, resource);
        }

        public static int GetMoveCost(Tile source, Tile dest)
        {
            if (source == dest)
            {
                return 0;
            }
            else if (source.GetNeighbor(source.FlowDirection) == dest)
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

        public static Tile ChooseNewLodgeLocation()
        {
            return AI._Game.Tiles.MinByValue(t => NewLodgeFitness(t));
        }

        public static float NewLodgeFitness(Tile tile)
        {
            if (tile.LodgeOwner != null || tile.Spawner != null || (tile.Beaver != null && tile.Beaver.Owner != AI._Player))
            {
                return float.MaxValue;
            }

            var friendlyBest = 5;
            var enemyMin = 7;
            var completeFactor = 5;

            var point = tile.ToPoint();

            var friendlyDistance = AI._Player.Lodges.Select(l => l.ToPoint().ManhattanDistance(point));
            var friendlyNearness = friendlyDistance.Any() ? friendlyDistance.Min() : 50;
            var friendlyFitness = Math.Abs(friendlyNearness - friendlyBest);

            var enemyDistance = AI._Player.Opponent.Lodges.Select(l => l.ToPoint().ManhattanDistance(point));
            var enemyNearness = enemyDistance.Any() ? enemyDistance.Min() : 50;
            var enemyFitness = Math.Max(enemyMin - enemyNearness, 0);

            var percentIncomplete = 1 - (tile.Branches / (float)AI._Player.BranchesToBuildLodge);
            var percentFitness = percentIncomplete * completeFactor;

            return friendlyFitness + enemyFitness + percentFitness;
        }

        public static Dictionary<Point, int> DistanceMap(IEnumerable<Point> sources, Func<Point, bool> isPassable)
        {
            var search = new AStar<Point>(
                sources,
                p => false,
                (p1, p2) => GetMoveCost(p1.ToTile(), p2.ToTile()),
                p => 0,
                p => p.ToTile().GetReachableNeighbors(3).Select(t => t.ToPoint())
            );

            return search.GScore;
        }
    }
}
