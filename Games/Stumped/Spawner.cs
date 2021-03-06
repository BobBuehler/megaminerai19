// A resource spawner that generates branches or food.

// DO NOT MODIFY THIS FILE
// Never try to directly create an instance of this class, or modify its member variables.
// Instead, you should only be reading its variables and calling its functions.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Joueur.cs.Games.Stumped
{
    /// <summary>
    /// A resource spawner that generates branches or food.
    /// </summary>
    class Spawner : Stumped.GameObject
    {
        #region Properties
        /// <summary>
        /// True if this Spawner has been harvested this turn, and it will not heal at the end of the turn, false otherwise.
        /// </summary>
        public bool HasBeenHarvested { get; protected set; }

        /// <summary>
        /// How much health this Spawner has, which is used to calculate how much of its resource can be harvested.
        /// </summary>
        public int Health { get; protected set; }

        /// <summary>
        /// The Tile this Spawner is on.
        /// </summary>
        public Stumped.Tile Tile { get; protected set; }

        /// <summary>
        /// What type of resource this is ('food' or 'branches').
        /// </summary>
        public string Type { get; protected set; }



        #endregion


        #region Methods
        /// <summary>
        /// Creates a new instance of Spawner. Used during game initialization, do not call directly.
        /// </summary>
        protected Spawner() : base()
        {
        }




        #endregion
    }
}
