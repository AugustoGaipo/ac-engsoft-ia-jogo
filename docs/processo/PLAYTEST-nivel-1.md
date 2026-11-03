# Ficha do teste de jogo — nível 1

> Para o usuário preencher jogando. Spec: [`SPEC-game.md`](../specs/SPEC-game.md) (critério 12 e
> "Open Questions"). Como abrir: `Godot_v4.7.2-stable_mono_win64.exe --path game` (nível 1) ou
> `… --path game -- --sandbox` (sala de testes).

**Controles:** WASD + mouse · espaço pula · `E` segura/solta · botão esquerdo arremessa · botão direito
troca · `R` reinicia a sala · `Esc` pausa.

## O que já veio dos testes de jogo de 2026-11-03
| Relato | O que foi feito |
|---|---|
| Mouse "bugado" ao andar | câmera interpolada, atualizada a cada quadro |
| Botão não acendia com o jogador em cima | botões do nível 1 contam o jogador (`any`) |
| Não sei os meus números nem os do objeto | HUD com massa, velocidade e energia |
| Não havia caixa para trocar na 1.2 | a próxima sala é montada ao cruzar a saída |
| A caixa trocada era lançada longe | a caixa ignora o "fantasma" do jogador por um instante |
| Empurrar não tinha peso | empurrão por força (200 N contra o atrito) |
| Morria em vez de procurar espaço | jogador em cápsula e busca em grade 3D |
| Esmagado pela caixa segurada, atravessei a parede; dava para voar nela | a caixa segurada não colide com quem a segura |
| Mira sempre visível | ponto em repouso, `+` sobre objetos |

## Por sala
Marque e comente.

| Sala | Entendi a lição sem ajuda? | Os números deram para ler? | Achei um atalho? | Comentário |
|---|---|---|---|---|
| 1.1 — andar, segurar, botão, porta, grade | | | | |
| 1.2 — primeira troca através do buraco | | | | |
| 1.3 — subir trocando com a bola, caixa no botão alto | | | | (pendente: passei com uma troca só?) |
| 1.4 — a caixa travada e a outra | | | | |

## Perguntas de gosto (a spec deixou para decidir jogando)
- **Cores do contorno:** branco (aceita), cinza (travada) e, nos níveis com mais propriedades, azul-claro
  (posição), laranja (massa), rosa (energia). O cinza na caixa cinza ficou visível?
- **Pulo:** o pulo físico de 70 kg (~0,5 m) é bom, ou prefere um pulo "de jogo"?
- **Mouse:** sensibilidade e campo de visão (75°).
- **Morte:** o escurecimento de 0,5 s está bom? Quer som ou efeito?
- **HUD:** números no canto e embaixo da mira, tempo no canto direito. Algo atrapalha?
- **Empurrar:** 10 kg anda, 30 kg rasteja, mais de ~34 kg não se mexe. Está bom?
- **Desempenho (critério 12):** o jogo ficou fluido (60 quadros por segundo ou mais)?

## Resultado de uma partida
| Data | Tempo | Trocas | Mortes | Reinícios | Observação |
|---|---|---|---|---|---|
| 2026-11-03 (log) | 33,1 s | 3 | 0 | 0 | primeira partida completa do usuário |
| | | | | | |
