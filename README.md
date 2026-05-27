# Shandong GIS Algorithm Platform

A lightweight C# WinForms GIS algorithm platform for visualizing Shandong Province boundary data and testing basic spatial analysis algorithms.

## Overview

This project loads geographic coordinate data from CSV, renders the boundary on a WinForms canvas, and provides several core GIS calculations implemented in C#.

The repository is suitable for learning how fundamental GIS operations work without relying on a full GIS engine.

## Features

- Load longitude/latitude coordinate data from CSV
- Render geographic boundary data in a WinForms interface
- Convert geographic coordinates to screen coordinates
- Calculate polygon area
- Calculate boundary perimeter
- Detect whether a clicked point is inside a polygon
- Interact with the map through a simple desktop UI

## Algorithms Implemented

- Coordinate transformation from geographic coordinates to screen coordinates
- Shoelace formula for polygon area calculation
- Distance-based perimeter calculation
- Point-in-polygon detection
- CSV-based geographic data loading

## Tech Stack

- C#
- .NET WinForms
- Visual Studio
- CSV boundary data exported from ArcGIS Pro preprocessing

## Project Structure

```text
.
├── Code/
│   ├── WindowsFormsApp1.sln   # Visual Studio solution file
│   └── Form1.cs               # Main WinForms UI and GIS algorithm logic
├── Shandong_points.csv        # Sample Shandong Province boundary coordinates
├── Figure                     # Placeholder/figure asset file
├── LICENSE                    # Project license
└── README.md                  # Project documentation
```

## How to Run

1. Clone or download this repository.
2. Open `Code/WindowsFormsApp1.sln` in Visual Studio.
3. Build and run the WinForms project.
4. In the application, load `Shandong_points.csv` when prompted.
5. Use the toolbar/status actions to render the boundary and run area, perimeter, or point-in-polygon operations.

## Dataset

`Shandong_points.csv` contains processed longitude and latitude coordinates for the Shandong Province boundary. The source boundary was prepared from shapefile data and exported through ArcGIS Pro.

Expected CSV format:

```text
latitude,longitude
```

Each row represents one boundary point.

## Notes for Readers

The main implementation is currently concentrated in `Code/Form1.cs`. For a larger version of this project, a clean next step would be to split the logic into separate classes/files, for example:

- `GeoPoint.cs` for coordinate data models
- `GeoDataProcessor.cs` for CSV loading and coordinate conversion
- `GeometryHelper.cs` for area and perimeter algorithms
- `MapPainter.cs` for rendering logic
- `ClickDetector.cs` for interaction logic

## System Architecture

<img width="554" height="355" alt="System architecture" src="https://github.com/user-attachments/assets/195649af-a26b-4d66-b6bd-9b5aea5dcd5d" />
