namespace GopalBattleship.Entities.Ships
{
    public class Destroyer : Ship
    {
        public Destroyer()
        {
            Name = "Destroyer";
            Width = 2;
            OccupationType = EnumLabel.Destroyer;
        }
    }
}
