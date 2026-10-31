# Diário do processo

Registro de como o trabalho está sendo feito com um agente de IA (Claude Code) e o conjunto de skills
**Addy Osmani Agent Skills** (`agent-skills:`). Cada entrada diz o que foi feito, qual skill guiou a etapa,
o que foi decidido e por quê. Serve de evidência para a apresentação.

---

## 2026-10-31 — Sessão 1: entender o trabalho e começar a ideia

**Skill usada:** `agent-skills:idea-refine` (fase 1: expandir ideias)

**O que aconteceu**
- O agente leu o enunciado (`docs/enunciado/`) e levantou os critérios: 3+ módulos, banco de dados,
  regras de negócio, API, autenticação, testes, documentação e modelos C4.
- Escolhi o conjunto de skills do Addy Osmani.
- Respondi às perguntas iniciais: jogo de puzzle 3D, sozinho, 1 semana, Unity, objetivo nota + portfólio.
- O agente apontou riscos: prazo curto, Unity sem domínio, e o Unity sozinho não cobre banco, login,
  API e testes.
- O agente propôs 6 variações (C# em todas as camadas, puzzle diário, fases criadas por jogadores,
  Three.js, jogo educativo, versão 10x mais simples).

**Pendência levantada:** "jogo" não está na lista de domínios do enunciado → pedir aprovação ao professor.

---

## 2026-10-31 — Sessão 2: a ideia própria e a escolha da engine

**Skill usada:** `agent-skills:idea-refine` (passando para a fase 2: avaliar direções)

**O que aconteceu**
- Achei que as variações fugiam do que eu imaginava e escrevi a minha ideia em `docs/CONTEXTOJOGO.md`:
  puzzle 3D em primeira pessoa em que o jogador **troca propriedades físicas** entre objetos
  (posição, peso, momento, energia cinética).
- O agente adaptou a arquitetura à minha ideia e propôs duas direções:
  - **A.** tudo no Unity e um backend mínimo;
  - **B.** regras de troca numa biblioteca separada e testada, fases em JSON, API com banco.
  Recomendou a B, porque testes e evidências valem 40% da nota.
- O agente propôs um corte de escopo para caber em 1 semana: 5 fases (posição, peso, arma),
  com momento e energia como extras.

**Decisão: trocar a engine.** Perguntei o que poderia substituir o Unity.

| Opção | A favor | Contra |
|---|---|---|
| Three.js + Rapier (web) | o agente consegue testar no navegador; uma linguagem só; link fácil para portfólio | menos ferramentas visuais |
| Godot 4 + C# | engine de verdade, gratuita, exporta para a Steam, visual melhor | o agente não vê o editor; não conheço a engine |
| Unity | o que eu tinha pensado | não domino e o agente não vê o editor; sem vantagem sobre o Godot |

- Falei que queria levar o jogo adiante, talvez para a Steam. O agente explicou que jogos web também
  chegam à Steam (Electron), mas que o Godot leva vantagem no visual e no crescimento do jogo.
- Perguntei se dá para fazer algo parecido com Portal e Superliminal. O agente explicou que a minha
  mecânica não precisa das partes difíceis desses jogos (portais e perspectiva forçada) e que as duas
  ferramentas dão conta do que ela precisa.
- **Escolhi Godot 4 com C#.** Para compensar o risco, as regras de troca ficam numa biblioteca em C#
  puro, fora da engine, onde o agente consegue testar tudo.

**Aprendizado:** na Steam, login, ranking e saves vêm do Steamworks. O backend atende à disciplina;
o que segue para um possível lançamento são as regras de troca e as fases.

**Próximos passos**
- Instalar o .NET SDK 8 e o Godot 4 (.NET).
- Confirmar o corte de escopo.
- Terminar a `idea-refine` (premissas e riscos, one-pager em `docs/ideas/`).
- Spec (`agent-skills:spec-driven-development`) e C4 de Contexto e Container antes do código.

---

## 2026-10-31 — Repositório no GitHub

- Pedi um histórico do processo para mostrar na apresentação → criado este diário.
- Repositório público criado: https://github.com/AugustoGaipo/ac-engsoft-ia-jogo
  (o enunciado do professor ficou fora, via `.gitignore`).
- Daqui em diante, cada etapa ganha uma entrada aqui e um commit, para o histórico do git também
  servir de evidência.

---

## 2026-10-31 — Domínio "jogo" aprovado

- O enunciado lista exemplos de domínios (Gestão, Educação, Saúde etc.) e diz que outro domínio
  precisa de aprovação do professor.
- O professor já tinha liberado jogos, **desde que não fossem simples demais** (como um jogo da cobrinha).
- O projeto atende a essa condição: mecânica própria de troca de propriedades físicas, 3+ módulos
  (regras, jogo, API), banco de dados, login, validação das trocas no servidor e testes automatizados.

---

## 2026-10-31 — Fases e habilidades

**Skill usada:** `agent-skills:idea-refine` (fase 2: convergir)

- Propus 10 fases: 2 com cada habilidade sozinha, 1 com cada dupla e 1 com as três juntas,
  repetindo a mesma habilidade em fases seguidas, como no Portal.
