using System;

namespace InspectionCore.Motion
{
    public sealed class MotorSimulator : IMotorService
    {
        private MotorState _state = new MotorState(0, 0, 1.0);
        public MotorState State => _state;

        public event EventHandler<MotorState>? StateChanged;

        public void SetPanPercent(double xPercent, double yPercent)
        {
            xPercent = Math.Clamp(xPercent, -100, 100);
            yPercent = Math.Clamp(yPercent, -100, 100);
            var changed = xPercent != _state.PanXPercent || yPercent != _state.PanYPercent;
            if (!changed) return;

            _state = new MotorState(xPercent, yPercent, _state.ZoomFactor);
            StateChanged?.Invoke(this, _state);
        }

        public void SetZoom(double zoomFactor)
        {
            zoomFactor = Math.Max(1.0, zoomFactor);
            if (zoomFactor == _state.ZoomFactor) return;

            _state = new MotorState(_state.PanXPercent, _state.PanYPercent, zoomFactor);
            StateChanged?.Invoke(this, _state);
        }

        public void Reset()
        {
            _state = new MotorState(0, 0, 1.0);
            StateChanged?.Invoke(this, _state);
        }
    }
}
