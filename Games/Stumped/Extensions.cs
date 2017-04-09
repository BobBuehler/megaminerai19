using System;
using System.Linq;
using System.Collections.Generic;


namespace Joueur.cs.Games.Stumped
{
    static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var s in source)
            {
                action(s);
            }
        }

        public static T MinByValue<T, K>(this IEnumerable<T> source, Func<T, K> selector)
        {
            var comparer = Comparer<K>.Default;

            var enumerator = source.GetEnumerator();
            enumerator.MoveNext();

            var min = enumerator.Current;
            var minV = selector(min);

            while (enumerator.MoveNext())
            {
                var s = enumerator.Current;
                var v = selector(s);
                if (comparer.Compare(v, minV) < 0)
                {
                    min = s;
                    minV = v;
                }
            }
            return min;
        }

        public static T MaxByValue<T, K>(this IEnumerable<T> source, Func<T, K> selector)
        {
            var comparer = Comparer<K>.Default;

            var enumerator = source.GetEnumerator();
            enumerator.MoveNext();

            var max = enumerator.Current;
            var maxV = selector(max);

            while (enumerator.MoveNext())
            {
                var s = enumerator.Current;
                var v = selector(s);
                if (comparer.Compare(v, maxV) > 0)
                {
                    max = s;
                    maxV = v;
                }
            }
            return max;
        }

        public static IEnumerable<T> While<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext() && predicate(enumerator.Current))
            {
                yield return enumerator.Current;
            }
        }

        public static Func<T, TResult> Memoize<T, TResult>(this Func<T, TResult> func, IDictionary<T, TResult> cache = null)
        {
            cache = cache ?? new Dictionary<T, TResult>();
            return t =>
            {
                TResult result;
                if (!cache.TryGetValue(t, out result))
                {
                    result = func(t);
                    cache[t] = result;
                }
                return result;
            };
        }

        public static Func<T, TResult> LRUMemoize<T, TResult>(this Func<T, TResult> func, int capacity)
        {
            var cache = new LRUCache<T, TResult>(capacity);
            cache.OnMiss = delegate (T input, out TResult output) { output = func(input); return true; };
            return t => cache[t];
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static Queue<T> ToQueue<T>(this IEnumerable<T> source)
        {
            return new Queue<T>(source);
        }

        public static Point ToPoint(this Tile tile)
        {
            return new Point(tile.X, tile.Y);
        }

        public static Point ToPoint(this Beaver beaver)
        {
            return beaver.Tile.ToPoint();
        }

        public static Tile ToTile(this Point point)
        {
            return AI._Game.GetTileAt(point.x, point.y);
        }

        public static Beaver ToBeaver(this Point point)
        {
            return point.ToTile().Beaver;
        }
        public static bool CanBuildLodge(this Beaver beaver)
        {
            return beaver.Branches + beaver.Tile.Branches >= beaver.Owner.BranchesToBuildLodge && beaver.Tile.LodgeOwner == null;
        }

        public static bool CanAct(this Beaver beaver)
        {
            return beaver.Health > 0 && beaver.Actions > 0 && beaver.TurnsDistracted == 0 && beaver.Recruited == true;
        }

        public static bool CanMove(this Beaver beaver)
        {
            return beaver.Health > 0 && beaver.Moves > 0 && beaver.TurnsDistracted == 0 && beaver.Recruited == true;
        }

        public static bool CanBeAttacked(this Beaver beaver)
        {
            return beaver.Owner == AI._Player.Opponent && beaver.Health > 0 && beaver.Recruited == true;
        }

        public static int OpenCarryCapacity(this Beaver beaver)
        {
            return beaver.Job.CarryLimit - (beaver.Branches + beaver.Food);
        }

        public static int CurrentCost(this Job job)
        {
            return AI.BeaverCount < AI._Game.FreeBeaversCount ? 0 : job.Cost;
        }
        
        public static bool CanRecruit(this Tile tile, Job job)
        {
            return tile.LodgeOwner == AI._Player && tile.Beaver == null && tile.Food >= job.CurrentCost();
        }
        
        public static bool FullLoad(this Beaver b)
        {
            return b.Food + b.Branches == b.Job.CarryLimit;
        }

        public static Tile GetNeighbor(this Tile tile, string direction)
        {
            switch (direction)
            {
                case "North":
                    return tile.TileNorth;
                case "East":
                    return tile.TileEast;
                case "South":
                    return tile.TileSouth;
                case "West":
                    return tile.TileWest;
                default:
                    return tile;
            }
        }

        public static int GetCount(this Tile tile, string resource)
        {
            return resource[0] == 'f' ? tile.Food : tile.Branches;
        }

        public static int GetCount(this Beaver beaver, string resource)
        {
            return resource[0] == 'f' ? beaver.Food : beaver.Branches;
        }

        public static int ManhattanDistance(this Point p1, Point p2)
        {
            return Math.Abs(p1.x - p2.x) + Math.Abs(p1.y - p2.y);
        }

        public static int ManhattanDistance(this Tile t1, Tile t2)
        {
            return Math.Abs(t1.X - t2.X) + Math.Abs(t1.Y - t2.Y);
        }

        public static bool _HasNeighbor(this Tile t1, Tile t2)
        {
            return (t1.TileNorth == t2 || t1.TileEast == t2 || t1.TileSouth == t2 || t1.TileWest == t2);
        }

        public static IEnumerable<Spawner> Trees(this Game game)
        {
            return game.Spawner.Where(s => s.Type == "brancges");
        }

        public static IEnumerable<Spawner> Cattails(this Game game)
        {
            return game.Spawner.Where(s => s.Type == "food");
        }
    }
}