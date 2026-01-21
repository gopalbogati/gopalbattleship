using System;

namespace GopalBattleship.Utilities
{
    public static class EnumLabelExtensions
    {
        public static bool IsShip(this EnumLabel label)
        {
            switch (label)
            {
                case EnumLabel.Carrier:
                case EnumLabel.Battleship:
                case EnumLabel.Cruiser:
                case EnumLabel.Submarine:
                case EnumLabel.Destroyer:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsShotResult(this EnumLabel label)
        {
            return label == EnumLabel.Hit || label == EnumLabel.Miss;
        }
    }
}
