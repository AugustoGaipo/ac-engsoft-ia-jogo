# Relatório do `game` (nível 1 no Godot)

> Concluído em 2026-11-03 e 2026-11-04. Spec: [`SPEC-game.md`](../specs/SPEC-game.md) ("SC n", decisões
> G1–G6) · Plano: [`tasks/game/plan.md`](../../tasks/game/plan.md) · Tarefas:
> [`tasks/game/todo.md`](../../tasks/game/todo.md) · Diário: [`DIARIO.md`](DIARIO.md) · Ficha do teste de
> jogo: [`PLAYTEST-nivel-1.md`](PLAYTEST-nivel-1.md).

## Resumo
- **O que é:**
  - o cliente Godot 4.7.2 .NET (`game/`), com as 4 salas do nível 1 jogáveis do começo ao fim;
  - a biblioteca pura `Swapping.GameLogic`, que guarda todas as decisões que dá para testar sem o motor;
  - uma sala de testes (`--sandbox`) e a prévia das peças no editor.
- **Regra de ouro (cumprida):** o Godot só mede e aplica. Quem decide se a troca vale é o `rules`; o
  conteúdo das salas vem do `levels`; o resto das decisões fica no `GameLogic`.
- **Como foi feito:**
  - skills usadas:
    - `spec-driven-development`: spec aprovada pelo usuário com G1–G6;
    - `planning-and-task-breakdown`: 11 tarefas e 3 checkpoints;
    - `test-driven-development`: teste vermelho antes de cada parte pura;
    - `incremental-implementation`;
  - **testes de jogo do usuário no meio do caminho:** 9 relatos viraram correções e testes.

| Medida | Valor |
|---|---|
| Testes do `GameLogic` | **102 aprovados de 102** (xUnit), 2 deles rodam o Godot sem janela |
| Verificações do teste sem janela | **145 de 145** (`--smoke-test level-1`, rodado pelo `dotnet test`) |
| Cobertura de linhas do `Swapping.GameLogic` | **98,8 %** (ramos 96,4 %); meta da spec ≥ 90 % |
| Toda a solução | 581 testes (106 `rules` + 125 `levels` + 248 API + 102 `GameLogic`) |
| Build | 0 avisos (`TreatWarningsAsErrors`, inclusive o projeto Godot) |
| Critérios de sucesso | **11 de 12** com verificação automática; o 12 (60 fps) é do teste de jogo |

## Arquitetura
```
game/ (Godot, C#)                     src/Swapping.GameLogic (puro)      src/Swapping.Levels → src/Swapping.Rules
  Main ─ LevelRunner (salas do JSON)    RunTracker, RunClock               LevelCatalog, LevelParser, RulesMapping
       ─ Player (cápsula, 70 kg)        Controls (G1), PushImpulse         World, SwapOperation, LevelRules
       ─ SwapController + MakeRoom      CrosshairState, SwapChoice, Readout
       ─ RunFlow (mortes, pausa, fim)   BarrierGeometry, FreeSpotSearch (G2)
       ─ Hud, LevelPreview (editor)     FallDeath (G3), ButtonState, OutlineColor, CooldownRing
       ─ SmokeTest (--smoke-test)
```

## O que foi feito em cada tarefa
| # | Tarefa | Principais peças |
|---|---|---|
| 1 | Projeto Godot e teste sem janela | `project.godot`, `Swapping.Game.csproj` na solução, cópia dos JSON (G6), `--smoke-test`, `--screenshot` |
| 2 | `GameLogic` e contador | `RunTracker`, `RunClock`, testes de arquitetura (sem Godot) |
| 3 | Cenário e peças do arquivo | `level_1.tscn` (gerado de `tools/scenery/`), `LevelRunner`, `Body` (CCD), `KillZone` |
| 4 | Botões, portas, barreiras | `ButtonState`, `FloorButton`, `Door`, `Barrier` (camadas), `Exit` |
| 5 | Jogador | `Player`, `Controls`, `PushImpulse`; câmera interpolada |
| 6 | Troca | `SwapController`, `CrosshairState`, `SwapChoice`, `Readout`, `BarrierGeometry`, 1ª HUD |
| 7 | Abrir espaço | `FreeSpotSearch`, `MakeRoom`; cenário de caixas sólidas |
| 8 | Partida | `RunFlow`, `FallDeath`, menu de pausa, tela de fim |
| 9 | HUD e contorno | `OutlineColor`, `CooldownRing`, contorno por "casca invertida" |
| 10 | Nível inteiro por roteiro | `WalkTo` no `SmokeTest`; o nível 1 é jogado até o fim |
| 11 | Fechamento | cobertura, esta ficha, este relatório, C4, README |

## Critérios de sucesso → verificações
"Sem janela" são as linhas do `SmokeTest` (o texto do `Check`); "unit" são os testes xUnit do `GameLogic`.

