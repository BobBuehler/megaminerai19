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
            Console.WriteLine($"My Turn {this.Game.CurrentTurn}");

            foreach(Beaver b in this.Player.Beavers.Where(b => b.CanAct()))
            {
                if (b.CanBuildLodge())
                {
                    b.BuildLodge();
                }
            }

            CoordinateBuildLodges();
			
			// Fall through
			foreach(Beaver b in this.Player.Beavers)
            {
                Solver.MoveAndPickup(b, this.Player.Opponent.Lodges, "branches");
                Solver.MoveAndAttack(b, this.Player.Opponent.Beavers);
                Solver.MoveAndHarvest(b, this.Game.Spawner);
                Solver.MoveAndDrop(b, this.Player.Lodges, "food");
            }

            Recruit();

            Console.WriteLine("Done with our turn");
            return true; // to signify that we are truly done with this turn
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
            Console.WriteLine("Recruit Start: Beaver Count {0}", this.Player.Beavers.Count);
            var jobs = this.Game.Jobs.ToLookup(j => j.Title);
            
            var counts = this.Player.Beavers.GroupBy(b => b.Job.Title).ToDictionary(g => g.Key, g => g.Count());
            this.Game.Jobs.ForEach(j =>
            {
                if (!counts.ContainsKey(j.Title))
                {
                    counts[j.Title] = 0;
                }
            });
            if (counts["Fighter"] < counts["Hungry"])
            {
                Recruit(jobs["Fighter"].First());
            }
            else
            {
                Recruit(jobs["Hungry"].First());
            }
            Console.WriteLine("Recruit End:   Beaver Count {0}", this.Player.Beavers.Count);
        }

        public bool Recruit(Job job)
        {
            var lodge = this.Player.Lodges.FirstOrDefault(l => l.CanRecruit(job));
            if (lodge != null)
            {
                job.Recruit(lodge);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

    }
}
