# Online-seabed-reconstruction-UNITY

> **This repository is based on [COLREG_navigation](https://github.com/the-cyclops/COLREG_navigation).**
> It reuses that project's Unity environment (HDRP water, boat physics, ML-Agents boat controller).

## What it is

This is a Unity HDRP simulator of a boat that surveys a real seabed: a 256 × 256 m patch of the
Malamocco inlet in the Venice Lagoon, built from 1 m multibeam bathymetry. A downward-looking
Velodyne VLP-16 LiDAR on the hull stands in for a multibeam sonar. You drive the boat with the
keyboard. While it moves, every scan is appended to a CSV file in real time.

## Why it exists

Testing **online** seabed reconstruction needs a stream of sonar-like pings that arrive while the
survey is running, over a seabed with an exact ground truth. Real survey data has no exact ground
truth, and offline datasets don't arrive as a stream. This simulator provides both:
- a live point-cloud stream, written to a CSV file while the boat moves;
- a ground-truth terrain in the same coordinate frame, so the reconstructed mesh can be scored against it.

## How it fits with Online-seabed-reconstruction

This repository is the **data source** for
[Online-seabed-reconstruction](https://github.com/trueilorp/Online-seabed-reconstruction), the Python pipeline that
reconstructs the seabed (Poisson / PPSurf, on the full scan or with a sliding window).

In the online experiment, the pipeline reads the CSV as it grows. Every 5 pings it updates a voxel
map and rebuilds the mesh locally with Poisson. It then compares the mesh with the ground truth
exported from this project.

The two projects communicate only through the CSV file and the ground-truth mesh.


## Requirements

- **Unity 6000.5.9f1** with HDRP (17.5.0), and a GPU that supports HDRP.
- Packages, which Unity installs from `Packages/manifest.json`:
  - [UnitySensors](https://github.com/Field-Robotics-Japan/UnitySensors) 3.0.0, from Git;
  - ML-Agents 4.1.0;
  - Unity Recorder 5.1.8;
  - Input System 1.20.0;
  - Splines 2.9.0.
- Git LFS.

## Usage

1. Clone the repository and run `git lfs pull`.
2. Open `UnityEnvironment/COLREG_simulation/` in Unity 6000.5.9f1, then open `Assets/Scenes/OgScene.unity`.
3. Press **Play**. Recording starts automatically.
4. Drive the boat with the **arrow keys** or **WASD**: Up/Down for throttle, Left/Right for steering.
5. Stop Play mode to close the file.

### Output CSV

Scans are saved to `UnityEnvironment/COLREG_simulation/Recordings/scan_<yyyyMMdd_HHmmss>.csv`.
The recorder appends one scan per second and flushes it straight away, so another process can read
the file while it grows.

```
x,y,z,intensity,ping_number,elapsed_s
```

| Column | Meaning |
|---|---|
| `x`, `y`, `z` | Point position in Unity world coordinates [m]. Y is up (depth is negative, the water surface is at y = 0) and Z is north. |
| `intensity` | Synthetic LiDAR intensity. Missed rays are already removed. |
| `ping_number` | Scan index, starting at 0. |
| `elapsed_s` | Simulation time of the scan [s]. |

To convert world coordinates to the original bathymetry grid, use:
`E = 291571.249 + (x + 128)`, `N = 5022982.649 + (z + 128)`.

### Ground truth

The ground truth is in the same frame as the CSV. You can use either of these:
- **Tools > Export Terrain to OBJ** in the Unity editor, which exports the terrain as an OBJ mesh;
- `Assets/Terrain/patch_malamocco/gt_mesh.ply`.

`Terrain_ground_truth.obj` at the repository root is an export that is already done.

## Main components

| File | Role |
|---|---|
| `Assets/Scripts/PointCloudRecorder.cs` | Streams the LiDAR scans to the CSV file. |
| `Assets/Scripts/ParticlePointCloudVisualizer.cs` | Draws the live point cloud, colored by range. |
| `Assets/Editor/ExportTerrainToObj.cs` | Exports the ground-truth OBJ. |
| `Assets/Scripts/BoatAgent.cs` | Boat control. `demoMode` disables the COLREG arena and lets you drive manually. |
| `Assets/Scripts/FollowCamera.cs` | Chase camera that follows the boat. |
| `Assets/Terrain/patch_malamocco/` | Bathymetry patch: heightmap, ground-truth mesh and metadata. |


## Credits

- **COLREG_navigation**: <https://github.com/the-cyclops/COLREG_navigation>. This repository is based on it.
- **UnitySensors**: Field-Robotics-Japan, <https://github.com/Field-Robotics-Japan/UnitySensors>.


## Contacts
Simone Dario (simone.dario@univr.it), Università degli studi di Verona