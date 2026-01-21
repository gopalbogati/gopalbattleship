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

        [Description("C")]
        Carrier,

        [Description("B")]
        Battleship,

        [Description("R")]
        Cruiser,

        [Description("S")]
        Submarine,

        [Description("D")]
        Destroyer,

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
