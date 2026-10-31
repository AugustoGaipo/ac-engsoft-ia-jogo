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
