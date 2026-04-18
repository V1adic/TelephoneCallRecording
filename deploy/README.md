# Deployment Notes

## Backend
- Build backend: `dotnet publish -c Release -o ./publish`
- Required secret inputs:
  - `ConnectionStrings__Default`
  - `EmailDelivery__SmtpHost`
  - `EmailDelivery__SmtpPort`
  - `EmailDelivery__Username`
  - `EmailDelivery__Password`
  - `EmailDelivery__FromAddress`
- Apply migrations on the target host before switching traffic:
  - `dotnet ef database update`

## Frontend
- Build frontend: `cmd /c npm run build`
- The resulting files are written to `frontend/dist`.
- Nginx serves `frontend/dist` as the site root and proxies `/auth`, `/calls`, and `/health` to ASP.NET Core.

## Smoke Checklist
1. `GET /health` returns `status=ok`.
2. Registration creates a user and sets the verification cookie.
3. Email confirmation accepts the code from the configured channel.
4. Login returns profile data and sets the auth cookie.
5. `/calls/start` and `/calls/end` work from the Vue UI.
