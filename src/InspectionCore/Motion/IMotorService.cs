using System;

namespace InspectionCore.Motion
{
    public interface IMotorService
    {
        MotorState State { get; }
        event EventHandler<MotorState>? StateChanged;

        void SetPanPercent(double xPercent, double yPercent); // -100..100
        void SetZoom(double zoomFactor);                      // >= 1.0
        void Reset();                                         // 0,0,1.0
    }
}
