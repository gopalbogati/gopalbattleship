namespace GopalBattleship.Entities.Ships
{
    public class Carrier : Ship
    {
        public Carrier()
        {
            Name = "Carrier";
            Width = 5;
            OccupationType = EnumLabel.Carrier;
        }
    }
}