| # | Critério | Como é verificado |
|---|---|---|
| 1 | Build da solução e do `game/` sem avisos; todos os testes passam | `TreatWarningsAsErrors` em tudo; `dotnet test Swapping.sln` |
| 2 | O nível 1 vem do arquivo; cada sala monta as peças nas posições do arquivo | sem janela: "room X: … at the file position", "… rests on the scenery", "building a room twice leaves one set of pieces" (4 salas) |
| 3 | 1.2: caixa a até 8 m → mira branca; além → vermelha "fora de alcance" | sem janela: "room 1.2, the box within 8 m → white crosshair", "beyond 8 m → red \"fora de alcance\""; unit: `CrosshairStateTests` |
| 4 | Troca de posição na 1.2 → o jogador **em pé no chão** do outro lado | sem janela: "make room (SC 4): after the swap … not inside it", "… the player then stands on the floor across the pit"; unit: `FreeSpotSearchTests` |
| 5 | Caixa travada da 1.4 recusada (`Locked`) e ainda pode ser segurada e arremessada | sem janela: "the locked box → red \"travado\"", "refuses the swap", "can still be held", "and thrown" |
| 6 | Buraco → morte → sala reconstruída, jogador na entrada, tempo e trocas mantidos | sem janela: "run (SC 6): the pit kills…", "run (SC 6): time and swaps are kept, 1 death and 1 room restart"; unit: `RunTrackerTests` |
| 7 | "Reiniciar nível" → sala 1.1, tempo e trocas zerados | sem janela: "run (SC 7): restart level → room 1.1 …"; unit: `Restarting_the_level_starts_a_new_run_from_zero` |
| 8 | A sala 1.3 se resolve pelo caminho planejado | sem janela: "full run 1.3 (SC 8): the box lands on the high button and the door opens" |
| 9 | HUD com tempo e trocas desde a 1.1 e números do jogador e do objeto mirado | sem janela: "the HUD shows time and swaps", "… the aimed box's numbers", "… the player's numbers"; unit: `ReadoutTests`, `The_clock_shows_…` |
| 10 | O teste sem janela do nível 1 passa (código 0) | `GodotSmokeTests.The_smoke_test_of_level_1_passes`, e o roteiro completo: "full run (SC 10): …" (28,6 s, 4 trocas, 0 mortes) |
| 11 | `GameLogic` sem Godot e cobertura ≥ 90 % | unit: `ArchitectureTests` (2); cobertura 98,8 % |
| 12 | 60 fps no PC do usuário | teste de jogo ([ficha](PLAYTEST-nivel-1.md)); nas partidas do usuário não houve queixa de desempenho |

**Os critérios 1 a 11 têm verificação automática. O 12 depende do teste de jogo.**

## Decisões tomadas sem o usuário (as principais; todas no diário)
- **Peças montadas em código**, sem `.tscn` por peça. O cenário é **gerado** de tabelas
  (`tools/scenery/`) como caixas sólidas.
- **O jogador é uma cápsula** (a caixa girava com a mira). O empurrão é uma força de 200 N.
- **A busca de espaço** é uma grade 3D de 0,25 m até 1,5 m, nunca para baixo, preferindo subir.
- **A caixa trocada** ignora o jogador por 0,25 s (o "fantasma" do corpo cinemático no Jolt). **A caixa
  segurada** nunca colide com quem a segura.
- **Objeto fora da sala** (pelos lados ou por baixo) volta ao início; **jogador fora do nível** morre.
- **A sala de testes não entra no catálogo** (a API não a vê); usa o mesmo validador, menos a regra do
  número do nível.
- **Sala 1.3:** a plataforma deixa um corredor até a porta. O caminho planejado não chegava lá; o
  roteiro do teste achou.

## Problemas encontrados (o que cada um ensinou)
1. **Só o teste de jogo pegou** a falta de troca entre as salas, a caixa lançada, a busca sem diagonal
   e a caixa segurada. **O log de cada troca** (`[game] swap with …`) foi o que permitiu reproduzir os
   casos.
2. **O teste sem janela pegou** o cenário CSG oco (um corpo no fundo do chão parecia livre) e a sala
   1.3 sem saída pelo caminho planejado.
3. **A física do Jolt:** um corpo cinemático movido por código só chega ao motor no passo seguinte.
   Isso explicou a caixa lançada.
4. **Ferramentas:** um script do PowerShell 5 falhou com "→" num arquivo sem BOM; o vídeo do usuário
   não pôde ser visto (sem `ffmpeg`), e o log bastou.

## Pendências
- **Perguntas ao usuário:** como a 1.3 foi passada com uma troca só (atalho?); as cores do contorno.
- **Fora desta fatia:** os níveis 2, 3 e 5 (barreiras, massa, energia, barra de arremesso) pedem uma
  revisão da spec; o `sync` (fila de envio) ainda não está ligado a `RunFlow.LevelFinished`.
- O log `[game] swap with …` fica ligado enquanto o jogo estiver em teste.
