
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
- **Day 5:** Refactored ViewModel for DI (camera/clock/filesystem/framesaver). Introduced small abstractions for time & file I/O. Added unit tests with xUnit (VM behavior + PNG saving).
- **Day 6:** Added OpenCV filters (None, Grayscale, Edge Detection/Canny). Filter dropdown in UI with live processing and threshold controls for Canny. Processing pipeline stays BGRA32-in/BGRA32-out.
- **Day 7:** Added basic defect detection (threshold-based scratch highlight using Sobel + Otsu + dilation). UI now displays Raw and Processed frames side-by-side. “Defect Detection” added to filter options.

