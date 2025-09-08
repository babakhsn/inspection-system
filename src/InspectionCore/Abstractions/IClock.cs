namespace InspectionCore.Abstractions
{
    public interface IClock
    {
        DateTime Now { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
