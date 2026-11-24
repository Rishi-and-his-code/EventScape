# EventScape Backend

Flask proxy server that forwards chat-completion requests to an OpenAI-compatible API, with tool/function-calling support for the EventScape Unity AI Assistant.

## Quick Start

```bash
cd backend/

# Create a virtual environment (recommended)
python -m venv .venv
source .venv/bin/activate   # Linux/macOS
# .venv\Scripts\activate    # Windows

# Install dependencies
pip install -r requirements.txt

# Configure (edit .env)
cp .env .env.local   # optional: keep a local override
# Set OPENAI_API_KEY, OPENAI_BASE_URL, OPENAI_MODEL in .env

# Run the server
python server.py
```

The server starts on **http://127.0.0.1:5055** (port 5055 avoids macOS AirPlay on 5000).

## Configuration (.env)

| Variable          | Default                         | Description                          |
|-------------------|---------------------------------|--------------------------------------|
| `OPENAI_API_KEY`  | *(required)*                    | API key for the upstream LLM         |
| `OPENAI_BASE_URL` | `http://localhost:20128/v1`     | Base URL of the OpenAI-compatible API|
| `OPENAI_MODEL`    | `coding-combo`                  | Model name to use for completions    |
| `PORT`            | `5055`                          | Port the Flask server listens on     |
| `FLASK_DEBUG`     | `0`                             | Set to `1` for auto-reload           |

## API Endpoints

### `GET /health`
Returns server status, configured model, and base URL.

### `POST /chat`
Proxies a chat-completion request.

**Request body:**
```json
{
  "messages": [
    {"role": "system", "content": "You are a helpful assistant."},
    {"role": "user",   "content": "Place 40 chairs facing the stage."}
  ],
  "tools": [ ... ]
}
```

**Response:** The raw OpenAI-compatible chat-completion response, including any `tool_calls`.

## Running Tests

```bash
# Start the server first, then in another terminal:
python test_backend.py
```

Tests cover: health check, simple chat, tool-calling chat, and error handling.

## Project Structure

```
backend/
├── .env                 # Environment variables (not committed)
├── requirements.txt     # Python dependencies
├── server.py            # Flask proxy server
├── test_backend.py      # Automated test suite
└── README.md            # This file
```
