namespace InspectionCore.Motion
{
    public readonly struct MotorState
    {
        public MotorState(double panXPercent, double panYPercent, double zoomFactor)
        {
            PanXPercent = panXPercent; // -100..100 (% of frame width)
            PanYPercent = panYPercent; // -100..100 (% of frame height)
            ZoomFactor = zoomFactor;  // 1.0 .. 3.0+
        }

        public double PanXPercent { get; }
        public double PanYPercent { get; }
        public double ZoomFactor { get; }
    }
}