- O agente ajustou a ordem para a energia cinética, a mais difícil de entender, vir depois do peso:

| Fase | Habilidade |
|---|---|
| 1–2 | Posição |
| 3–4 | Peso |
| 5–6 | Energia cinética |
| 7 | Posição + Peso |
| 8 | Posição + Energia |
| 9 | Peso + Energia |
| 10 | As três |

- **Regra do peso:** o volume continua o mesmo; o que muda é a densidade (massa ÷ volume).
- **Momento:** fica de fora por enquanto, porque ainda não sei como aplicar.
- **Mais duas habilidades** entram no futuro, ainda sem definição (quero uma que lide com números).
  → A arquitetura precisa permitir adicionar propriedades novas sem reescrever o resto.
- **Decisão pendente para a spec:** ao trocar o peso, o objeto mantém a velocidade ou mantém a energia?

---

## 2026-10-31 — Eixos de evolução das habilidades

**Skill usada:** `agent-skills:idea-refine` (fase 2: convergir)

- Perguntei ao agente qual troca parece mais evoluída. Minha ideia: **jogador ↔ objeto** como a mais
  básica, porque ensina melhor, limita atalhos nas fases e abre espaço para evoluir depois.
  O agente concordou: mira em uma coisa só, o jogador sente a troca no próprio corpo, e na biblioteca
  de regras o jogador é só mais um "recipiente".
- Propus combinar duas habilidades, primeiro **com um intervalo** entre as trocas e depois
  **ao mesmo tempo**. O agente organizou isso em dois eixos de evolução:

| Alcance (com quem troco) | Combinação (quantas de uma vez) |
|---|---|
| 1. jogador ↔ objeto | 1. uma habilidade por vez |
| 2. objeto ↔ objeto | 2. duas, com intervalo entre elas |
| 3. arma que guarda valores | 3. duas ao mesmo tempo (intervalo zero) |

**Escopo das fases**
- Fases 1–10: só jogador ↔ objeto. Fases 1–6 com uma troca por vez; fases 7–10 combinam
  habilidades com intervalo (o intervalo vale entre qualquer troca).
- **Fase 11 (extra, se sobrar tempo):** troca simultânea.
- Depois: objeto ↔ objeto e a arma, mas a biblioteca de regras já nasce suportando as duas.

**Cuidados levados para a spec**
- Se o jogador aperta botões com o próprio peso, resolve fases sem trocar → botões só para objetos,
  ou que exigem mais peso que o jogador.
- Quando o jogador recebe energia, para onde ele é lançado? Câmera ou direção anterior?
- A troca simultânea não é "intervalo quase zero": as duas propriedades mudam no mesmo passo.

---

## 2026-10-31 — One-pager e banco de ideias

**Skill usada:** `agent-skills:idea-refine` (fases 2 e 3)

- **Teste de estresse do agente:** o ponto fraco é a viabilidade (1 semana, sozinho, aprendendo Godot).
  Premissa escondida: o personagem do Godot não é um corpo físico comum, então o peso e a energia
  do jogador vão ser simulados pelo nosso código. Critério de corte: se no dia 3 a troca não
  estiver jogável, ficam garantidas as fases 1, 3, 5 e 10.
- **Antitrapaça descartada:** o agente explicou que a física do Godot não é determinística (a mesma
  jogada pode dar resultados um pouco diferentes), então o servidor não conseguiria repetir a jogada.
  Argumentei que é um jogo offline para um jogador e que passar de fase é chegar à saída.
  O agente concordou e tirou a validação do escopo.
- **Decisões de física:** ao trocar o peso, a energia se mantém (a velocidade se ajusta); quem recebe
  energia sai na direção em que o outro se movia.
- **Duração da troca:** modelo C (permanente por padrão, objetos instáveis no futuro), começando pelo A.
- **Jogo offline:** o progresso vai para a API quando houver internet.
- **Novas ideias futuras:** power-up que zera a recarga, peso glorioso, downgrade de tempo limitado.
- **Pedido meu:** tudo o que não foi concretizado fica como possível, com espaço na arquitetura,
  para evitar gambiarras depois. Criado o [banco de ideias](../ideas/BANCO-DE-IDEIAS.md).
- **Resultado:** [one-pager](../ideas/troca-de-propriedades.md) com problema, direção, premissas,
  escopo, "não vamos fazer" e pontos de extensão.
- **Pedido para o futuro:** quando a arquitetura estiver quase pronta, o agente vai explicá-la num site
  interativo simples, com exemplos do dia a dia.

---

## 2026-10-31 — Ideias novas: densidade e tempo

- **Densidade:** além de afundar, serve para quebrar coisas quebradiças (vidro, gelo, tábua),
  com um limite numérico de peso ou pancada.
- **Tempo como habilidade:** em vez de trocar, o jogador **rouba** tempo de objetos em movimento
  constante (o mundo fica mais lento para ele) e **entrega** tempo a si ou a objetos (o mundo fica
  mais rápido).
