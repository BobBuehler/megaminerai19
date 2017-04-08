using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joueur.cs.Games.Stumped
{
    static class Solver
    {
        public static bool CanBuildLodge(this Beaver b)
        {
            return b.Branches + b.Tile.Branches >= b.Owner.BranchesToBuildLodge && b.Tile.LodgeOwner == null;
        }

        public static bool CanAct(this Beaver b)
        {
            return b.Health > 0 && b.Actions > 0 && b.TurnsDistracted == 0;
        }
    }
}
