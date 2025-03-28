# PhaseShift
A stylish productivity timer app for developers, with Pomodoro, stopwatch, and custom timer modes. Designed to keep your focus in phase.

## 🍅 What is the Pomodoro Technique?

The **Pomodoro Technique** is a simple time management method:
- Work in focused intervals (typically **25 minutes**) called _Pomodoros_
- Take **short breaks** between intervals
- After a few Pomodoros, take a **longer break**

It’s a surprisingly effective way to avoid burnout, reduce procrastination, and maintain momentum — especially for deep, uninterrupted work.

> 🧐 **Why is it called _Pomodoro_?**  
> The method was developed by Francesco Cirillo in the late 1980s, who used a **tomato-shaped kitchen timer** while studying. _Pomodoro_ is the Italian word for **tomato** — and the name stuck.

## Features

- ✅ **Stopwatch Mode** – Start, stop, and reset your work sessions with ease.
- ✅ **Custom Timers** – Set countdowns for arbitrary tasks and activities.  

## Planned Features

- 🍅 **Pomodoro Timer** – Work/break intervals based on the classic Pomodoro technique.
- ⚙️ **User Settings & Persistence** – Configure durations, themes, and preferences.  
- 🔁 **Stopwatch Rounds** – Track multiple rounds/splits within a single stopwatch session.

## Dependencies
All projects in this solution use the .NET 8 SDK.

### PhaseShift.Core.Tests
- Microsoft.NET.Test.Sdk: 17.10.0  
- NSubstitute: 5.1.0  
- NUnit: 4.1.0  
- NUnit3TestAdapter: 4.5.0  

### PhaseShift.UI
- CommunityToolkit.Mvvm: 8.4.0  
- Material.Icons.WPF: 2.1.10  
- MaterialDesignThemes: 5.1.0  

### PhaseShift.UI.Tests
- Microsoft.NET.Test.Sdk: 17.10.0  
- NSubstitute: 5.1.0  
- NUnit: 4.1.0  
- NUnit3TestAdapter: 4.5.0  

## How to Build

### Using Visual Studio

1. Install **.NET 8 SDK** and **Visual Studio 2022 or later**.  
2. Clone the repository:  
   ```sh
   git clone https://github.com/thomaswening/PhaseShift.git
   ```
3. Open the solution (`PhaseShift.sln`) in Visual Studio and build the project.

### Using .NET CLI

1. Install **.NET 8 SDK**.  
2. Clone the repository:  
   ```sh
   git clone https://github.com/thomaswening/PhaseShift.git
   ```
3. Navigate to the solution directory and build:  
   ```sh
   dotnet build
   ```

## Solution Structure

Since PhaseShift is a small project, it is designed with a three-tier architecture in mind: Presentation, Application, and in the future also Persistence.

### PhaseShift.Core

- Timer logic and shared domain models  
- Stopwatch service and abstraction of timer behaviors  
- Designed for testability and decoupling from UI

### PhaseShift.Core.Tests

- Unit tests for core logic

### PhaseShift.UI

- WPF-based user interface using **MVVM** architecture  
- Styled with **MaterialDesignThemes** and **Material.Icons.WPF**

### PhaseShift.UI.Tests

- Unit tests for view models and UI logic

### PhaseShift.AccuracyTestTool

- Prototype tool (optional) to test timer accuracy and behavior  
- Intended for development and testing purposes

## License

This project is licensed under the **GNU General Public License v3.0**. See the [LICENSE](LICENSE) file for details.
