# V13 — Configuration

**ASVS 5.0 L2** · [← dashboard](README.md)

## Status summary

✅ Implemented — least-privilege `calendar_app` role ([db/init/30-app-role.sh](../../../db/init/30-app-role.sh)) + secrets via env ([.env.example](../../../.env.example)).

---

| Req | L | State | Notes |
|-----|---|-------|-------|
| V13.2.2 | 2 | ✅ | App connects as the dedicated `calendar_app` `NOSUPERUSER` role owning only the `calendar` DB — see [../postgres-least-privilege.md](../postgres-least-privilege.md). (Today it connects as the bootstrap superuser — the gap this closes.) |
| V13.3.x | 2 | ✅ | Secrets injected via environment only ([.env.example](../../../.env.example) documents names, never values); `.env` is git-ignored; gitleaks pre-commit + CI scan. |
| V13.4.x | 2 | ✅ | Production runs as `Production` (no detailed errors); container runs non-root ([Dockerfile](../../../Dockerfile)); no directory browsing on static files. |
