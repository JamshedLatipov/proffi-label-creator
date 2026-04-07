# New Project Page — Implementation Plan

## Overview
Dedicated wizard page that collects project settings and opens the editor.
Triggered via "New Project" button in the sidebar.

---

## Sections on the page

| # | Section | Fields |
|---|---------|--------|
| 1 | **Project Details** | Name (required), Description, Category |
| 2 | **Label Format** | Size presets, W×H mm, Orientation, Material, Color mode |
| 3 | **Print Target** | Default printer (from Registry), DPI |
| 4 | **Data Source** | None / ERP / CSV (conditional fields) |
| 5 | **Actions** | Cancel → Templates, Create Project → Editor |

---

## Steps

- [ ] **Step 1** — `Models/ProjectSettings.cs` — record with all project params
- [ ] **Step 2** — `ViewModels/NewProjectViewModel.cs` — all properties + commands
- [ ] **Step 3** — `Views/NewProjectView.axaml` + `NewProjectView.axaml.cs` — full UI
- [ ] **Step 4** — `ViewModels/EditorViewModel.cs` — add constructor accepting `ProjectSettings`
- [ ] **Step 5** — `ViewModels/MainWindowViewModel.cs` — add `NavigateToNewProjectCommand`
- [ ] **Step 6** — `Views/MainWindow.axaml` — rebind "New Project" button
- [ ] **Step 7** — `dotnet build` — verify no errors

---

## ViewModel properties (`NewProjectViewModel`)

```
ProjectName         string      required, CreateProject disabled when empty
Description         string      optional
SelectedCategory    string      Logistics | Inventory | Safety | Retail
SelectedPreset      string      "58×40" | "100×150" | "110×35" | "40×80" | "Custom"
LabelWidth          double      mm, autofilled from preset; editable when Custom
LabelHeight         double      mm, autofilled from preset; editable when Custom
IsCustomSize        bool        computed from SelectedPreset
IsPortrait          bool        toggle; swaps W/H on change
SelectedMaterial    string      Thermal Transfer | Direct Thermal | Vinyl | Paper
IsMonochrome        bool        Color mode toggle
Printers            ObservableCollection<string>   from Registry
SelectedPrinter     string
SelectedDpi         string      "203 dpi" | "300 dpi" | "600 dpi"
DataSource          string      "None" | "ERP" | "CSV"
IsErpVisible        bool        computed
IsCsvVisible        bool        computed
ErpConnectionString string
CsvFilePath         string
BrowseCsvCommand    RelayCommand
CreateProjectCommand RelayCommand   CanExecute: ProjectName not empty
CancelCommand       RelayCommand
```

---

## Preset map

| Preset | Width | Height |
|--------|-------|--------|
| 58×40  | 58    | 40     |
| 100×150| 100   | 150    |
| 110×35 | 110   | 35     |
| 40×80  | 40    | 80     |
| Custom | —     | —      |

---

## Navigation pattern
- `NewProjectViewModel` receives `Action<ViewModelBase> navigate` from `MainWindowViewModel`
- `CreateProjectCommand` → `navigate(new EditorViewModel(settings))`
- `CancelCommand` → `navigate(new TemplatesViewModel())`

---

## UI layout (NewProjectView)

```
┌─ Header ─────────────────────────────────────────────┐
│  Projects › New Project                               │
│  New Project  [subtitle]                              │
└───────────────────────────────────────────────────────┘
┌─ Card (680px centered) ──────────────────────────────┐
│                                                       │
│  ── 1. PROJECT DETAILS ─────────────────────────────  │
│  Name *        [TextBox                          ]    │
│  Description   [TextBox                          ]    │
│  Category      [ComboBox ▾]                           │
│                                                       │
│  ── 2. LABEL FORMAT ────────────────────────────────  │
│  [58×40] [100×150] [110×35] [40×80] [Custom]         │
│  Width [____] mm   Height [____] mm                   │
│  [ Portrait card ]  [ Landscape card ]                │
│  Material [ComboBox ▾]   ○ Mono  ● Color              │
│                                                       │
│  ── 3. PRINT TARGET ────────────────────────────────  │
│  Printer [ComboBox ▾]   DPI [ComboBox ▾]              │
│                                                       │
│  ── 4. DATA SOURCE ─────────────────────────────────  │
│  ● None   ○ ERP Connection   ○ CSV File               │
│  [ERP string field — visible when ERP selected]       │
│  [CSV path field + Browse — visible when CSV]         │
│                                                       │
│  ── 5. ACTIONS ─────────────────────────────────────  │
│               [Cancel]   [Create Project →]           │
└───────────────────────────────────────────────────────┘
```
