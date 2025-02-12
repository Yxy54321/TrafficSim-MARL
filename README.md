# TrafficSim-MARL


A comprehensive simulation and training system that combines traffic signal control and emergency vehicle routing using multi-agent reinforcement learning (MARL). The system integrates Unity-based simulation with Python-based MARL algorithms to create an intelligent urban traffic management solution.

## Project Overview

This project implements a novel approach to urban traffic management by treating both traffic signals and emergency vehicles as intelligent agents that can learn and adapt their behavior through reinforcement learning. The system consists of two main components:

1. **Unity Simulation Environment**: A 3D simulation environment that provides realistic traffic scenarios and visualizations
2. **MARL Training Framework**: A Python-based implementation of various multi-agent reinforcement learning algorithms

### Key Features

- Joint training of traffic signals and emergency vehicles
- Multiple MARL algorithm implementations (QMIX, IQL,  etc.)
- Real-time visualization of agent performance
- Customizable simulation parameters
- Performance monitoring and analytics
- Advanced reward visualization system

## System Requirements

### Unity Environment
- Unity 2020.3 or later
- .NET Framework 4.7.1 or later
- Windows 10/11 (recommended)

### Python Environment
- Python 3.7+
- PyTorch 1.7+
- NumPy
- Matplotlib
- Requests

## Project Structure

```
├── Simulation/                  # Unity simulation core
│   ├── FireDepartment.cs       # Emergency vehicle management
│   ├── HttpServer.cs           # Communication interface
│   ├── NpcControl.cs           # Vehicle behavior control
│   └── ...
├── UI/                         # User interface components
│   ├── ControlPanelManager.cs  # Simulation control panel
│   ├── RLRewardVisualizer.cs   # Reward visualization
│   └── ...
├── MARL_algorithms/            # Learning algorithms
│   ├── qmix_net.py            # QMIX 
│   ├── vdn_net.py             # VDN 
│   └── ...
```

## Installation

1. Clone the repository:
```bash
git clone [repository-url]
```

2. Set up the Unity environment:
   - Open the project in Unity Hub
   - Install required dependencies through the Package Manager
   - Build the simulation scene

3. Set up the Python environment:
```bash
pip install -r requirements.txt
```

## Usage

1. Start the Unity simulation:
   - Open the project in Unity
   - Load the simulation scene
   - Click Play to start the simulation server

2. Run the MARL training:
```bash
python main.py --alg qmix --map smart_city
```

### Configuration Parameters

- `--alg`: Choose the MARL algorithm (qmix, iql, vdn, etc.)
- `--n_episodes`: Number of episodes for training
- `--evaluate_cycle`: Evaluation frequency
- `--signal`: Enable traffic signal control
- `--cuda`: Enable CUDA acceleration

## Implementation Details

### Communication Architecture

The system uses HTTP-based communication between Unity and Python:
- Unity implements an HTTP server (HttpServer.cs)
- Python agents communicate through HTTP requests
- Real-time state and action information exchange

### Reward System

The reward function considers multiple factors:
- Traffic flow efficiency
- Emergency vehicle response time
- Traffic signal timing optimization
- Collision avoidance
- Overall system performance

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contact

For questions , support or unity asset ,  please contact 2153572@tongji.edu.cn.