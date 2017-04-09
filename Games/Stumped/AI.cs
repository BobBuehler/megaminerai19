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
        public static Dictionary<string, Point> GoalLocations;

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
        public Job Bulky;
        public Job Builder;
        public Job Fighter;
        public Job Hungry;

        public IEnumerable<Tile> attackTargets;
        public HashSet<Point> harvestTrees;

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

            AI.GoalLocations = new Dictionary<string, Point>();

            this.Basic = this.Game.Jobs.First(j => j.Title == "Basic");
            this.Bulky = this.Game.Jobs.First(j => j.Title == "Bulky");
            this.Builder = this.Game.Jobs.First(j => j.Title == "Builder");
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
            if (this.Game.CurrentTurn < 2)
            {
                Console.WriteLine("STARTING POSITION: {0}", this.Player.Beavers[0].ToPoint());
            }
            Console.WriteLine("TURN:{0}, Beavers:{1}x{2}, Lodges:{3}x{4} ...", this.Game.CurrentTurn, this.Player.Beavers.Count, this.Player.Opponent.Beavers.Count, this.Player.Lodges.Count, this.Player.Opponent.Lodges.Count);

            AI.BeaverCount = AI._Player.Beavers.Count;
            AI.GoalLocations.Clear();
            this.Game.Beavers.ForEach(b => AI.GoalLocations[b.Id] = b.ToPoint());

            // OnTheBrink();
            
            BuildLodges();

            Attack();

            LodgeBuilders();

            Feast();

            BuildLodges();

            var useless = this.Player.Beavers.Where(b => b.CanAct() && b.CanMove());

            // Fall through
            foreach (Beaver b in this.Player.Beavers)
            {
                Solver.Pickup(b, this.Player.Opponent.Lodges, "branches");
                Solver.Attack(b, this.Player.Opponent.Beavers);
                Solver.Attack(b, useless);
            }

            Recruit();

            return true; // to signify that we are truly done with this turn
        }

        public void OnTheBrink()
        {
            if (this.Player.Beavers.Count == 1 && this.Player.Lodges.Count == 0 && this.Player.Beavers[0].Branches > 0)
            {
                var beaver = this.Player.Beavers[0];

                beaver.BuildLodge();
                var lodge = beaver.Tile;

                var openNeighbor = beaver.Tile.GetNeighbors().FirstOrDefault(n => n.IsPathable());
                if (openNeighbor != null)
                {
                    beaver.Move(openNeighbor);
                }
            }
        }

        public void Attack()
        {
            var angryBeavers = this.Player.Beavers.Where(b => b.Job != this.Builder && b.Job != this.Hungry).ToList();
            this.attackTargets = this.Player.Opponent.Lodges.Concat(this.Player.Opponent.Beavers.Where(b => b.CanBeAttacked()).Select(b => b.Tile));

            while (angryBeavers.Any())
            {
                if (this.Player.Opponent.Lodges.Count >= 8)
                {
                    this.attackTargets = this.Player.Opponent.Lodges;
                }

                var movePoints = this.attackTargets.SelectMany(t => t.GetNeighbors()).Select(t => t.ToPoint()).ToHashSet();

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

                var target = this.attackTargets.FirstOrDefault(t => t._HasNeighbor(beaver.Tile));
                if (target == null)
                {
                    continue;
                }

                if (target.LodgeOwner == this.Player.Opponent && beaver.OpenCarryCapacity() > target.Branches)
                {
                    if (beaver.CanAct())
                    {
                        beaver.Pickup(target, "branch");
                    }
                }
                else if (target.Beaver != null && target.Beaver.CanBeAttacked())
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
                        else if (beaver.Branches > 0)
                        {
                            beaver.Drop(beaver.Tile, "branch");
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                this.attackTargets = this.Player.Opponent.Lodges.Concat(this.Player.Opponent.Beavers.Where(b => b.CanBeAttacked()).Select(b => b.Tile));
            }
        }

        public void BuildLodges()
        {
            foreach (Beaver b in this.Player.Beavers.Where(b => b.Job != this.Builder))
            {
                BuildLodge(b);
            }
        }
        
        public void BuildLodge(Beaver beaver)
        {
            if (beaver.CanAct() && beaver.CanBuildLodge())
            {
                Console.WriteLine("    Build Lodge: {0}, {1}+{2}/{3}", beaver.ToPoint(), beaver.Branches, beaver.Tile.Branches, this.Player.BranchesToBuildLodge);
                beaver.BuildLodge();
            }
        }

        public void LodgeBuilders()
        {
            var builderBeavers = this.Player.Beavers.Where(b => b.Job == this.Builder).ToList();
            this.harvestTrees = this.Game.Spawner.Where(s => s.Type == "branches" && ValidTree(s)).Select(s => s.Tile.ToPoint()).ToHashSet();

            while(builderBeavers.Any() && this.harvestTrees.Any())
            {
                var treeNeighbors = this.harvestTrees.SelectMany(t => t.ToTile().GetNeighbors().Where(n => n.LodgeOwner == null && ValidTreeNeighbor(n))).Select(s => s.ToPoint()).ToHashSet();
                if (!treeNeighbors.Any())
                {
                    return;
                }

                var pairPath = Solver.GetClosestPath(builderBeavers, p => treeNeighbors.Contains(p), this.Builder.Moves).ToArray();
                if (pairPath.Length < 1)
                {
                    return;
                }

                var treeNeighbor = pairPath.Last().ToTile();
                var tree = treeNeighbor.GetNeighbors().First(n => this.harvestTrees.Contains(n.ToPoint())).Spawner;
                var beaver = pairPath.First().ToTile().Beaver;

                if (pairPath.Length == 1)
                {
                    beaver = builderBeavers.Where(b => b.Tile._HasNeighbor(tree.Tile)).MaxByValue(b => b.Branches + b.Tile.Branches);
                    pairPath = new[] { beaver.ToPoint() };
                }

                EngageBeaverAndTree(beaver, tree, pairPath);

                builderBeavers.Remove(beaver);
                this.harvestTrees.Remove(tree.Tile.ToPoint());
            }
        }

        public void EngageBeaverAndTree(Beaver beaver, Spawner tree, Point[] path)
        {
            var safeDistance = 5;
            var enemyDistance = 100;
            var opponentBeavers = this.Player.Opponent.Beavers.Where(b => b.Health > 0);
            if (opponentBeavers.Any())
            {
                enemyDistance = opponentBeavers.Min(b => b.ToPoint().ManhattanDistance(tree.Tile.ToPoint()));
            }
            if (tree.Tile.GetNeighbors().Where(n => ValidTreeNeighbor(n)).Count() > 1 || enemyDistance < safeDistance)
            {
                BuildLodge(beaver);
                Solver.MoveAlong(beaver, path);
                if (beaver.ToPoint().Equals(path.Last()))
                {
                    Solver.Drop(beaver, new[] { beaver.Tile }, "branches");
                    Solver.Harvest(beaver, new[] { tree });
                }
                BuildLodge(beaver);
            }
            else
            {
                var landmark = this.Game.Spawner.Where(s => s.Type == "branches" && s != tree).MaxByValue(t => t.Tile.ToPoint().ManhattanDistance(tree.Tile.ToPoint())).Tile;
                var roadPath = new AStar<Point>(
                    new[] { tree.Tile.ToPoint() },
                    p => landmark.HasNeighbor(p.ToTile()),
                    (p1, p2) => Solver.GetMoveCost(p1.ToTile(), p2.ToTile()),
                    p => 0,
                    p => p.ToTile().GetNeighbors().Where(n => n.Spawner == null && n.LodgeOwner == null).Select(t => t.ToPoint())
                ).CalcPathTo(landmark.ToPoint()).ToHashSet();

                var dropOffSearch = new AStar<Point>(
                    new[] { beaver.ToPoint() },
                    p => !roadPath.Contains(p) && p.ToTile().FlowDirection == "",
                    (p1, p2) => Solver.GetMoveCost(p1.ToTile(), p2.ToTile()),
                    p => 0,
                    p => p.ToTile().GetReachableNeighbors(this.Builder.Moves).Select(t => t.ToPoint())
                );

                var dropOff = dropOffSearch.GScore
                    .Where(g => !roadPath.Contains(g.Key))
                    .OrderByDescending(g => g.Key.ToTile().Branches)
                    .MinByValue(g => g.Value).Key;

                var dropOffPath = dropOffSearch.CalcPathTo(dropOff);

                if (beaver.ToPoint().Equals(dropOff))
                {
                    BuildLodge(beaver);
                }
    
                if (!beaver.FullLoad())
                {
                    Solver.MoveAlong(beaver, path);
                    Solver.Harvest(beaver, new[] { tree });
                }
                else
                {
                    Solver.MoveAlong(beaver, dropOffPath);
                    if (beaver.ToPoint().Equals(dropOffPath.Last()) || (beaver.Branches + dropOff.ToTile().Branches) < this.Player.BranchesToBuildLodge)
                    {
                        Solver.Drop(beaver, new[] { dropOff.ToTile() }, "branches");
                    }
                }
            }
        }
        
        public bool ValidTree(Spawner tree)
        {
            return tree.Tile.GetNeighbors().Where(n => n.LodgeOwner == null && n.Spawner == null && ValidTreeNeighbor(n)).Count() > 0;
        }

        public bool ValidTreeNeighbor(Tile t)
        {
            return t.Spawner == null && t.LodgeOwner == null && (this.Player.BranchesToBuildLodge < this.Builder.CarryLimit || t.FlowDirection == "");
        }

        public void CoordinateBuildLodges()
        {
            var targetDrop = Solver.ChooseNewLodgeLocation();

            this.Player.Beavers
                .Where(b => b.ToPoint().ManhattanDistance(targetDrop.ToPoint()) < 5)
                .ForEach(b => Solver.MoveAndDrop(b, new[] { targetDrop }, "branch"));

            foreach (Beaver b in this.Player.Beavers.Where(b => b.Job.Title == "Builder"))
            {
                Solver.MoveAndHarvest(b, this.Game.Spawner.Where(s => s.Type.StartsWith("b")));
                Solver.MoveAndDrop(b, new[] { targetDrop }, "branch");
            }
        }

        public void Feast()
        {
            var hungryBeavers = this.Player.Beavers.Where(b => b.Job == this.Hungry && b.Food < 12 && b.OpenCarryCapacity() > 0).ToList();
            var cattails = this.Game.Cattails().Select(s => s.Tile.ToPoint()).ToHashSet();

            while (hungryBeavers.Any() && cattails.Any())
            {
                var cattailNeighbors = cattails.SelectMany(c => c.ToTile().GetNeighbors()).Select(n => n.ToPoint()).ToHashSet();

                var pairPath = Solver.GetClosestPath(hungryBeavers, p => cattailNeighbors.Contains(p), this.Hungry.Moves).ToArray();
                if (pairPath.Length < 1)
                {
                    break;
                }

                var cattailNeighbor = pairPath.Last().ToTile();
                var cattail = cattailNeighbor.GetNeighbors().First(n => cattails.Contains(n.ToPoint())).Spawner;
                var beaver = pairPath.First().ToTile().Beaver;

                if (pairPath.Length == 1)
                {
                    beaver = hungryBeavers.Where(b => b.Tile._HasNeighbor(cattail.Tile)).MaxByValue(b => b.Food);
                    pairPath = new[] { beaver.ToPoint() };
                }

                EngageBeaverAndFood(beaver, cattail, pairPath);

                hungryBeavers.Remove(beaver);
                cattails.Remove(cattail.Tile.ToPoint());
            }

            var fullBeavers = this.Player.Beavers.Where(b => b.Food >= 12).ToList();
            while (fullBeavers.Any())
            {
                var lodgeNeighbors = this.Player.Lodges.SelectMany(l => l.GetNeighbors()).Where(n => n.IsPathable()).Select(n => n.ToPoint()).ToHashSet();
                var pairPath = Solver.GetClosestPath(fullBeavers, p => lodgeNeighbors.Contains(p), this.Hungry.Moves).ToArray();
                if (pairPath.Length < 1)
                {
                    break;
                }

                var lodgeNeighbor = pairPath.Last().ToTile();
                var lodge = lodgeNeighbor.GetNeighbors().First(n => n.LodgeOwner == this.Player);
                var beaver = pairPath.First().ToTile().Beaver;

                MoveBeaverAndDrop(beaver, lodge, pairPath);

                fullBeavers.Remove(beaver);
            }
        }

        public void EngageBeaverAndFood(Beaver beaver, Spawner cattail, Point[] path)
        {
            Solver.MoveAlong(beaver, path);
            if (beaver.ToPoint().Equals(path.Last()))
            {
                Solver.Harvest(beaver, new[] { cattail });
            }
        }

        public void MoveBeaverAndDrop(Beaver beaver, Tile lodge, Point[] path)
        {
            Solver.MoveAlong(beaver, path);
            if (beaver.ToPoint().Equals(path.Last()))
            {
                Solver.Drop(beaver, new[] { lodge }, "food");
            }
        }

        public void Recruit()
        {
            var didRecruit = true;
            while (didRecruit)
            {
                var counts = this.Game.Jobs.ToDictionary(j => j.Title, j => 0);
                this.Player.Beavers.ForEach(b => counts[b.Job.Title]++);

                if (counts["Hungry"] < AI.BeaverCount / 7 && ShouldSpawnHungry())
                {
                    Console.WriteLine("HUNGRY!!!");
                    didRecruit = Recruit(this.Hungry, this.Game.Cattails().Select(s => s.Tile.ToPoint()));
                }
                else if (counts["Fighter"] <= counts["Builder"] || !this.harvestTrees.Any())
                {
                    didRecruit = Recruit(this.Fighter, this.attackTargets.Select(t => t.ToPoint()));
                }
                else if (this.harvestTrees.Any())
                {
                    didRecruit = Recruit(this.Builder, this.harvestTrees);
                }

                if (didRecruit)
                {
                    AI.BeaverCount++;
                }
            }
        }

        public bool Recruit(Job job, IEnumerable<Point> targets)
        {
            if (!targets.Any())
            {
                return false;
            }

            var recruitersInOrder = this.Player.Lodges.OrderBy(l => targets.Min(t => t.ManhattanDistance(l.ToPoint())));
            var recruiters = recruitersInOrder.Where(l => l.CanRecruit(job));
            if (!recruiters.Any())
            {
                var recruiter = recruitersInOrder
                    .Where(r => r.Food >= job.CurrentCost() && r.Beaver != null)
                    .FirstOrDefault(r => Solver.MoveOff(r.Beaver));
                if (recruiter == null)
                {
                    return false;
                }
                recruiters = new[] { recruiter };
            }

            var lodge = recruiters.First();

            return lodge != null && job.Recruit(lodge) != null;
        }

        public bool ShouldSpawnHungry()
        {
            var nearness = Solver.MinManhattanDistance(this.Player.Lodges.ToPoints(), this.Game.Cattails().ToPoints());
            if (this.Game.CurrentTurn < 70)
            {
                return nearness <= 2;
            }
            if (this.Game.CurrentTurn < 150)
            {
                return nearness <= 3;
            }
            return nearness <= 4;
        }

        #endregion

    }
}