- **Impacto na arquitetura apontado pelo agente:** o tempo não é uma troca (A ↔ B), e sim uma
  transferência de mão única (A → reserva → B), como a arma que guarda valores. A arquitetura passa a
  prever dois tipos de operação: trocar e transferir. As duas ideias entram como possíveis no
  banco de ideias, com ponto de extensão no one-pager.
- **Pressão:** o que quebra o vidro é a pressão (força ÷ área de contato), não só o peso. Um objeto
  pequeno e pesado quebra mais fácil que um grande com o mesmo peso. Registrado no banco de ideias.
- Fim das ideias por agora → começa a etapa de especificação.

---

## 2026-10-31 — Mapa de módulos e decisões de segurança

**Skill usada:** `agent-skills:spec-driven-development` (fase 0: mapa de módulos)

- O agente propôs dividir o sistema em 6 módulos (regras, fases, contas, progresso, sincronização,
  jogo), cada um com sua spec, e construir uma versão mínima do jogo cedo para testar o critério
  de corte do dia 3.
- **Idioma:** código e documentos técnicos em inglês.
- **Login:** propus só nome, para agilizar. O agente apontou que só com nome qualquer um enviaria
  pontuação no nome de outro, e sugeriu um perfil sem senha (nome + ícone) com uma chave secreta
  gerada pelo servidor. Não guarda senha nem e-mail, então não há o que vazar. No futuro, a conta
  da Steam substitui a chave.
- **Ranking:** dois modos, por tempo ou por movimentos, cada um usando o outro critério como desempate.
- **Tempo:** a velocidade e a sensibilidade do personagem nunca mudam; só o mundo muda.
- **Código de acesso:** propus que a chave fosse mostrada ao jogador para entrar de outro lugar.
  O agente ajustou para o servidor gerar o código (gerador criptográfico), com hash, bloqueio por
  tentativas erradas e opção de gerar um código novo.
- **Compartilhar partidas:** assistir aos replays dos primeiros do ranking. O agente lembrou que, como a
  física não é determinística, o replay mostra o caminho gravado em vez de re-simular. Fica como
  possível, com espaço previsto no módulo `progress`.
- **Fantasma no lugar do replay completo:** grava só o personagem (posição e direção da cabeça),
  sem os objetos. Fica para se sobrar tempo.
- **Rota de prioridade:** pedi uma ordem do que é mais importante. Criada no banco de ideias, em
  faixas P0 (essencial) a P3 (futuro).
- **Mapa de módulos aprovado** e salvo em [`CAPABILITY-MAP.md`](../../CAPABILITY-MAP.md).
- **Prioridade ajustada:** densidade (água, quebradiças, pressão) e o site explicando a arquitetura
  subiram para P1. O site é só para mim: precisa ser compreensível, não bonito.
- **Pressão:** a versão parada (peso ÷ área, objeto parado em cima) é simples e fica no P1 junto com a densidade; a pressão de impacto (objeto caindo) vai para o P3.

---

## 2026-10-31 — Fases contínuas e paredes especiais

- Propus fases contínuas, como no Portal, e paredes especiais: só entidades passam, só objetos passam,
  e uma parede que devolve o valor verdadeiro de quem atravessa. Combinadas, formam a parede de saída
  de cada fase: nada trocado passa para a próxima.
- **Apontamento do agente:** a restauração funciona porque as trocas só embaralham os valores (cada
  valor original está em exatamente um recipiente). A biblioteca `rules` precisa registrar de quem é
  cada valor, e isso entra na spec antes do código. Energia e posição mudam sozinhas com a física,
  então a restauração vale só para propriedades estáveis (peso).
- **Prioridade:** parede de saída no P0; fases contínuas e as outras paredes no P1.
- **Modelo de variáveis (ideia minha):** em vez de trocar valores, trocar as variáveis. Cada objeto tem
  a sua (Xa, Xb…), a física continua alterando o valor enquanto outro objeto a usa, e na parede cada um
  pega a sua de volta. Com isso a energia também pode ser restaurada. O agente adotou o modelo e
  levantou três pontos: energia cinética ou mecânica, direção na restauração, e deixar a posição fora.
- **Posição na restauração:** também entra. Na parede, troca só o lugar (não a energia): o objeto que
  estava parado aparece na parede e cai.
- **Alcance máximo:** cada habilidade tem uma distância máxima de troca, configurável por fase.
- **Parede como filtro:** a parede define o que passa (jogador, objetos) e quais variáveis passam;
  a mira da troca obedece ao mesmo filtro. Isso existe nas referências do gênero para separar as fases.
- **Regras finas das paredes:** cada variável volta para o seu dono; dentro de uma barreira que
  bloqueia trocas não há troca; barreiras de passagem só soltam o objeto segurado. Perguntei ao agente
  se isso ficou complicado demais para o jogador.
- **Opinião do agente:** as regras são coerentes (a parede é um filtro), mas muitos tipos de parede no
  começo confundem o jogador. Sugeriu o modelo completo na arquitetura e paredes introduzidas aos poucos.
- **Decisão:** 3 barreiras básicas e combináveis (barra objetos, barra o jogador, barra variáveis),
  cada uma com cor e ícone próprios. A grade de saída = barreira 1 + 3.
