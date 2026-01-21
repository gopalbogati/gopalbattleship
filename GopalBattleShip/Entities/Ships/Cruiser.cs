namespace GopalBattleship.Entities.Ships
{
    public class Cruiser : Ship
    {
        public Cruiser()
        {
            Name = "Cruiser";
            Width = 3;
            OccupationType = EnumLabel.Cruiser;
        }
    }
}
