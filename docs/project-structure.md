# PriceProof SA Project Structure

```text
priceProof/
|- docs/
|  |- implementation-plan.md
|  `- project-structure.md
|- infra/
|  |- docker-compose.yml
|  `- env/
|     |- backend.env.example
|     `- frontend.env.example
|- src/
|  |- backend/
|  |  |- PriceProofSA.slnx
|  |  |- .dockerignore
|  |  |- src/
|  |  |  |- PriceProofSA.Api/
|  |  |  |- PriceProofSA.Application/
|  |  |  |- PriceProofSA.Domain/
|  |  |  `- PriceProofSA.Infrastructure/
|  |  `- tests/
|  |     |- PriceProofSA.Application.Tests/
|  |     `- PriceProofSA.Api.Tests/
|  `- frontend/
|     |- app/
|     |- components/
|     |- lib/
|     |- tests/
|     |- Dockerfile
|     `- package.json
|- README.md
|- .gitignore
`- .editorconfig
```

## Notes
- Backend projects use a clean architecture split while staying pragmatic about EF Core reads.
- Frontend lives in a separate app so it can later evolve independently or be replaced by a native client.
- `infra` contains local orchestration and environment templates rather than deployment-specific cloud manifests.
