# TimetablePlanner

TimetablePlanner is a C#/.NET desktop application for creating, generating, and optimizing school timetables.

The application is designed around a modular architecture that separates domain logic, data access, and the user interface. The primary goal is to provide a simple and intuitive timetable planning experience while handling the complexity of scheduling and optimization in the background.

> **Project status:** In active development

## Overview

Timetable scheduling is a constraint-based optimization problem involving teachers, classes, groups, rooms, subjects, and time slots.

TimetablePlanner aims to automatically generate valid timetables while considering both mandatory constraints and optional preferences. The project also focuses on efficient algorithms, solution quality, and the ability to compare different scheduling approaches.


## Architecture

The solution is divided into three main layers.

```text
TimetablePlanner
│
├── TimetablePlanner.Core
│   ├── Domain models
│   ├── Constraints
│   ├── Scheduling logic
│   ├── Generators
│   └── Optimization
│
├── TimetablePlanner.Data
│   ├── Entity Framework Core
│   ├── SQLite
│   ├── Repositories
│   └── Persistence
│
└── TimetablePlanner.UI
    ├── WPF
    ├── MVVM
    ├── Views
    └── ViewModels
```





## Optimization

Optimization is one of the main areas of the project.

The system is designed to support multiple approaches to timetable generation and optimization, allowing their performance and solution quality to be compared.

Current and planned approaches include:

- Greedy algorithms
- Backtracking and search-based approaches
- Constraint-based optimization
- Parallel solution generation
- Multi-objective optimization
- Heuristic optimization
- AI-assisted optimization

The objective is not simply to generate a valid timetable, but to efficiently find high-quality solutions among a large number of possible schedules.

## Technology Stack

| Technology | Purpose |
|---|---|
| C# | Main programming language |
| .NET 9 | Application framework |
| WPF | Windows desktop UI |
| MVVM | UI architecture |
| CommunityToolkit.Mvvm | MVVM implementation |
| Entity Framework Core | Data access |
| SQLite | Local database |
| Visual Studio / Cursor | Development environment |



## Getting Started

### Requirements

- Windows
- .NET 9 SDK
- Visual Studio or another compatible C# IDE
- Git




## Data Storage

TimetablePlanner uses SQLite for local data persistence.

Using SQLite allows the application to remain:

- Offline
- Lightweight
- Easy to distribute
- Independent from an external database server

The data layer is separated from the scheduling engine so that the persistence mechanism can be extended or replaced without significantly affecting the core scheduling logic.

## Development Goals

The long-term goal is to combine three main areas:

```text
User-friendly desktop application
              +
Efficient scheduling algorithms
              +
Constraint-based optimization
```

The complexity of the scheduling algorithms should remain largely invisible to the end user.

The user should be able to configure the school, generate a timetable, review conflicts, evaluate the result, and make manual changes without having to understand the underlying optimization process.

## Academic Direction

TimetablePlanner is also being developed as a potential engineering-oriented thesis project.

The project provides an opportunity to investigate:

- Constraint satisfaction problems
- Combinatorial optimization
- Heuristic algorithms
- Timetable scheduling
- Parallel processing
- Multi-objective optimization
- Algorithm performance comparison
- Software architecture
- Desktop application development

A major focus is the evaluation of different scheduling approaches based on measurable criteria such as:

- Execution time
- Solution quality
- Number of constraint violations
- Scalability
- Number of generated candidate solutions

This makes it possible to evaluate not only whether an algorithm produces a valid result, but also how efficiently it solves the scheduling problem.

## Development Roadmap

### Completed / In Progress

- [x] Core domain model
- [x] Lesson requirement modelling
- [x] Data layer
- [x] SQLite integration
- [x] Repository layer
- [x] Basic lesson generation
- [x] Hard constraint architecture
- [x] Soft constraint architecture
- [x] Initial timetable generation
- [ ] Advanced optimization
- [ ] Parallel solution generation
- [ ] Advanced WPF interface
- [ ] Timetable editing
- [ ] Performance benchmarking
- [ ] Additional optimization and AI approaches

The roadmap is subject to change as the project evolves.

## Contributing

This project is primarily developed as a personal project and potential thesis project.

Bug reports, suggestions, and technical feedback are welcome. Issues and pull requests can be submitted through the GitHub repository.

## License

No open-source license has currently been specified for this repository.

Unless a license is added, the source code should not be assumed to be freely reusable or redistributable.

## Links

- [Repository](https://github.com/xixed/TimetablePlanner)
- [Issues](https://github.com/xixed/TimetablePlanner/issues)

## Author

**Tarnai András**

[GitHub](https://github.com/xixed)
