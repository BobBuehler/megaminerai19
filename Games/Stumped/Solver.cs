﻿using System;
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

            if (mover.CanMove())
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

        public static void MoveAlong(Beaver beaver, IEnumerable<Point> steps, bool dontStopInDanger = false)
        {
            if (!steps.Any())
            {
                return;
            }

            IEnumerable<Point> dontStops = new Point[0];
            if (dontStopInDanger)
            {
                var fears = AI._Player.Opponent.Beavers
                    .Where(b => b.CanBeAttacked())
                    .Where(b => b.Job == AI.Basic || b.Job == AI.Fighter || b.Job == AI.Bulky || b.Job == AI.HotLady)
                    .Where(b => !b.Tile.GetNeighbors().Any(n => n.Beaver != null && n.Beaver.Owner == AI._Player));
                if (fears.Any())
                {
                    dontStops = AI._Game.Tiles.Where(t => fears.Select(b => b.ToPoint().ManhattanDistance(t.ToPoint())).Min() == 2)
                        .ToPoints()
                        .ToHashSet();
                }
            }

            AI.GoalLocations[beaver.Id] = steps.Last();
            if (!beaver.CanMove())
            {
                return;
            }

            var queue = steps.SkipWhile(p => p.Equals(beaver.ToPoint())).ToQueue();
            while (queue.Count > 0 && queue.Peek().ToTile().IsPathable() && GetMoveCost(beaver.Tile, queue.Peek().ToTile()) <= beaver.Moves && !dontStops.Contains(queue.Peek()))
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
                p => p.ToTile().GetReachableNeighbors(moves, beavers.ToPoints()).Select(t => t.ToPoint())
            );

            return search.Path;
        }

        public static void Attack(Beaver attacker, IEnumerable<Beaver> targets)
        {
            var targettables = targets.Where(t => t.CanBeAttacked());
            var target = targettables.FirstOrDefault(t => attacker.Tile._HasNeighbor(t.Tile));
            if (target != null)
            {
                while (attacker.CanAct() && target.CanBeAttacked())
                {
                    if (attacker.Owner == target.Owner)
                    {
                        Console.WriteLine("DIE SLACKER");
                    }
                    attacker.Attack(target);
                }
            }
        }

        public static void MoveAndAttack(Beaver attacker, IEnumerable<Beaver> targets)
        {
            if (attacker.CanMove())
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
            if (!harvester.CanAct() || harvester.OpenCarryCapacity() == 0)
            {
                return;
            }

            var targettables = targets.Where(t => t.Health > 0);
            var target = targettables.FirstOrDefault(t => harvester.Tile._HasNeighbor(t.Tile));
            if (target != null)
            {
                while (harvester.CanAct() && target.Health > 0)
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
            
            if (harvester.CanMove())
            {
                var movePoints = spawners
                    .SelectMany(s => s.Tile.GetNeighbors().Select(n => n.ToPoint()));

                Move(harvester, movePoints);
            }

            Harvest(harvester, spawners);
        }

        public static void Pickup(Beaver picker, IEnumerable<Tile> targets, string resource)
        {
            if (!picker.CanAct() || picker.OpenCarryCapacity() == 0)
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
            if (!picker.CanAct() || picker.OpenCarryCapacity() == 0)
            {
                return;
            }

            var targetPoints = targets.Where(t => t.GetCount(resource) > 0);
            if (picker.CanMove())
            {
                var movePoints = targetPoints.Concat(targetPoints.SelectMany(t => t.GetNeighbors()))
                    .Select(n => n.ToPoint());
                Move(picker, movePoints);
            }

            Pickup(picker, targets, resource);
        }

        public static void Drop(Beaver dropper, IEnumerable<Tile> targets, string resource)
        {
            if (!dropper.CanAct() || dropper.GetCount(resource) == 0)
            {
                return;
            }

            var target = targets.FirstOrDefault(t => t == dropper.Tile || dropper.Tile._HasNeighbor(t));
            if (target != null)
            {
                dropper.Drop(target, resource, dropper.GetCount(resource));
            }
        }

        public static void MoveAndDrop(Beaver dropper, IEnumerable<Tile> targets, string resource)
        {
            if (!dropper.CanAct() || dropper.GetCount(resource) == 0)
            {
                return;
            }
            
            if (dropper.CanMove())
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

        public static IEnumerable<Tile> GetReachableNeighbors(this Tile tile, int jobMoves, IEnumerable<Point> starts)
        {
            if (starts.Contains(tile.ToPoint()))
            {
                return GetReachableNeighbors(tile, jobMoves);
            }
            var pathable = tile.GetNeighbors().Where(t => t.Spawner == null && t.LodgeOwner == null && WillNotHaveBeaver(t));
            return pathable.Where(n => GetMoveCost(tile, n) <= jobMoves);
        }

        public static bool WillNotHaveBeaver(Tile tile)
        {
            return tile.Beaver == null || !tile.ToPoint().Equals(AI.GoalLocations[tile.Beaver.Id]);
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

        public static AStar<Point> Search(IEnumerable<Point> sources, int moves)
        {
            var search = new AStar<Point>(
                sources,
                p => false,
                (p1, p2) => GetMoveCost(p1.ToTile(), p2.ToTile()),
                p => 0,
                p => p.ToTile().GetReachableNeighbors(moves).Select(t => t.ToPoint())
            );

            return search;
        }

        public static int CalcTurnsToMove(Beaver beaver, Point end, AStar<Point> search)
        {
            var path = search.CalcPathTo(end);
            var turns = 0;
            var moves = beaver.Moves;
            foreach (var p in path)
            {
                var cost = GetMoveCost(beaver.Tile, p.ToTile());
                moves -= cost;
                if (moves < 0)
                {
                    turns++;
                    moves = beaver.Job.Moves - cost;
                }
            }
            return turns;
        }

        public static bool MoveOff(Beaver beaver)
        {
            if (!beaver.CanMove())
            {
                return false;
            }

            var target = beaver.Tile.GetNeighbors().FirstOrDefault(n => n.IsPathable() && beaver.Moves >= GetMoveCost(beaver.Tile, n));
            return target != null && beaver.Move(target);
        }

        public static int MinManhattanDistance(IEnumerable<Point> set1, IEnumerable<Point> set2)
        {
            return set1.Select(s1 => set2.Select(s2 => s1.ManhattanDistance(s2)).Min()).Min();
        }

        public static IEnumerable<Point> ToPoints(this IEnumerable<Tile> tiles)
        {
            return tiles.Select(t => t.ToPoint());
        }

        public static IEnumerable<Point> ToPoints(this IEnumerable<Spawner> spawners)
        {
            return spawners.Select(s => s.Tile.ToPoint());
        }

        public static IEnumerable<Point> ToPoints(this IEnumerable<Beaver> beavers)
        {
            return beavers.Select(b => b.Tile.ToPoint());
        }
    }
}
