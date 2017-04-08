using System;

namespace Joueur.cs.Games.Stumped
{
    public struct Point
    {
        public int x;
        public int y;

        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            Point o = (Point)obj;
            return o.x == x && o.y == y;
        }

        public override int GetHashCode()
        {
            return AI._Game.MapWidth * y + x;
        }

        public override string ToString()
        {
            return String.Format("({0},{1})", x, y);
        }
    }
}