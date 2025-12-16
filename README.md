# Unity Warehouse Robot Simulation

A Unity-based visualization and simulation environment for warehouse robotics, designed to work in conjunction with a ROS-based robot control system.

![Unity](https://img.shields.io/badge/Unity-6000.0.62f1+-black?logo=unity)
![ROS](https://img.shields.io/badge/ROS-2-22314E?logo=ros)
![HDRP](https://img.shields.io/badge/Render%20Pipeline-HDRP-blueviolet)

---

## Overview

This Unity project provides a 3D visualization environment for a multi-robot warehouse management system. It integrates with ROS via the **ROS-TCP-Connector** package to:

- Generate and publish **occupancy grid maps** to ROS
- Transmit **simulation configuration** (robots, shelves, delivery points, charging stations)
- Receive and visualize **robot poses** and **states** in real-time
- Provide visual feedback for robot activities (idle, retrieving, delivering, charging)

---

## Prerequisites

- **Unity 6000.0.62f1 LTS** or compatible
- **High Definition Render Pipeline (HDRP)** — required for correct scene visualization
- **ROS-TCP-Connector** package (already included)
- ROS counterpart system running and ready

> ⚠️ **Important**: This Unity simulation must be launched **after** the ROS counterpart is running. The system expects ROS topics to be available upon startup.

---

## Render Pipeline Requirements

This project uses **Unity HDRP (High Definition Render Pipeline)** for advanced lighting and visual effects.

To ensure correct visualization:

1. Open **Edit > Project Settings > Graphics**
2. Verify that the **Scriptable Render Pipeline Settings** is set to an HDRP asset
3. If materials appear pink/magenta, you may need to upgrade them via **Edit > Rendering > Materials > Convert All Built-in Materials to HDRP**

---

## Project Structure

```
Assets/
└── Scripts/
    └── Single_robot_test/
        ├── GenerateGrid.cs      # Occupancy grid generation & ROS publishing
        └── RobotController.cs   # Robot pose & state visualization
```

---

## Setup Instructions

1. **Start the ROS counterpart first**
   ```bash
   # Ensure your ROS system is running
   ```

2. **Configure ROS Connection in Unity**
   - Open **Robotics > ROS Settings** in the Unity Editor
   - Set ROS2
   - Set the ROS IP address (default: `127.0.0.1`)
   - Set the ROS port (default: `10000`)

3. **Launch Unity Scene**
   - Open the main simulation scene
   - Press **Play** to start the simulation
   - The occupancy grid and configuration will be automatically published to ROS

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Materials appear pink/magenta | Convert materials to HDRP (see Render Pipeline Requirements) |
| No robot movement | Ensure ROS is running and publishing pose messages |
| Occupancy grid not received by ROS | Verify ROS-TCP-Connector settings and network connectivity |

---

## License

See [LICENSE](LICENSE) for details.
