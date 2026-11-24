"""
EventScape Backend Proxy Server
================================
Flask server that proxies requests to an OpenAI-compatible API endpoint.
Handles chat completions with tool/function calling support for the
EventScape Unity AI Assistant.
"""

import os
import sys
import json
import logging
from datetime import datetime

from flask import Flask, request, jsonify
from flask_cors import CORS
from dotenv import load_dotenv
from openai import OpenAI, APIError, APIConnectionError, AuthenticationError

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

load_dotenv()

API_KEY = os.getenv("OPENAI_API_KEY", "")
BASE_URL = os.getenv("OPENAI_BASE_URL", "http://localhost:20128/v1")
MODEL = os.getenv("OPENAI_MODEL", "coding-combo")

if not API_KEY:
    print("WARNING: OPENAI_API_KEY is not set. Requests will likely fail.",
          file=sys.stderr)

# ---------------------------------------------------------------------------
# App factory
# ---------------------------------------------------------------------------

app = Flask(__name__)
CORS(app, resources={r"/*": {"origins": "*"}})

# Logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
)
logger = logging.getLogger(__name__)

# OpenAI client
client = OpenAI(
    api_key=API_KEY,
    base_url=BASE_URL,
)

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _error_response(message: str, status_code: int = 500, details: str | None = None):
    """Return a consistent JSON error envelope."""
    body = {"error": {"message": message, "status": status_code}}
    if details:
        body["error"]["details"] = details
    return jsonify(body), status_code


def _validate_chat_request(data: dict):
    """Validate the incoming /chat request body. Returns an error tuple or None."""
    if not data:
        return _error_response("Request body must be valid JSON.", 400)
    if "messages" not in data:
        return _error_response("'messages' field is required.", 400)
    messages = data["messages"]
    if not isinstance(messages, list) or len(messages) == 0:
        return _error_response("'messages' must be a non-empty list.", 400)
    return None


# ---------------------------------------------------------------------------
# Routes
# ---------------------------------------------------------------------------


@app.route("/", methods=["GET"])
def root():
    """Root endpoint for quick browser verification."""
    return jsonify({
        "service": "EventScape AI Backend Proxy",
        "status": "running",
        "endpoints": {
            "health": "/health",
            "chat": "/chat"
        },
        "upstream": {
            "base_url": BASE_URL,
            "model": MODEL
        }
    })


@app.route("/health", methods=["GET"])
def health():
    """Health-check endpoint. Returns model and base_url for diagnostics."""
    return jsonify({
        "status": "ok",
        "model": MODEL,
        "base_url": BASE_URL,
        "timestamp": datetime.utcnow().isoformat() + "Z",
    })


@app.route("/chat", methods=["POST"])
@app.route("/api/chat", methods=["POST"])
def chat():
    """
    Proxy a chat-completion request to the configured OpenAI-compatible API.

    Expects JSON:
        {
            "messages": [ {"role": "...", "content": "..."}, ... ],
            "tools":    [ ... ]          // optional
        }

    Returns the upstream API response verbatim.
    """
    data = request.get_json(silent=True)

    # Validate
    err = _validate_chat_request(data)
    if err is not None:
        return err

    messages = data["messages"]
    tools = data.get("tools")

    # Build kwargs for the OpenAI client
    kwargs: dict = {
        "model": MODEL,
        "messages": messages,
    }

    if tools:
        kwargs["tools"] = tools
        kwargs["tool_choice"] = "auto"

    logger.info(
        "POST /chat — %d message(s), %d tool def(s), model=%s",
        len(messages),
        len(tools) if tools else 0,
        MODEL,
    )

    try:
        response = client.chat.completions.create(**kwargs)
        # Convert the pydantic model to a plain dict for JSON serialisation
        return jsonify(response.model_dump())

    except AuthenticationError as exc:
        logger.error("Authentication failed: %s", exc)
        return _error_response("Authentication with upstream API failed.", 401,
                               details=str(exc))

    except APIConnectionError as exc:
        logger.error("Connection error: %s", exc)
        return _error_response(
            f"Could not connect to upstream API at {BASE_URL}.", 502,
            details=str(exc),
        )

    except APIError as exc:
        status = getattr(exc, "status_code", None) or 502
        logger.error("Upstream API error (%s): %s", status, exc)
        return _error_response("Upstream API returned an error.", status,
                               details=str(exc))

    except Exception as exc:
        logger.exception("Unexpected error in /chat")
        return _error_response("Internal server error.", 500,
                               details=str(exc))


# ---------------------------------------------------------------------------
# Catch-all for unsupported routes
# ---------------------------------------------------------------------------


@app.errorhandler(404)
def not_found(_):
    return _error_response("Not found.", 404)


@app.errorhandler(405)
def method_not_allowed(_):
    return _error_response("Method not allowed.", 405)


# ---------------------------------------------------------------------------
# Options pre-flight (belt-and-suspenders alongside flask-cors)
# ---------------------------------------------------------------------------


@app.after_request
def add_cors_headers(response):
    response.headers["Access-Control-Allow-Origin"] = "*"
    response.headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization"
    response.headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS"
    return response


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    host = os.getenv("HOST", "127.0.0.1")
    port = int(os.getenv("PORT", 5055))
    debug = os.getenv("FLASK_DEBUG", "0") == "1"
    logger.info("Starting EventScape backend on %s:%d (model=%s, base_url=%s)",
                host, port, MODEL, BASE_URL)
    app.run(host=host, port=port, debug=debug)
