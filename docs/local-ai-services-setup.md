# Local AI Services Setup

Ember does not ship model binaries. The game auto-detects local services and degrades gracefully when they are absent.

## Ollama

```powershell
winget install Ollama.Ollama
ollama pull qwen2.5:3b-instruct-q4_K_M
ollama serve
```

Expected local endpoint:

```text
http://localhost:11434/api/generate
```

If Ollama is down, Ember falls back to deterministic template barks.

## ComfyUI

```powershell
git clone https://github.com/comfyanonymous/ComfyUI.git C:\ComfyUI
cd C:\ComfyUI
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
python main.py
```

Drop an SDXL Turbo or SDXL-compatible checkpoint into:

```text
C:\ComfyUI\models\checkpoints
```

Expected local endpoint:

```text
http://localhost:8188
```

If ComfyUI is down, Ember uses placeholder portraits and keeps gameplay running.

## Ember Flow

1. Start `ollama serve`.
2. Start `python C:\ComfyUI\main.py`.
3. Run Ember.
4. New Game creates a `WorldProfile`.
5. Forge queues NPC portrait requests asynchronously.
6. Cache files are written under the platform persistent data path in `forge-cache/<hash>.png`.

Save files store cache-key references only. PNG bytes never enter the save file.
