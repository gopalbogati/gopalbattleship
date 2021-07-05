using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GopalBattleship
{
    public enum EnumLabel
    {
        [Description("O")]
        Empty,

        [Description("B")]
        Battleship,

        [Description("H")]
        Hit,

        [Description("M")]
        Miss
    }

    public enum ShotResult
    {
        Miss,
        Hit
    }
}
