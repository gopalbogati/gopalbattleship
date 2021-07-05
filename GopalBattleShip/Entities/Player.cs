using GopalBattleship.Entities.Boards;
using GopalBattleship.Entities.Ships;
using GopalBattleship.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GopalBattleship.Entities
{
    public class Player
    {
        public string Name { get; set; }
        public PlayBoard GameBoard { get; set; }
        public ShootingBoard FiringBoard { get; set; }
        public List<Ship> Ships { get; set; }
        public bool HasLost
        {
            get
            {
                return Ships.All(x => x.IsSunk);
            }
        }

        public Player(string name)
        {
            Name = name;
            Ships = new List<Ship>()
            {
                new Battleship()
            };
            GameBoard = new PlayBoard();
            FiringBoard = new ShootingBoard();
        }

        /// <summary>
        /// Each player has a 10x10 board
        /// </summary>
        public void OutputBoards()
        {
            Console.WriteLine(Name);
            Console.WriteLine("Own Board:                          Firing Board:");
            for(int row = 1; row <= 10; row++)
            {
                for(int ownColumn = 1; ownColumn <= 10; ownColumn++)
                {
                    Console.Write(GameBoard.Panels.At(row, ownColumn).Status + " ");
                }
                Console.Write("                ");
                for (int firingColumn = 1; firingColumn <= 10; firingColumn++)
                {
                    Console.Write(FiringBoard.Panels.At(row, firingColumn).Status + " ");
                }
                Console.WriteLine(Environment.NewLine);
            }
            Console.WriteLine(Environment.NewLine);
        }

        /// <summary>
        /// During setup, players can place an arbitrary number of “battleships” on their board.
        /// The ships are 1-by-n sized, must fit entirely on the board, must be aligned either
        /// vertically or horizontally, and cannot overlap.
        /// </summary>
        public void PlaceShips()
        {
           
            Random random = new Random(Guid.NewGuid().GetHashCode());
            foreach (var ship in Ships)
            {
                bool isOpen = true;
                while (isOpen)
                {
                    var startcolumn = random.Next(1,11);
                    var startrow = random.Next(1, 11);
                    int endrow = startrow, endcolumn = startcolumn;
                    var orientation = random.Next(1, 101) % 2; //0 for Horizontal

                    List<int> panelNumbers = new List<int>();
                    if (orientation == 0)
                    {
                        for (int i = 1; i < ship.Width; i++)
                        {
                            endrow++;
                        }
                    }
                    else
                    {
                        for (int i = 1; i < ship.Width; i++)
                        {
                            endcolumn++;
                        }
                    }

                    //ships should be placed within the boundry.
                    if(endrow > 10 || endcolumn > 10)
                    {
                        isOpen = true;
                        continue;
                    }

                    //Check if specified panels are occupied
                    var affectedPanels = GameBoard.Panels.Range(startrow, startcolumn, endrow, endcolumn);
                    if(affectedPanels.Any(x=>x.IsOccupied))
                    {
                        isOpen = true;
                        continue;
                    }

                    foreach(var panel in affectedPanels)
                    {
                        panel.OccupationType = ship.OccupationType;
                    }
                    isOpen = false;
                }
            }
        }

        public Coordinates FireShot()
        {
            var hitNeighbors = FiringBoard.GetHitNeighbors();
            Coordinates coords;
            if (hitNeighbors.Any())
            {
                coords = SearchingShot();
            }
            else
            {
                coords = RandomShot();
            }
            Console.WriteLine(Name + " says: \"Firing shot at " + coords.Row.ToString() + ", " + coords.Column.ToString() + "\"");
            Console.WriteLine($"Press any key [except power button] to fire!!!");
            Console.ReadKey();
            return coords;
        }

        /// <summary>
        /// During play, players take a turn “attacking” a single position on the opponent’s board,
        ///and the opponent must respond by either reporting a “hit” on one of their battleships
        //(if one occupies that position) or a “miss”
        /// </summary>
        /// <param name="coords"></param>
        /// <returns></returns>
        public ShotResult ProcessShot(Coordinates coords)
        {
            var panel = GameBoard.Panels.At(coords.Row, coords.Column);
            if (!panel.IsOccupied)
            {
                Console.WriteLine(Name + " says: \"Firing missed!\"");
                return ShotResult.Miss;
            }
            //A battleship is sunk if it has been hit on all the squares it occupies
            //A player wins if all of their opponent’s battleships have been sunk.
            var ship = Ships.First(x => x.OccupationType == panel.OccupationType);
            ship.Hits++;
            Console.WriteLine(Name + " says: \"Firing hit the target!\"");
            if (ship.IsSunk)
            {
                Console.WriteLine(Name + " says: \"You sunk my " + ship.Name + "!\"");
            }
            return ShotResult.Hit;
        }


        private Coordinates RandomShot()
        {
            var availablePanels = FiringBoard.GetOpenRandomPanels();
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            var panelID = rand.Next(availablePanels.Count);
            return availablePanels[panelID];
        }

        private Coordinates SearchingShot()
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            var hitNeighbors = FiringBoard.GetHitNeighbors();
            var neighborID = rand.Next(hitNeighbors.Count);
            return hitNeighbors[neighborID];
        }

        public void ProcessShotResult(Coordinates coords, ShotResult result)
        {
            var panel = FiringBoard.Panels.At(coords.Row, coords.Column);
            switch(result)
            {
                case ShotResult.Hit:
                    panel.OccupationType = EnumLabel.Hit;
                    break;

                default:
                    panel.OccupationType = EnumLabel.Miss;
                    break;
            }
        }
    }
}
