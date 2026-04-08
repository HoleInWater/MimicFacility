#!/usr/bin/env bash
# Start the HAL 9000 voice server for INTAKE
# Unity's PiperTTSClient connects to localhost:5000

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

if [ ! -d "venv" ]; then
    echo "[HAL] Creating virtual environment..."
    python3 -m venv venv
    source venv/bin/activate
    pip install piper-tts huggingface-hub
else
    source venv/bin/activate
fi

if [ ! -f "models/hal.onnx" ]; then
    echo "[HAL] Downloading HAL 9000 voice model..."
    python3 -c "
from huggingface_hub import hf_hub_download
hf_hub_download(repo_id='campwill/HAL-9000-Piper-TTS', filename='hal.onnx', local_dir='models')
hf_hub_download(repo_id='campwill/HAL-9000-Piper-TTS', filename='hal.onnx.json', local_dir='models')
print('[HAL] Model downloaded.')
"
fi

echo "============================================"
echo "  INTAKE — HAL 9000 Director Voice Server"
echo "  Port: 5000"
echo "  Model: HAL 9000 (Piper TTS)"
echo "============================================"
echo ""

python3 server.py
