# EventScape

Interactive 3D auditorium decorations & event layout planner (Unity). This repository also includes the CG IP course **baseline Python pipeline** (Phases 2–3) required for project setup.

## Project structure

```
EventScape/
├── src/
│   ├── gui/
│   ├── processing/
│   └── main.py          # CG IP baseline OpenCV pipeline
├── docs/
│   └── screenshots/     # Baseline run output
├── requirements.txt     # Python deps (opencv, numpy, matplotlib, PyQt5)
├── UNITY_ENVIRONMENT.txt  # Unity / Blender stack notes for the 3D app
├── PROJECT_CONTEXT.md
└── README.md
```

## Phase 2–3: Baseline pipeline setup

### 1. Create & activate virtual environment

```bash
python3 -m venv venv
source venv/bin/activate          # macOS / Linux
# .\venv\Scripts\activate         # Windows
```

### 2. Install dependencies

```bash
pip install -r requirements.txt
```

### 3. Run the baseline test

```bash
python src/main.py
```

**Success check:** A black window opens showing green edge contours of the text `CG IP Pipeline is Okay!`. The same frame is saved to `docs/screenshots/baseline_output.png`.

## Unity app (main project)

See `PROJECT_CONTEXT.md` and `UNITY_ENVIRONMENT.txt` for the Virtual Auditorium tech stack (Unity 2022.3 LTS, URP, Cinemachine, etc.).
