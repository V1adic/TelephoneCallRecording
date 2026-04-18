# Release Checklist

## Before deploy
- Connection string is stored outside the repository.
- SMTP credentials are configured for the production host.
- `dotnet build --no-restore` passes for backend.
- `cmd /c npm run build` passes for frontend.
- `dotnet ef database update` was executed on the target database.

## Smoke after deploy
- `GET /health` returns `ok`.
- Register a user and confirm email.
- Login from the Vue frontend.
- Start a call and verify that duplicate active calls are blocked.
- End the call and verify calculated cost in the UI.

## Rollback
- Restore the previous backend publish folder.
- Restore the previous frontend `dist` directory.
- Revert database only with a reviewed migration rollback plan; do not rollback live schema blindly.
