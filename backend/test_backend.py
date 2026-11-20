#!/usr/bin/env python3
"""
EventScape Backend — Automated Test Suite
==========================================
Run with:  python test_backend.py
Requires:  pip install requests
Server must be running on http://localhost:5000
"""

import sys
import json
import requests

BASE_URL = "http://localhost:5000"
PASS = 0
FAIL = 0


def report(name: str, passed: bool, detail: str = ""):
    global PASS, FAIL
    tag = "PASS ✓" if passed else "FAIL ✗"
    print(f"  [{tag}] {name}")
    if detail:
        for line in detail.strip().splitlines():
            print(f"         {line}")
    if passed:
        PASS += 1
    else:
        FAIL += 1


def separator(title: str):
    print(f"\n{'─' * 60}")
    print(f"  {title}")
    print(f"{'─' * 60}")


# ───────────────────────────────────────────────────────────
# Test 1: Health check
# ───────────────────────────────────────────────────────────

def test_health():
    separator("Test 1 — GET /health")
    try:
        r = requests.get(f"{BASE_URL}/health", timeout=10)
        ok = r.status_code == 200
        report("Status 200", ok, f"Got {r.status_code}")

        data = r.json()
        has_status = data.get("status") == "ok"
        report("status == 'ok'", has_status, json.dumps(data, indent=2))

        has_model = "model" in data
        report("Response contains 'model'", has_model)

        has_base = "base_url" in data
        report("Response contains 'base_url'", has_base)

    except requests.ConnectionError:
        report("Server reachable", False,
               f"Could not connect to {BASE_URL}. Is the server running?")
    except Exception as exc:
        report("Health check", False, str(exc))


# ───────────────────────────────────────────────────────────
# Test 2: Simple chat (no tools)
# ───────────────────────────────────────────────────────────

def test_simple_chat():
    separator("Test 2 — POST /chat (no tools)")
    payload = {
        "messages": [
            {"role": "system", "content": "You are a helpful assistant. Reply in one sentence."},
            {"role": "user", "content": "What is an event layout?"},
        ]
    }
    try:
        r = requests.post(f"{BASE_URL}/chat", json=payload, timeout=60)
        ok = r.status_code == 200
        report("Status 200", ok, f"Got {r.status_code}")

        data = r.json()
        has_choices = "choices" in data and len(data["choices"]) > 0
        report("Response has 'choices'", has_choices)

        if has_choices:
            msg = data["choices"][0].get("message", {})
            has_content = bool(msg.get("content"))
            report("First choice has content", has_content,
                   f"Content: {msg.get('content', '')[:120]}...")

    except requests.ConnectionError:
        report("Server reachable", False, "Connection refused.")
    except Exception as exc:
        report("Simple chat", False, str(exc))


# ───────────────────────────────────────────────────────────
# Test 3: Chat with tool definitions
# ───────────────────────────────────────────────────────────

