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
