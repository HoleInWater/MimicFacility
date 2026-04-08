"""
HAL 9000 Voice Server for INTAKE
Runs Piper TTS with the HAL voice model as an HTTP server.
Unity's PiperTTSClient connects to this on port 5000.

Usage:
    python server.py
    ./start_hal_voice.sh
"""

import struct
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse, parse_qs
from piper import PiperVoice

MODEL_PATH = "models/hal.onnx"
HOST = "0.0.0.0"
PORT = 5000

voice = None

class TTSHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        parsed = urlparse(self.path)
        params = parse_qs(parsed.query)

        if parsed.path == "/health" or parsed.path == "/api/health":
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(b'{"status":"ok","model":"hal-9000"}')
            return

        text = params.get("text", [""])[0]
        if not text:
            self.send_response(400)
            self.send_header("Content-Type", "text/plain")
            self.end_headers()
            self.wfile.write(b"Missing 'text' parameter")
            return

        preview = text[:80] + ('...' if len(text) > 80 else '')
        print(f'[HAL] "{preview}"')

        try:
            chunks = list(voice.synthesize(text))
            raw = b''.join([c.audio_int16_bytes for c in chunks])
            sr = voice.config.sample_rate

            # Build WAV manually (Python 3.14 wave module is strict)
            wav_header = b'RIFF'
            wav_header += struct.pack('<I', 36 + len(raw))
            wav_header += b'WAVE'
            wav_header += b'fmt '
            wav_header += struct.pack('<IHHIIHH', 16, 1, 1, sr, sr * 2, 2, 16)
            wav_header += b'data'
            wav_header += struct.pack('<I', len(raw))

            audio_data = wav_header + raw
            duration = len(raw) / sr / 2

            self.send_response(200)
            self.send_header("Content-Type", "audio/wav")
            self.send_header("Content-Length", str(len(audio_data)))
            self.send_header("X-Sample-Rate", str(sr))
            self.send_header("X-Duration-Seconds", f"{duration:.2f}")
            self.end_headers()
            self.wfile.write(audio_data)

            print(f"[HAL] Sent {len(audio_data)} bytes ({duration:.1f}s)")

        except Exception as e:
            print(f"[HAL] Error: {e}")
            self.send_response(500)
            self.send_header("Content-Type", "text/plain")
            self.end_headers()
            self.wfile.write(f"Synthesis error: {e}".encode())

    def log_message(self, format, *args):
        pass

def main():
    global voice

    print(f"[HAL] Loading model: {MODEL_PATH}")
    voice = PiperVoice.load(MODEL_PATH)
    print(f"[HAL] Model loaded. Sample rate: {voice.config.sample_rate}")

    print(f"[HAL] Starting server on {HOST}:{PORT}")
    print(f"[HAL] Unity connects to: http://localhost:{PORT}")
    print(f"[HAL] Test: curl 'http://localhost:{PORT}?text=Hello+Dave' -o test.wav")
    print(f"[HAL] Health: http://localhost:{PORT}/health")
    print()

    server = HTTPServer((HOST, PORT), TTSHandler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n[HAL] Server stopped.")
        server.server_close()

if __name__ == "__main__":
    main()
