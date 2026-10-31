# Progressão dos níveis

Curva de aprendizado do jogo: o que o jogador aprende em cada nível e em cada sala.
Base da spec do módulo `levels`. Estado: **proposta em revisão** (2026-10-31).

## Regras da curva
- Cada nível tem várias salas. Cada sala ensina **uma coisa nova**; a última sala do nível junta o que foi aprendido.
- **O tempo e as trocas aparecem na tela desde a sala 1.1.** O ranking mede o nível inteiro
  (ver "Níveis, salas e reinícios" em [`BANCO-DE-IDEIAS.md`](../ideas/BANCO-DE-IDEIAS.md)).
- Os níveis garantidos no corte (1, 3, 5 e 10) ensinam sozinhos o essencial de cada propriedade;
  os níveis 2, 4 e 6 aprofundam. Se o 2 for cortado, a morte por queda e por sufocamento passa para a sala 5.3.

## Níveis 1–2: Posição
| Sala | O que o jogador aprende |
|---|---|
| **1.1** | Andar, pular, segurar e soltar uma caixa; botão abre porta; grade de saída. Ainda sem troca. |
| **1.2** | Pega o aparelho e faz a **primeira troca de posição** com uma caixa do outro lado de um vão. Contorno branco; a mira muda de cor além dos 8 m. |
| **1.3** | Trocar para pôr a caixa no botão, que fica num lugar alto. |
| **1.4** | **Contorno colorido**: uma caixa que não aceita posição; é preciso usar outra. |
| **2.1** | **Barreira 1** (barra objetos): a caixa não passa, mas atravessa por troca de posição. |
| **2.2** | **Barreira 2** (barra o jogador): o jogador atravessa trocando com uma caixa do outro lado. |
| **2.3** | **Barreira 3** (zona sem trocas): preparar as trocas antes de entrar. |
| **2.4** | **Morte por queda e por sufocamento**: trocar com uma caixa num vão apertado mata; o ponto livre mais próximo salva quando dá. |

## Níveis 3–4: Peso
| Sala | O que o jogador aprende |
|---|---|
| **3.1** | **Botão que exige mais peso que o jogador**: trocar o peso com uma caixa leve. |
| **3.2** | **O pulo depende da massa**: com 7 kg, o jogador pula 1,84 m. |
| **3.3** | **Vidro quebradiço**: ficar pesado para quebrar o vidro ou leve para passar por cima. |
| **3.4** | Junta as três ideias. |
| **4.1** | **Água**: pesado afunda e anda no fundo, leve boia. **Barra de ar** (15 s) e afogamento. |
| **4.2** | **Segurar entra na conta**: segurar uma caixa leve para boiar. |
| **4.3** | **Gás denso**: flutuar no ar ficando muito leve. |
| **4.4** | **Pilha sobre vidro**: empilhar caixas até o vidro sentir o peso de todas. |

## Níveis 5–6: Energia
| Sala | O que o jogador aprende |
|---|---|
| **5.1** | **Receber energia** de uma bola em movimento e ser lançado na direção dela. |
| **5.2** | **Dar energia**: correr e passar a energia para uma caixa parada. |
| **5.3** | **Barra de lançamento**: carregar e jogar a caixa no botão. A queda mata. |
| **5.4** | Junta tudo: pegar energia de um objeto e entregar para outro. |
| **6.1** | **Recuo no ar**: jogar uma caixa enquanto voa para mudar a trajetória. |
| **6.2** | **Elasticidade**: uma bola que quica guarda a energia por mais tempo. |
| **6.3** | **Barreira 3 com energia**: a restauração inverte a direção. |

## Níveis 7–10: Combinações
| Nível | Ideia central |
|---|---|
| **7** pos+peso | Levar o peso para longe: trocar o peso e depois a posição. |
| **8** pos+energia | A troca de posição mantém a velocidade: correr, trocar com uma caixa distante e sair voando lá. |
| **9** peso+energia | Deixar uma caixa leve para ela acelerar com a mesma energia. |
| **10** as três | Sala final com água, vidro, barreiras e as três trocas. |
| **11** (P2) | Troca simultânea. |

## Escopo da entrega (sexta, 2026-11-06)
- Jogável: **nível 1** (salas 1.1 a 1.4).
- Os demais níveis ficam descritos aqui e entram na apresentação como continuação planejada.

## Em aberto
- Recarga dos níveis 7–10 (decidir jogando; nos níveis 1–6 é 1 s).
- Raio de busca do ponto livre mais próximo (troca normal e quântico).