SAMPLE_TOOLS = [
    {
        "type": "function",
        "function": {
            "name": "PlaceObject",
            "description": "Place one or more objects in the 3-D event scene.",
            "parameters": {
                "type": "object",
                "properties": {
                    "objectType": {
                        "type": "string",
                        "enum": ["chair", "table", "stage", "podium", "screen",
                                 "speaker", "light", "barrier"],
                        "description": "Kind of object to place."
                    },
                    "count": {
                        "type": "integer",
                        "description": "How many to place."
                    },
                    "arrangement": {
                        "type": "string",
                        "enum": ["rows", "circle", "grid", "scatter", "line"],
                        "description": "Spatial arrangement."
                    },
                    "facingTarget": {
                        "type": "string",
                        "description": "What the objects should face, e.g. 'stage'."
                    },
                    "columns": {
                        "type": "integer",
                        "description": "Columns per row when arrangement is 'rows' or 'grid'."
                    }
                },
                "required": ["objectType", "count"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "RemoveObject",
            "description": "Remove objects from the scene by type or selection.",
            "parameters": {
                "type": "object",
                "properties": {
                    "objectType": {
                        "type": "string",
                        "description": "Type of object to remove."
                    },
                    "all": {
                        "type": "boolean",
                        "description": "Remove all instances of this type."
                    }
                },
                "required": ["objectType"]
            }
        }
    }
]


def test_tool_calling():
    separator("Test 3 — POST /chat (with tools)")
    payload = {
        "messages": [
            {
                "role": "system",
                "content": (
                    "You are an AI event layout assistant. "
                    "Use the provided tools to manipulate the 3-D scene. "
                    "Always respond with tool calls when the user asks to place or remove objects."
                ),
            },
            {
                "role": "user",
                "content": "Place 5 rows of 8 chairs facing the stage.",
            },
        ],
        "tools": SAMPLE_TOOLS,
    }
    try:
        r = requests.post(f"{BASE_URL}/chat", json=payload, timeout=60)
        ok = r.status_code == 200
        report("Status 200", ok, f"Got {r.status_code}")

        data = r.json()
        has_choices = "choices" in data and len(data["choices"]) > 0
        report("Response has 'choices'", has_choices)

        if has_choices:
            msg = data["choices"][0].get("message", {})
            tool_calls = msg.get("tool_calls")
            has_tool_calls = isinstance(tool_calls, list) and len(tool_calls) > 0
            report("Response contains tool_calls", has_tool_calls,
                   json.dumps(tool_calls, indent=2) if tool_calls else "No tool_calls found")

            if has_tool_calls:
                fn_name = tool_calls[0]["function"]["name"]
                report(f"First tool call function name: {fn_name}",
                       fn_name in ("PlaceObject", "RemoveObject"))

    except requests.ConnectionError:
        report("Server reachable", False, "Connection refused.")
    except Exception as exc:
        report("Tool calling chat", False, str(exc))

    # Test 3b: EventScape place_grid tool
    separator("Test 3b — POST /chat (with EventScape place_grid tool)")
    eventscape_tools = [
        {
            "type": "function",
            "function": {
                "name": "place_grid",
                "description": "Places a rectangular grid of identical furniture items in the auditorium.",
                "parameters": {
                    "type": "object",
                    "properties": {
                        "furnitureId": {"type": "string", "description": "e.g. auditorium_chair"},
                        "rows": {"type": "integer"},
                        "cols": {"type": "integer"},
                        "rowSpacing_m": {"type": "number"},
                        "colSpacing_m": {"type": "number"},
                        "originX_m": {"type": "number"},
                        "originZ_m": {"type": "number"},
                        "rotY_deg": {"type": "number"}
                    },
                    "required": ["furnitureId", "rows", "cols", "rowSpacing_m", "colSpacing_m", "originX_m", "originZ_m", "rotY_deg"]
                }
            }
        }
    ]
    payload_grid = {
        "messages": [
            {
                "role": "system",
                "content": "You are an AI layout assistant for a virtual auditorium event planner. Use the place_grid tool to arrange furniture."
            },
            {
                "role": "user",
                "content": "Place 6 rows of 8 auditorium chairs facing the stage."
            }
        ],
        "tools": eventscape_tools
    }
    try:
        r = requests.post(f"{BASE_URL}/chat", json=payload_grid, timeout=60)
        ok = r.status_code == 200
        report("Status 200", ok, f"Got {r.status_code}")
        data = r.json()
        tool_calls = data.get("choices", [{}])[0].get("message", {}).get("tool_calls", [])
        has_grid_call = any(c.get("function", {}).get("name") == "place_grid" for c in tool_calls)
        report("Response called 'place_grid'", has_grid_call, json.dumps(tool_calls, indent=2) if tool_calls else "No tool calls")
    except Exception as exc:
        report("place_grid test", False, str(exc))


# ───────────────────────────────────────────────────────────
# Test 4: Error handling — bad request
# ───────────────────────────────────────────────────────────

def test_error_handling():
    separator("Test 4 — Error handling")

    # 4a: Empty body
    try:
        r = requests.post(f"{BASE_URL}/chat",
                          data="not json",
                          headers={"Content-Type": "application/json"},
                          timeout=10)
        is_error = r.status_code >= 400
        report("Invalid JSON → 4xx", is_error, f"Got {r.status_code}")
    except Exception as exc:
        report("Invalid JSON request", False, str(exc))

    # 4b: Missing messages
    try:
        r = requests.post(f"{BASE_URL}/chat", json={"tools": []}, timeout=10)
        is_error = r.status_code == 400
        report("Missing 'messages' → 400", is_error, f"Got {r.status_code}")
    except Exception as exc:
        report("Missing messages", False, str(exc))

    # 4c: Empty messages list
    try:
        r = requests.post(f"{BASE_URL}/chat", json={"messages": []}, timeout=10)
        is_error = r.status_code == 400
        report("Empty messages → 400", is_error, f"Got {r.status_code}")
    except Exception as exc:
        report("Empty messages list", False, str(exc))

    # 4d: 404 on unknown route
    try:
        r = requests.get(f"{BASE_URL}/nonexistent", timeout=10)
        is_404 = r.status_code == 404
        report("Unknown route → 404", is_404, f"Got {r.status_code}")
    except Exception as exc:
        report("Unknown route", False, str(exc))


# ───────────────────────────────────────────────────────────
# Runner
# ───────────────────────────────────────────────────────────

def main():
    print("=" * 60)
    print("  EventScape Backend — Test Suite")
    print("=" * 60)

    test_health()
    test_simple_chat()
    test_tool_calling()
    test_error_handling()

    print(f"\n{'=' * 60}")
    total = PASS + FAIL
    print(f"  Results: {PASS}/{total} passed, {FAIL}/{total} failed")
    print(f"{'=' * 60}\n")

    sys.exit(0 if FAIL == 0 else 1)


if __name__ == "__main__":
    main()
