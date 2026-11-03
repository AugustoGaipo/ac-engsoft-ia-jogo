# Jogo de puzzle 3D de troca de propriedades

Puzzle 3D em primeira pessoa em que o jogador resolve as fases **trocando propriedades físicas**
entre objetos: posição, peso, momento e energia cinética.

Trabalho da disciplina de Engenharia de Software + IA, desenvolvido com o agente Claude Code e o
conjunto de skills [Agent Skills](https://github.com/addyosmani/agent-skills) do Addy Osmani.

- Ideia do jogo: [`docs/CONTEXTOJOGO.md`](docs/CONTEXTOJOGO.md)
- Diário do processo: [`docs/processo/DIARIO.md`](docs/processo/DIARIO.md)
- Banco de ideias e rota de prioridade: [`docs/ideas/BANCO-DE-IDEIAS.md`](docs/ideas/BANCO-DE-IDEIAS.md)
- Mapa de módulos: [`CAPABILITY-MAP.md`](CAPABILITY-MAP.md)
- Arquitetura (C4): [`docs/architecture/C4.md`](docs/architecture/C4.md)

**Stack planejada:** Godot 4 (C#), biblioteca de regras em C# puro, API ASP.NET Core + SQLite.

## Compilar e testar

Requer o **.NET 8 SDK**. Rode na raiz do repositório (no Git Bash do Windows, antes:
`export PATH="$PATH:/c/Program Files/dotnet"`).

```
dotnet build Swapping.sln                                 # compila (avisos viram erros)
dotnet test Swapping.sln                                  # roda todos os testes xUnit
dotnet test Swapping.sln --collect:"XPlat Code Coverage"  # cobertura (cobertura.xml em tests/*/TestResults/)
dotnet format Swapping.sln                                # formata o código
```

- `src/Swapping.Rules/`: biblioteca pura de regras das trocas (sem Godot, IO, relógio ou aleatoriedade).
  Spec: [`docs/specs/SPEC-rules.md`](docs/specs/SPEC-rules.md).
- `tests/Swapping.Rules.Tests/`: testes xUnit, uma classe por área de regra.
- `src/Swapping.Levels/`: lê, valida e converte os arquivos de nível para o `rules` (só referencia o
  `Swapping.Rules`; a única classe que lê arquivos é o `LevelCatalog`).
  Spec: [`docs/specs/SPEC-levels.md`](docs/specs/SPEC-levels.md).
- `tests/Swapping.Levels.Tests/`: testes do leitor, do validador (um por código de erro), da ligação com o
  `rules`, do catálogo e um teste de design por sala.
- `levels/`: os níveis em JSON (`level_1.json`, `level_2.json`, `level_3.json`, `level_5.json`) e o
  `catalog.json` com a ordem. Formato na spec do `levels`. Os arquivos aceitam comentários `//`.

### Criar ou mudar um nível
1. Edite o JSON em `levels/`. O progresso de cada sala está em
   [`docs/design/PROGRESSAO-NIVEIS.md`](docs/design/PROGRESSAO-NIVEIS.md).
2. Rode `dotnet test Swapping.sln --filter "FullyQualifiedName~ShippedLevels"`. Um nível inválido falha
   o teste, e a mensagem mostra o arquivo e o caminho do problema (ex.: `level_1.json:rooms[0].objects[0].massKg`).

## API do jogo (`profiles` + `progress`)

- `src/Swapping.Api/`: API ASP.NET Core 8 + SQLite. Perfis com nome, ícone e código de acesso (só o
  hash fica no banco), login com bloqueio, token, envio de resultados, níveis destravados em ordem e
  rankings por tempo e por trocas. Specs: [`SPEC-profiles.md`](docs/specs/SPEC-profiles.md) e
  [`SPEC-progress.md`](docs/specs/SPEC-progress.md). Relatório:
  [`RELATORIO-api.md`](docs/processo/RELATORIO-api.md).
- `tests/Swapping.Api.Tests/`: testes xUnit com a API inteira em memória (`WebApplicationFactory`,
  SQLite em memória, relógio falso), mais os testes de arquitetura.

```
dotnet run --project src/Swapping.Api       # sobe em http://127.0.0.1:5257 (só no PC), banco swapping.db
dotnet restore Swapping.sln --locked-mode   # confere que os pacotes batem com os packages.lock.json
dotnet list Swapping.sln package --vulnerable --include-transitive   # auditoria das dependências
```

| Rota | Login | O que faz |
|---|---|---|
| `POST /profiles` | — | cria um perfil `{ name, icon }` e devolve o código de acesso e um token |
| `POST /sessions` | — | entra com `{ name, accessCode }` e devolve um token novo |
| `GET` / `PATCH /profiles/me` | token | mostra o meu perfil / troca o ícone |
| `POST /profiles/me/access-code` | token | gera um código novo (precisa do atual) |
| `DELETE /sessions/current` | token | sai |
| `GET /icons`, `GET /levels` | — | os 12 ícones e os níveis do catálogo |
| `POST /results` | token | envia `{ levelId, timeMs, swaps, deaths, roomRestarts, finishedAt }` |
| `GET /progress/me` | token | níveis destravados e os meus melhores resultados |
| `GET /rankings/{levelId}?mode=time\|moves&top=N` | — | o ranking do nível (padrão 20, até 100) |
| `GET /rankings/{levelId}/me?mode=…` | token | a minha posição e o total de jogadores |

Os erros vêm em `application/problem+json` com um campo `code` (ex.: `InvalidBody`, `NameTaken`,
`ProfileLocked`, `LevelLocked`, `InvalidQuery`). Fora de `Development`, a API exige HTTPS.
