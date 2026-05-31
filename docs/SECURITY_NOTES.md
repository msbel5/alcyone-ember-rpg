# Ember — Security Notes

> Short, enforced policy. Audit item EMB-052.

## Secrets
- **No secret is ever committed.** A repo-wide scan (`api[_-]?key|secret|bearer|password = "..."`)
  found **zero** hardcoded keys. Keep it that way.
- **Keys & endpoints come from environment variables only:**
  - `EMBER_LOCAL_LLM_ENDPOINT` — optional local LLM HTTP endpoint (e.g. Ollama).
  - `EMBER_NATIVE_LLM_MODEL` — optional override for the local GGUF model path.
  - Cloud LLM `ApiKey` — passed in via `CloudLlmConfig`/`LlmClients`; sourced from the
    environment by the caller, never literal in code. The `Authorization: Bearer <key>` header is
    only added when a non-empty key is supplied.
- `.gitignore` blocks `.env`, `*.env`, `secrets.json`, `*.key`, `*.pem`, `*.pfx`, `*.p12`, and
  `appsettings.*.local.json` so local secret material can't be staged by accident.
- Dependency URLs, model paths, and provider endpoints may appear in logs/proofs only after redacting
  tokens, keys, bearer strings, and auth-bearing query parameters.

## Network / cloud policy (see also EMB-044)
- Cloud / network LLM providers are **opt-in and disabled by default**, and are **never
  authoritative** over world state. The default build must not call out to the network for
  gameplay; the local Qwen GGUF (offline) is the shipped path.

## If you add a new provider that needs a key
1. Read it from an environment variable (document the var name here).
2. Never log the key. Never put it in a URL query string.
3. Never enable the provider by default.
