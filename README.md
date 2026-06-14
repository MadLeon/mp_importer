# MP Importer

A WPF desktop application for extracting Manufacturing Process entries from `.xlsm` files and importing them into the job management database.

## Overview

MP Importer reads structured Excel workbooks (Manufacturing Process sheets), extracts part information and production process steps, and uploads the data to a SQLite database. Extracted results are saved locally as JSON files for review and editing before import.

## Features

- Select and manage multiple `.xlsm` files in a list
- Extract part metadata and process steps from each file
- Review and edit extracted results before uploading
- Upload selected results to the database with smart conflict handling:
  - Auto-creates new `part` records when not found
  - Updates blank `revision` or `description` fields when better data is available
  - Logs warnings for field mismatches without overwriting
  - Skips `process_template` insertion if records already exist

## Tech Stack

| Component | Library |
|---|---|
| UI Framework | WPF (.NET 10, win-x64) |
| MVVM | CommunityToolkit.Mvvm |
| Excel Parsing | ClosedXML |
| Database | Microsoft.Data.Sqlite |
| JSON | Newtonsoft.Json |
| Logging | Serilog (file sink) |

## Project Structure

```
mp_importer/
├── src/MpImporter/
│   ├── Models/             # ExtractionResult, ProcessStep
│   ├── Services/           # ExcelExtractorService, JsonResultService, DatabaseService
│   ├── ViewModels/         # MainViewModel, FileEntryViewModel, ResultViewerViewModel
│   ├── Views/              # ResultViewerWindow, OverwriteConfirmWindow
│   └── App.xaml
├── data/
│   └── record.db           # Development database
├── reference/
│   ├── project_description.md
│   └── file_structure.md
└── results/                # Generated JSON export files (auto-created)
```

## Excel File Format

Each `.xlsm` file contains two sections:

**Info fields (header area)**

| Cell | Field |
|---|---|
| B7 | PO Number |
| N6 | OE Number |
| Q6 | Job Number |
| F7 | Line Number |
| J7 | Drawing Number |
| R7 | Revision |
| B8 | Drawing Release Date |
| H8 | Description |
| D9 | Delivery Required Date |
| Q9 | Quantity |

**Process steps (rows 11–26)**

| Column | Field |
|---|---|
| D | Shop Code |
| E | Row Number |
| F–N | Process Description (merged) |

Shop codes: `FI`, `P`, `RT`, `SC`, `I`, `H`, `W`, `PI`

## Getting Started

### Prerequisites

- .NET 10 SDK
- Windows x64

### Build

```powershell
dotnet build src/MpImporter/MpImporter.csproj
```

Output: `src/MpImporter/bin/Debug/net10.0-windows/`

### Run

Launch `MpImporter.exe` from the build output directory.

The application automatically locates `data/record.db` by walking up the directory tree from the executable location.

## Database

| Environment | Path |
|---|---|
| Development | `data/record.db` |
| Production | `\\rtdnas2\OE\record.db` |

Key tables used: `part`, `process_template`

## Logs

Log files are written to the `logs/` folder next to the executable, rotated daily:

```
logs/app-20260613.log
```
