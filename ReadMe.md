# Batch File Creator for ScummVM Games

This application helps you create batch files for launching your ScummVM games. It simplifies the process of setting up individual batch files for each game, allowing you to quickly launch your favorite ScummVM titles.

## Features

-   **User-Friendly Interface:** A simple and intuitive GUI with a dark theme, top menu, and status bar for easy interaction.
-   **ScummVM Executable Selection:** Allows you to browse and select the path to your `scummvm.exe` file.
-   **Game Folder Selection:** Lets you choose the root folder containing your ScummVM game folders.
-   **Automated Batch File Creation:** Automatically generates batch files for each game folder within the selected root folder (scans top-down for folders containing files).
-   **Cancellation:** A cancel button allows you to stop the batch file creation process at any time.
-   **Logging:** Provides a log window to display progress and any errors during batch file creation.
-   **Status Bar:** Displays real-time status messages about the application's state.
-   **Error Handling:** Includes error handling and reporting to ensure smooth operation and provide helpful feedback.
-   **Silent Bug Reporting:** Automatically sends error reports to a backend API for tracking and fixing issues.
-   **Update Checking:** Checks GitHub for new releases on startup and notifies you when a newer version is available.

## How to Use

1.  **Select ScummVM Executable:** Click the "Browse" button next to the "ScummVM Path" field and select your `scummvm.exe` file.
2.  **Select Game Folder:** Click the "Browse" button next to the "Games Folder" field and select the root folder containing your ScummVM game folders.
3.  **Create Batch Files:** Click the "Create Batch Files" button to generate the batch files.
4.  **Locate Batch Files:** The generated batch files will be created in the root folder you selected. Each batch file will be named after the corresponding game folder (e.g., `game_folder_name.bat`).

## System Requirements

-   Windows Operating System
-   .NET 10 Desktop Runtime
-   ScummVM installed

## Installation

1.  Download the compiled application.
2.  Extract the files to a folder of your choice.
3.  Run the `CreateBatchFilesForScummVMGames.exe` executable.

## Code Structure

-   **App.xaml & App.xaml.cs:** Defines the application, handles global exception handling, and centrally manages service instances (`BugReportService`, `ApplicationStatsService`, `GitHubReleaseService`).
-   **MainWindow.xaml & MainWindow.xaml.cs:** Defines the main window's UI and logic, including the menu, status bar, input fields, cancellation, update notification, and batch file creation process.
-   **AboutWindow.xaml & AboutWindow.xaml.cs:** A separate window that displays application information, version, and credits.
-   **BugReportService.cs:** Sends bug reports to a remote API using a shared `HttpClient` with concurrency throttling.
-   **AssemblyInfo.cs:** Contains assembly-level metadata attributes.
-   **Services/BatchFileGenerator.cs:** Generates batch file content and handles filename sanitization/capitalization.
-   **Services/GitHubReleaseService.cs:** Checks the GitHub API for new releases and compares versions.

## Background Services

The application initializes several services at startup for silent background operation:

-   **Bug Reporting:** `BugReportService` sends error reports to a specified API endpoint to help track and fix issues. Uses a semaphore to prevent concurrent request flooding.
-   **Update Checking:** `GitHubReleaseService` queries the GitHub Releases API and notifies the user if a newer version is available.

## Batch File Format

Each generated batch file contains the following command:

```batch
"path\to\scummvm.exe" -p "path\to\game\folder" --auto-detect --fullscreen
