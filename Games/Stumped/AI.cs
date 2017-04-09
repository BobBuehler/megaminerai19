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
        public static int BeaverCount;

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

        public Job Basic;
        public Job Fighter;
        public Job Hungry;

        #region Methods
        /// <summary>
        /// This returns your AI's name to the game server. Just replace the string.
        /// </summary>
        /// <returns>string of you AI's name.</returns>
        public override string GetName()
        {
            return "Rodents of Mass Deforestation";
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

            this.Basic = this.Game.Jobs.First(j => j.Title == "Basic");
            this.Fighter = this.Game.Jobs.First(j => j.Title == "Fighter");
            this.Hungry = this.Game.Jobs.First(j => j.Title == "Hungry");
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
            Console.WriteLine($"My Turn {this.Game.CurrentTurn}");
            AI.BeaverCount = AI._Player.Beavers.Count;
            
            BuildLodges();

            Attack();

            HungryLodgeBuilders();

            BuildLodges();

            // Fall through
            foreach (Beaver b in this.Player.Beavers)
            {
                Solver.Pickup(b, this.Player.Opponent.Lodges, "branches");
                Solver.Attack(b, this.Player.Opponent.Beavers);
            }

            Recruit();

            Console.WriteLine("Done with our turn");
            return true; // to signify that we are truly done with this turn
        }

        public void Attack()
        {
            var angryBeavers = this.Player.Beavers.Where(b => b.Job == this.Basic || b.Job == this.Fighter).ToList();

            while(angryBeavers.Any())
            {
                var targetTiles = this.Player.Opponent.Lodges.Concat(this.Player.Opponent.Beavers.Where(b => b.CanBeAttacked()).Select(b => b.Tile));
                if (this.Player.Opponent.Lodges.Count >= 8)
                {
                    targetTiles = this.Player.Opponent.Lodges;
                }

                var movePoints = targetTiles.SelectMany(t => t.GetNeighbors()).Select(t => t.ToPoint()).ToHashSet();

                var pairPath = Solver.GetClosestPath(angryBeavers, p => movePoints.Contains(p), this.Fighter.Moves).ToArray();
                if (pairPath.Length < 1)
                {
                    return;
                }
                
                var beaver = pairPath.First().ToTile().Beaver;
                angryBeavers.Remove(beaver);

                if (pairPath.Length > 1)
                {
                    Solver.MoveAlong(beaver, pairPath);
                }

                var target = targetTiles.FirstOrDefault(t => t._HasNeighbor(beaver.Tile));
                if (target == null)
                {
                    continue;
                }

                if (target.Beaver != null && target.Beaver.CanBeAttacked())
                {
                    while (beaver.CanAct())
                    {
                        beaver.Attack(target.Beaver);
                    }
                }
                else if (target.LodgeOwner == this.Player.Opponent)
                {
                    while (beaver.CanAct())
                    {
                        if (beaver.OpenCarryCapacity() > 0)
                        {
                            beaver.Pickup(target, "branch");
                        }
                        else
                        {
                            beaver.Drop(beaver.Tile, "branch");
                        }
                    }
                }
            }
        }

        public void BuildLodges()
        {
            foreach (Beaver b in this.Player.Beavers)
            {
                BuildLodge(b);
            }
        }
        
        public void BuildLodge(Beaver beaver)
        {
            if (beaver.CanAct() && beaver.CanBuildLodge())
            {
                beaver.BuildLodge();
            }
        }

        public void HungryLodgeBuilders()
        {
            var hungryBeavers = this.Player.Beavers.Where(b => b.Job == this.Hungry).ToList();
            var trees = this.Game.Spawner.Where(s => s.Type == "branches" && ValidTree(s)).Select(s => s.Tile.ToPoint()).ToHashSet();

            while(hungryBeavers.Any() && trees.Any() && trees.Any())
            {
                var treeNeighbors = trees.SelectMany(t => t.ToTile().GetNeighbors().Where(n => n.LodgeOwner == null)).ToHashSet();
                if (!treeNeighbors.Any())
                {
                    return;
                }

                var pairPath = Solver.GetClosestPath(hungryBeavers, p => treeNeighbors.Contains(p.ToTile()), this.Hungry.Moves).ToArray();
                if (pairPath.Length < 1)
                {
                    return;
                }

                var treeNeighbor = pairPath.Last().ToTile();
                var tree = treeNeighbor.GetNeighbors().First(n => trees.Contains(n.ToPoint())).Spawner;
                var beaver = pairPath.First().ToTile().Beaver;

                if (pairPath.Length == 1)
                {
                    beaver = hungryBeavers.Where(b => b.Tile._HasNeighbor(tree.Tile)).MaxByValue(b => b.Branches + b.Tile.Branches);
                    pairPath = new[] { beaver.ToPoint() };
                }

                EngageBeaverAndTree(beaver, tree, pairPath);

                hungryBeavers.Remove(beaver);
                trees.Remove(tree.Tile.ToPoint());
            }
        }

        public void EngageBeaverAndTree(Beaver beaver, Spawner tree, Point[] path)
        {
            BuildLodge(beaver);
            Solver.MoveAlong(beaver, path);
            if (path.Length <= 2)
            {
                Solver.Drop(beaver, new[] { beaver.Tile }, "branches");
                Solver.Harvest(beaver, new[] { tree });
            }
            BuildLodge(beaver);
        }
        
        public bool ValidTree(Spawner tree)
        {
            return tree.Tile.GetNeighbors().Where(n => n.LodgeOwner == null && n.Spawner == null).Count() > 0;
        }

        public void CoordinateBuildLodges()
        {
            var targetDrop = Solver.ChooseNewLodgeLocation();

            this.Player.Beavers
                .Where(b => b.ToPoint().ManhattanDistance(targetDrop.ToPoint()) < 5)
                .ForEach(b => Solver.MoveAndDrop(b, new[] { targetDrop }, "branch"));

            foreach (Beaver b in this.Player.Beavers.Where(b => b.Job.Title == "Hungry"))
            {
                Solver.MoveAndHarvest(b, this.Game.Spawner.Where(s => s.Type.StartsWith("b")));
                Solver.MoveAndDrop(b, new[] { targetDrop }, "branch");
            }
        }

        public void Recruit()
        {
            var jobs = this.Game.Jobs.ToLookup(j => j.Title);
            
            var counts = this.Player.Beavers.GroupBy(b => b.Job.Title).ToDictionary(g => g.Key, g => g.Count());
            this.Game.Jobs.ForEach(j =>
            {
                if (!counts.ContainsKey(j.Title))
                {
                    counts[j.Title] = 0;
                }
            });

            var didRecruit = true;
            while (didRecruit)
            {
                if (counts["Fighter"] < counts["Hungry"])
                {
                    didRecruit = Recruit(jobs["Fighter"].First());
                }
                else
                {
                    didRecruit = Recruit(jobs["Hungry"].First());
                }
                if (didRecruit)
                {
                    AI.BeaverCount++;
                }
            }
        }

        public bool Recruit(Job job)
        {
            var lodge = this.Player.Lodges.FirstOrDefault(l => l.CanRecruit(job));
            return lodge != null && job.Recruit(lodge) != null;
        }

        #endregion

    }
}
