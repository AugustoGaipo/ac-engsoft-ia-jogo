# CLAUDE.md — Jogo de Puzzle 3D de Troca de Propriedades

## Visão geral
- **Gênero:** puzzle 3D em primeira pessoa, inspirado em Portal e Superliminal (mas com mecânica **própria**, sem copiar).
- **Engine:** Unity.
- **Plataforma:** computador (mouse + teclado).
- **Visual:** simples e limpo (formas geométricas, cores sólidas).

## Mecânica central: trocar propriedades
O jogador resolve as fases **trocando propriedades físicas** entre as coisas.

Exemplo: uma porta só abre com um botão pesado apertado. O jogador só tem uma caixinha leve. Ele troca o **peso** da caixinha com um bloco de ferro → a caixinha fica pesada e aperta o botão, e o bloco fica leve.

### Propriedades da primeira versão
| Propriedade | O que acontece | Dificuldade de fazer |
|---|---|---|
| **Posição** | Os dois objetos trocam de lugar | Fácil |
| **Peso / massa** | Um fica pesado, o outro leve | Fácil |
| **Velocidade / momento** | Um objeto parado sai em movimento e o que se movia para | Médio |
| **Energia cinética** | Troca "quanto de energia de movimento" cada um tem | Difícil |

**Energia × momento:** momento tem direção, energia não. Ao trocar energia, o jogo precisa decidir para onde o objeto vai. Exemplo com uma bolinha leve e rápida e um bloco pesado parado:
- trocar o **momento** → o bloco sai bem devagar;
- trocar a **energia** → o bloco sai um pouco mais rápido.

### Propriedades extras (para depois)
Tamanho, atrito, elasticidade, direção da gravidade, temperatura.

## Os 3 jeitos de trocar
Todos entram no jogo, unidos em **uma regra só**: *tudo é troca entre dois "recipientes"*.
- **Objeto ↔ objeto**: mira em dois objetos e troca uma propriedade.
- **Jogador ↔ objeto**: troca com o próprio jogador (ex.: ficar pesado para afundar, roubar a velocidade de uma bola para pular longe).
- **Arma ↔ objeto**: a arma "suga" um valor de um objeto, guarda, e solta em outro depois.

### Números na tela
Mostrar os valores dos objetos (ex.: 5 kg, 3 m/s). É obrigatório para o jogador entender a diferença entre energia e momento.

## Design de puzzles e dificuldade
- Toda troca mexe em **duas coisas ao mesmo tempo**. Esse "efeito colateral" é o coração do puzzle.
  - Puzzle fácil: o efeito colateral não importa.
  - Puzzle difícil: o efeito colateral atrapalha (ex.: o bloco de ferro segurava uma porta; ao ficar leve, a porta fecha).
- **Controles de dificuldade:**
  1. Quantos objetos dá para trocar na sala (2–3 = fácil, 5+ = difícil).
  2. Quantas propriedades estão liberadas.
  3. Ordem das trocas (A↔B depois B↔C dá diferente do contrário).
  4. Limite de trocas por fase.
  5. Tempo (trocar velocidade de algo em queda depende do momento certo).
- **Risco principal:** com trocas ilimitadas, o jogador acha atalhos. Para evitar:
  - objetos **travados** (não trocam nada);
  - objetos que só aceitam **certas propriedades** (indicado por cor);
  - só trocar com o que está **à vista** (nada através de parede).

## Ordem das fases (liberar aos poucos)
1. Fases 1–2: posição (objeto ↔ objeto)
2. Fases 3–4: peso
3. Fase 5: a arma guarda valores
4. Fase 6: o jogador entra na troca
5. Fases 7–8: velocidade / momento
6. Fase final: energia cinética

## Suposições a testar
- O jogador entende a troca sem tutorial longo (testar a fase 1 com alguém).
- A diferença energia × momento fica clara com os números na tela. Se não ficar, vira opcional.
- Dá para montar as fases rápido no Unity. Ideia: um script que monta as fases a partir de um arquivo de texto.

## Ainda não decidido
- Confirmar a ordem das fases acima.
- Nome do jogo.
