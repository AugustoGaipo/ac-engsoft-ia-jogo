# Contexto para a próxima sessão (handoff curto)

Histórico completo das sessões: `docs/processo/HISTORICO-SESSOES.md` (só ler se precisar).

## O trabalho
Disciplina de IA (Prof. Pedro Calais): jogo puzzle 3D em 1ª pessoa de **troca de propriedades**, Godot 4.7.2
.NET + C#, com as skills do Addy Osmani (`agent-skills:`). **Entrega: sexta 2026-11-06** (slides + ~10 min).
Quinta é para slides e ensaio, sem código novo.

## Pronto
- `rules`, `levels` e a API (`profiles` + `progress`): completos e com relatório em `docs/processo/`.
- `game` nível 1: completo (`RELATORIO-game.md`, `PLAYTEST-nivel-1.md`).

## Em andamento: fatia 2 do `game` (spec `docs/specs/SPEC-game.md` "Slice 2", tarefas `tasks/game2/todo.md`)
- 3 níveis: **1 Posição**, **2 Peso** (posição + peso, com 2 barreiras), **3 Energia** (as três), mais o
  **Mapa de teste** no menu (sandbox com as 3 trocas, fora do progresso).
- **Feitas:** tarefa 12 (`LevelProgress`), 13 (novo catálogo), 14 (teclas 1/2/3, roda troca a propriedade, F + mouse gira o item) e 15 (menu, progresso salvo, botões).
- **Feito em 11-03 (fora do todo, pedidos do usuário):** sala de testes refeita (corredor + 6 baias),
  barreira que restaura (`restoresVariables`, rosa), encostar numa barreira de variáveis barra a troca,
  peso volta a 70 kg ao morrer, barra de arremesso (3 s, 400 J, teto suave 15 m/s). Detalhes no diário.
- **Feito em 11-03 (tarde):** `HoldLoad` (segurar depende do peso) e tarefa 16 (nível 2: 2.3 "Empréstimo", 2.4 "Pulo alto"; jogada sem janela). Falta o usuário jogar o nível 2.
- **Feito em 11-03 (noite):** sons (pulo, queda, troca, segurar, barra, batidas), tela de morte, HUD de emprestado (⇄), jogador lançado (`Launch`), não se segura através de barreira de objetos, tarefa 17 (nível 3: 3.1 Canhão, 3.2, 3.3 Catapulta). Falta o usuário jogar os níveis 2 e 3.
- **Feito em 11-05:** tarefa 18 (relatório `RELATORIO-game-fatia2.md`, ficha `PLAYTEST-niveis-2-3.md`, README, C4), fundo do menu, teclas na pausa, pegar com o botão esquerdo, exportação Windows (`build/Swapping-windows.zip`; ver README).
- **Próxima:** slides e ensaio (`docs/apresentacao/ROTEIRO.md`). Sem código novo.
- **Depois da entrega:** editor de fases no Godot (peças como nós + "Exportar fase" para JSON com o
  validador). Adiado: resta menos de 50 % do plano Pro normal até sexta.
- Sempre `dotnet build game/Swapping.Game.csproj` antes do `dotnet test` (senão o teste sem janela roda o jogo velho).

## Processo de cada tarefa
Teste vermelho → código → 1–2 mutações → diário (`docs/processo/DIARIO.md`) → marcar no todo → commit e push.

## Pendências com o usuário
- Ficha dos níveis 2 e 3 para o usuário preencher.
- Cores do contorno.
- Skills desligadas para economizar tokens (ver memória `skills-desligadas`).

## Comandos
- `export PATH="$PATH:/c/Program Files/dotnet"; dotnet test Swapping.sln` (inclui o teste sem janela do nível 1).
- Jogo: `Godot…exe --path game` (`-- --sandbox` sala de testes). Godot em
  `%LOCALAPPDATA%/Microsoft/WinGet/Packages/GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe/Godot_v4.7.2-stable_mono_win64/`.
