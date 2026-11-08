"""Adaptive Storage Framework Wealth Adjuster adapter for the sibling RimWorldDevTests launcher."""
from pathlib import Path
import os
import runpy
import sys

ROOT = Path(__file__).resolve().parents[1]
HARNESS = Path(os.environ.get("RIMWORLD_DEV_TESTS_ROOT", str(ROOT.parent / "RimWorldDevTests")))
LAUNCHER = HARNESS / "rimtest.py"

if __name__ == "__main__":
    if not LAUNCHER.is_file():
        sys.exit(f"shared harness not found: {LAUNCHER}; set RIMWORLD_DEV_TESTS_ROOT to its checkout")
    sys.argv = [str(LAUNCHER), "--config", str(ROOT / "DevTests" / "devtests.json")] + sys.argv[1:]
    runpy.run_path(str(LAUNCHER), run_name="__main__")
