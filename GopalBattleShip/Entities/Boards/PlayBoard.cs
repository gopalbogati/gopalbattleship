using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GopalBattleship.Entities.Boards
{
    /// <summary>
    /// Represents a collection of Panels to provide a Player with their Game Board (e.g. where their ships are placed).
    /// </summary>
    public class PlayBoard
    {
        public const int BoardSize = 10;
        public List<Panel> Panels { get; set; }

        public PlayBoard()
        {
            Panels = new List<Panel>();
            for (int i = 1; i <= BoardSize; i++)
            {
                for (int j = 1; j <= BoardSize; j++)
                {
                    Panels.Add(new Panel(i, j));
                }
            }
        }
    }
}
