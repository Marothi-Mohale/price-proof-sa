# PriceProof SA Project Structure

```text
priceProof/
|- docs/
|  |- implementation-plan.md
|  `- project-structure.md
|- src/
|  |- PriceProof.Api/
|  |- PriceProof.Application/
|  |- PriceProof.Domain/
|  |- PriceProof.Infrastructure/
|  `- frontend/
|     |- app/
|     |- components/
|     |- lib/
|     |- tests/
|     |- Dockerfile
|     `- package.json
|- tests/
|  |- PriceProof.UnitTests/
|  `- PriceProof.IntegrationTests/
|- README.md
|- .gitignore
`- PriceProof.sln
```

## Notes
- The backend follows a clean architecture split while keeping API and persistence wiring pragmatic.
- The frontend is isolated in its own Next.js application so the web experience can evolve independently.
- Docker-based local orchestration lives at the repository root.
- Local operational configuration stays outside the repository.
