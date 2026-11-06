# Relatório do `game` — fatia 2 (3 níveis, menu, progresso, online e exportação)

> Concluído de 2026-11-03 a 2026-11-05. Spec: [`SPEC-game.md`](../specs/SPEC-game.md), "Slice 2" (SC 13–18) ·
> Tarefas: [`tasks/game2/todo.md`](../../tasks/game2/todo.md) · Diário: [`DIARIO.md`](DIARIO.md) · Ficha do teste
> de jogo: [`PLAYTEST-niveis-2-3.md`](PLAYTEST-niveis-2-3.md) · Fatia 1: [`RELATORIO-game.md`](RELATORIO-game.md).

## Resumo
- **O que é:** o jogo completo para a entrega:
  - três níveis no menu, cada um ensinando uma troca: **1 Posição** (4 salas), **2 Peso** (4 salas) e
    **3 Energia** (3 salas);
  - o **Mapa de teste**, uma sala com as três trocas que fica fora do progresso;
  - progresso salvo, conta online com ranking por nível e um `.exe` de Windows exportável.
- **Regra de ouro (mantida):** o Godot só mede e aplica. As decisões novas são todas puras e testadas:
  `LevelProgress`, `PropertyKeys`, `HoldLoad`, `SwapRing`, `AbilityColors`, `ThrowCharge`, `HeldSpin` e
  `Online/GameApi`.
- **Como foi feito:** com as mesmas skills da fatia 1 (`spec-driven-development`,
  `planning-and-task-breakdown`, `test-driven-development`, `incremental-implementation`). Cada parte pura
  começou por um teste vermelho e passou por 1 ou 2 mutações. Os testes de jogo do usuário guiaram boa
  parte do trabalho: vários pedidos feitos fora do todo, todos registrados no diário.

| Medida | Valor (2026-11-05) |
|---|---|
| Testes do `GameLogic` | **235 aprovados de 235** (xUnit), incluindo os que rodam o Godot sem janela |
| Verificações do teste sem janela | **285** (`--smoke-test`: os níveis 1, 2 e 3 jogados por roteiro do começo ao fim) + **27** do menu (`--smoke-menu`) |
| Cobertura de linhas do `Swapping.GameLogic` | **98,8 %** sem `Online/GameApi.cs`, que é testado pelos testes da API (`Api.Tests/GameClient`); meta da spec ≥ 90 % |
| `rules` / `levels` | 121 testes (98,6 % das linhas) / 124 testes (96,9 %) |
| Build | 0 avisos (`TreatWarningsAsErrors`) |

## O que foi feito
| # | Tarefa | Principais peças |
|---|---|---|
| 12 | Progresso | `LevelProgress` (ordem do catálogo, melhores tempos, JSON; arquivo corrompido → começa do zero), `ProgressStore` |
| 13 | Novo catálogo | `level_2.json` (Peso) e `level_3.json` (Energia); testes de design por sala |
| 14 | Teclas das trocas | `PropertyKeys` (1/2/3, roda), `HeldSpin` (F + mouse gira o item) |
| 15 | Menu | `Menu.cs` (Jogar/continuar, Níveis com cartões travados, Mapa de teste, Controles), tela de fim e pausa com botões |
| 16 | Nível 2 | `level_2.tscn`, barreiras com visual próprio, `HoldLoad` (segurar depende do peso), jogada por roteiro |
| 17 | Nível 3 | `level_3.tscn` (3.1 Canhão, 3.2, 3.3 Catapulta), jogador lançado (`Launch`), jogada por roteiro |
| — | Pedidos do usuário | sala de testes refeita, barreira que restaura, barra de arremesso, sons, tela de morte, HUD com uma cor por habilidade, contorno grosso/fino, anel na mira, porta que salva o spawn e devolve atributos, conta e ranking, fundo do menu, teclas na pausa, pegar com o botão esquerdo |
| — | Exportação | preset "Windows Desktop" com as fases, `GameFiles` (lê do `.pck`), testado no `.exe` |
| 18 | Fechamento | cobertura, ficha, este relatório, README, C4, diário |

## Critérios de sucesso → verificações
| SC | Critério | Como é verificado |
|---|---|---|
| 13 | `LevelProgress`: ordem, dois melhores, ids desconhecidos, JSON, texto corrompido | unit (`LevelProgressTests`) + sem janela ("an unreadable file starts fresh…") |
| 14 | "Continuar" escolhe o primeiro nível aberto não terminado | unit |
| 15 | `PropertyKeys`: 1/2/3, propriedade não liberada recusada, padrão = primeira liberada | unit |
| 16 | Catálogo com os níveis 1, 2 e 3; teste de design por sala; testes da API com o catálogo novo | unit (`levels`) + `Api.Tests`. **Desvio:** o nível 3 tem 3 salas, não 2 (a 3.3 "Catapulta" foi acrescentada) |
| 17 | Menu sem argumentos; cartão travado não abre; terminar o 1 libera o 2; níveis 2 e 3 por roteiro; F gira; roda troca | sem janela (`--smoke-menu` e `--smoke-test`) |
| 18 | Cobertura ≥ 90 %, zero avisos, tudo verde | ver a tabela acima |

## O que ficou de fora ou pendente
- **Fila offline do `sync`:** a conta e o ranking funcionam, mas sem servidor o resultado não é guardado
  para envio depois. A `SPEC-sync` continua como rascunho.
- **Teste de jogo dos níveis 2 e 3 pelo usuário:** a ficha está pronta para preencher.
- **Teste instável:** a verificação "crosshair … small dot" do smoke falha às vezes, também em versões
  anteriores às mudanças de 11-05.
- **Assinatura do `.exe`:** sem assinatura, o SmartScreen avisa ao abrir.
- **Depois da entrega:** editor de fases no Godot.
