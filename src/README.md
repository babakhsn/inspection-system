
# Miniature Industrial Inspection System

This project simulates a small-scale industrial inspection system using **C# WPF (MVVM)**, **OpenCV**, and **gRPC**.

## Goals
- Live camera feed with image processing
- Simulated motor control
- gRPC service for modular control
- CI/CD integration
- Clear documentation

## Structure
- `src/InspectionApp` → WPF UI
- `src/InspectionCore` → Core logic (camera, motor, processing)
- `tests/InspectionTests` → Unit tests
- `docs/` → Architecture diagrams, notes


## Progress Log
- **Day 1:** Repo + WPF MVVM skeleton.
- **Day 2:** Basic UI shell (header, controls, status), dummy Start/Stop commands wired via MVVM.
- **Day 3:** Integrated webcam via Emgu CV; live preview in WPF using BGRA32 frames bound to `Image`.
- **Day 4:** Added Serilog logging (console + rolling file). Implemented frame capture with PNG/JPEG encoding and default save folder (`Pictures/InspectionSystem/Captures`).

