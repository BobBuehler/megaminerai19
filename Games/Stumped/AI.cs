// This is where you build your AI for the Stumped game.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Joueur.cs.Games.Stumped
{
    /// <summary>
    /// This is where you build your AI for the Stumped game.
    /// </summary>
    class AI : BaseAI
    {
        public static Game _Game;
        public static Player _Player;

        #region Properties
        #pragma warning disable 0169 // the never assigned warnings between here are incorrect. We set it for you via reflection. So these will remove it from the Error List.
        #pragma warning disable 0649
        /// <summary>
        /// This is the Game object itself, it contains all the information about the current game
        /// </summary>
        public readonly Stumped.Game Game;
        /// <summary>
        /// This is your AI's player. This AI class is not a player, but it should command this Player.
        /// </summary>
        public readonly Stumped.Player Player;
        #pragma warning restore 0169
        #pragma warning restore 0649

        // you can add additional properties here for your AI to use
        #endregion


        #region Methods
        /// <summary>
        /// This returns your AI's name to the game server. Just replace the string.
        /// </summary>
        /// <returns>string of you AI's name.</returns>
        public override string GetName()
        {
            return "Stumped C# Player"; // REPLACE THIS WITH YOUR TEAM NAME!
        }

        /// <summary>
        /// This is automatically called when the game first starts, once the Game object and all GameObjects have been initialized, but before any players do anything.
        /// </summary>
        /// <remarks>
        /// This is a good place to initialize any variables you add to your AI, or start tracking game objects.
        /// </remarks>
        public override void Start()
        {
            base.Start();
            AI._Game = this.Game;
            AI._Player = this.Player;
        }

        /// <summary>
        /// This is automatically called every time the game (or anything in it) updates.
        /// </summary>
        /// <remarks>
        /// If a function you call triggers an update this will be called before that function returns.
        /// </remarks>
        public override void GameUpdated()
        {
            base.GameUpdated();
        }

        /// <summary>
        /// This is automatically called when the game ends.
        /// </summary>
        /// <remarks>
        /// You can do any cleanup of you AI here, or do custom logging. After this function returns the application will close.
        /// </remarks>
        /// <param name="won">true if your player won, false otherwise</param>
        /// <param name="reason">a string explaining why you won or lost</param>
        public override void Ended(bool won, string reason)
        {
            base.Ended(won, reason);
        }


        /// <summary>
        /// This is called every time it is this AI.player's turn.
        /// </summary>
        /// <returns>Represents if you want to end your turn. True means end your turn, False means to keep your turn going and re-call this function.</returns>
        public bool RunTurn()
        {
            // This is your Stumped ShellAI
            // ShellAI is intended to be a simple AI that does everything possible in the game, but plays the game very poorly
            // This example code does the following:
            // 1. Grabs a single beaver
            // 2. tries to move the beaver
            // 3. tries to do one of the 5 actions on it
            // 4. Grabs a lodge and tries to recruit a new beaver

            // NOTE: If you're executing from Visual Studio (or a similar IDE), you should modify the project's Properties file
            //       and set your session name in the Debug tab

            // First let's do a simple print statement telling us what turn we are on
            Console.WriteLine($"My Turn {this.Game.CurrentTurn}");

            foreach(Beaver b in this.Player.Beavers.Where(b => b.CanAct()))
            {
                if (b.CanBuildLodge())
                {
                    b.BuildLodge();
                }
            }
			
			foreach(Beaver b in this.Player.Beavers.Where(b => b.Job.Title == "Hungry"))
			{
			    Solver.MoveAndHarvest(b, this.Game.Spawner.Where(s => !s.Type.StartsWith("f")));
			}
			
			// Move and drop full loads at lodges (TODO: check move/act conditions)
			var dropOffs = new List<Tile>();
			foreach(Tile lodge in this.Player.Lodges)
			{
			    var neighbors = lodge.GetNeighbors();
			    var branchPiles = neighbors.Where(n => n.Branches > 0);
			    if (branchPiles.Any())
			    {
			        dropOffs.AddRange(branchPiles);
			    } else {
			        dropOffs.AddRange(neighbors);
			    }
			}
			foreach(Beaver b in this.Player.Beavers.Where(b => b.FullLoad() && !b.CanBuildLodge()))
			{
			    Solver.MoveAndDrop(b, dropOffs, "branch");
			}
			
			// Attack Enemy Beavers with Fighters
			foreach(Beaver b in this.Player.Beavers.Where(b => b.Job.Title == "Fighter" || b.Job.Title == "Basic"))
			{
			    Solver.MoveAndAttack(b, this.Player.Opponent.Beavers);
			}
            
            // Recruit Fighters!
            List<Job> recruitJobs = this.Game.Jobs.Where(j => j.Title == "Fighter" || j.Title == "Hungry").ToList();
			foreach(Tile l in this.Player.Lodges)
			{
			    Job job = RandomElement<Job>(recruitJobs);
				if (l.CanRecruit(job))
				{
					// TODO: Print error message on failure (null return)
					job.Recruit(l);
				}
			}

            Console.WriteLine("Done with our turn");
            return true; // to signify that we are truly done with this turn
        }

        // A random number generator, used for ShellAI. Feel free to remove if you gut C
        public Random rand = new Random();

        /// <summary>
        /// Simply returns a random element of an array
        /// </summary>
        public T RandomElement<T>(IList<T> items) where T : class
        {
            return items.Any() ? items[rand.Next(items.Count())] : null;
        }

        /// <summary>
        /// Simply returns a shuffled copy of an array
        /// </summary>
        public IList<T> Shuffled<T>(IList<T> a)
        {
            a = a.ToList();
            for (int i = a.Count(); i > 0; i--)
            {
                int j = rand.Next(i);
                T x = a[i - 1];
                a[i - 1] = a[j];
                a[j] = x;
            }
            return a;
        }

        /// <summary>
        /// A very basic path finding algorithm (Breadth First Search) that when given a starting Tile, will return a valid path to the goal Tile.
        /// </summary>
        /// <remarks>
        /// This is NOT an optimal pathfinding algorithm. It is intended as a stepping stone if you want to improve it.
        /// </remarks>
        /// <param name="start">the starting Tile</param>
        /// <param name="goal">the goal Tile</param>
        /// <returns>A List of Tiles representing the path, the the first element being a valid adjacent Tile to the start, and the last element being the goal. Or an empty list if no path found.</returns>
        List<Tile> FindPath(Tile start, Tile goal)
        {
            // no need to make a path to here...
            if (start == goal)
            {
                return new List<Tile>();
            }

            // the tiles that will have their neighbors searched for 'goal'
            Queue<Tile> fringe = new Queue<Tile>();

            // How we got to each tile that went into the fringe.
            Dictionary<Tile, Tile> cameFrom = new Dictionary<Tile, Tile>();

            // Enqueue start as the first tile to have its neighbors searched.
            fringe.Enqueue(start);

            // keep exploring neighbors of neighbors... until there are no more.
            while (fringe.Count > 0)
            {
                // the tile we are currently exploring.
                Tile inspect = fringe.Dequeue();

                // cycle through the tile's neighbors.
                foreach (Tile neighbor in inspect.GetNeighbors())
                {
                    if (neighbor == goal)
                    {
                        // Follow the path backward starting at the goal and return it.
                        List<Tile> path = new List<Tile>();
                        path.Add(goal);

                        // Starting at the tile we are currently at, insert them retracing our steps till we get to the starting tile
                        for (Tile step = inspect; step != start; step = cameFrom[step])
                        {
                            path.Insert(0, step);
                        }

                        return path;
                    }

                    // if the tile exists, has not been explored or added to the fringe yet, and it is pathable
                    if (neighbor != null && !cameFrom.ContainsKey(neighbor) && neighbor.IsPathable())
                    {
                        // add it to the tiles to be explored and add where it came from.
                        fringe.Enqueue(neighbor);
                        cameFrom.Add(neighbor, inspect);
                    }

                } // foreach(neighbor)

            } // while(fringe not empty)

            // if you're here, that means that there was not a path to get to where you want to go.
            //   in that case, we'll just return an empty path.
            return new List<Tile>();
        }

        #endregion

    }
}
