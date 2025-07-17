namespace TaskbarTray.stuff
{
    public class TemperatureUnitChangedMessage
    {
        public TemperatureUnit Unit { get; }
        public TemperatureUnitChangedMessage(TemperatureUnit unit)
        {
            Unit = unit;
        }
    }
}
