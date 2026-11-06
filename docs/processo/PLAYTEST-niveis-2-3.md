# Ficha do teste de jogo — níveis 2 (Peso) e 3 (Energia)

> Para o usuário preencher jogando. Spec: [`SPEC-game.md`](../specs/SPEC-game.md), "Slice 2" (critério 17).
> Como abrir: `jogar.bat` → menu › Níveis (o nível 2 libera ao terminar o 1), ou o `.exe` exportado.

**Controles:** WASD + mouse · `Shift` corre · espaço pula · botão esquerdo pega (com item: toque solta,
segurar arremessa) · botão direito troca · `1` `2` `3` ou roda escolhem a troca · `F` + mouse gira o item ·
`R` reinicia a sala · `Esc` pausa (com "Controles").

## O que já veio dos testes de jogo de 2026-11-05
| Relato | O que foi feito |
|---|---|
| Na 1.3 dava para passar com uma troca só | era um bug; corrigido (confirmado pelo usuário) |
| Voltando para a sala anterior não dava para trocar | a sala ativa é a sala onde o jogador está; a porta devolve os atributos a cada passagem |
| HUD sem diferença entre as habilidades | uma cor por habilidade (azul posição, laranja massa, rosa energia) em chips, mira, anel e painel |
| Não sabia se podia segurar o objeto | contorno grosso só quando dá para segurar; anel na mira com as trocas aceitas e X se recusado |
| Fundo do menu quebrado ao mudar o tamanho da janela | o fundo preenche a janela; os textos não ficam atrás da folha |
| Queria ver as teclas durante o jogo | "Controles" na pausa |
| Pegar no E era estranho | pegar passou para o botão esquerdo |

## Por sala
Marque e comente.

| Sala | Entendi a lição sem ajuda? | Os números deram para ler? | Achei um atalho? | Comentário |
|---|---|---|---|---|
| 2.1 — primeira troca de peso | | | | |
| 2.2 — peso com barreira | | | | |
| 2.3 — "Empréstimo" | | | | |
| 2.4 — "Pulo alto" | | | | |
| 3.1 — "Canhão" | | | | |
| 3.2 — energia com as três trocas | | | | |
| 3.3 — "Catapulta" | | | | |

## Perguntas de gosto
- **Barra de arremesso:** 3 s para encher, até 400 J. Rápida ou lenta demais?
- **Segurar pelo peso:** objetos pesados não sobem. Ficou claro quando dá e quando não dá?
- **Sons:** pulo, queda, troca, segurar, barra e batidas. Algum incomoda?
- **Lançado (3.1 e 3.3):** a velocidade do lançamento deu para controlar?
- **Desempenho:** o jogo ficou fluido (60 quadros por segundo ou mais)?

## Resultado de uma partida
| Data | Nível | Tempo | Trocas | Mortes | Reinícios | Observação |
|---|---|---|---|---|---|---|
| 2026-11-05 (smoke) | 2 | 41,7 s | 8 | 1 | 1 | jogada sem janela do teste automático |
| 2026-11-05 (smoke) | 3 | 38,9 s | 7 | 5 | 5 | jogada sem janela do teste automático |
| | | | | | | |
