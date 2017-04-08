using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joueur.cs.Games.Stumped
{
    static class Solver
    {
        public static void MoveAndAttack(IEnumerable<Beaver> attackers, IEnumerable<Beaver> targets)
        {
            var targetPoints = targets.SelectMany(t => t.Tile.GetNeighbors().Select(n => n.ToPoint())).ToHashSet();

            var search = new AStar<Point>(
                attackers.Select(b => b.ToPoint()),
                p => targetPoints.Contains(p),
                (p1, p2) => GetMoveCost(p1.ToTile(), p2.ToTile()),
                p => 0,
                p => p.ToTile().GetNeighbors().Select(t => t.ToPoint())
            );


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
    }
}
