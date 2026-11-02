# Roteiro da apresentação (10 minutos)

> Rascunho feito no loop autônomo de 2026-11-02. Para você ajustar. Os números são os de hoje; atualize
> na quinta, depois do `game` e da API.

Como dividir os 10 minutos pelos critérios e pesos do trabalho:

| Bloco | Tempo | Critério (peso) | Ideia central |
|---|---|---|---|
| 1. O jogo e o problema | 1 min | complexidade (15) | "trocar propriedades" parece simples, mas exige um modelo próprio |
| 2. Especificação e arquitetura | 1 min 30 | especificação e arquitetura (15) | spec antes do código, mapa de módulos, C4 |
| 3. Como usei as skills | 2 min | uso das skills (20) | uma skill por fase, com evidência de cada uma |
| 4. Qualidade e testes | 2 min | qualidade e testes (20) | TDD, cobertura, mutação, bugs achados na revisão |
| 5. Evidências do processo | 1 min 30 | evidências (20) | diário, relatórios, commits, perguntas abertas |
| 6. Demo | 2 min | demo (10) | jogar a sala 1.2 e a 1.3 e mostrar o ranking |

O bloco 6 vale só 10 pontos e é o que mais pode dar errado. Por isso fica no fim e tem um vídeo
gravado como reserva.

---

## 1. O jogo e o problema (1 min)
- **Uma frase:** puzzle 3D em primeira pessoa em que você **troca** posição, peso e energia com os objetos.
- **O que deixa o projeto não trivial:**
  - a troca é de **variáveis**, não de valores (a analogia dos crachás);
  - a física continua escrevendo nelas;
  - existe uma **regra de ouro** que nunca pode quebrar.
- **Mostrar:** o slide com o exemplo 10/2/3 → 3/10/2 → 3/2/10 (ou o simulador de `docs/site/index.html`).

## 2. Especificação e arquitetura (1 min 30)
- **Mapa de módulos** (`CAPABILITY-MAP.md`): `rules`, `levels`, `game`, `profiles`, `progress`, `sync`,
  com dependências num só sentido.
- **C4:**
  - Contexto e Container **antes do código**, como pede o enunciado;
  - Componentes e Código no fim (`docs/architecture/C4.md`).
- **Decisões que viram slide:** offline-first, regras no cliente (a física não é determinística), um só
  arquivo de nível para o jogo e a API, perfil sem senha (o servidor guarda só o hash).
- **Uma spec por módulo** (`docs/specs/`), com critérios de sucesso testáveis.

## 3. Como usei as skills (2 min)
| Fase | Skill | Evidência (arquivo) |
|---|---|---|
| Ideia | `idea-refine` | `docs/ideas/troca-de-propriedades.md` (one-pager), `BANCO-DE-IDEIAS.md` |
| Módulos | `spec-driven-development` (fase 0) | `CAPABILITY-MAP.md` |
| Specs | `spec-driven-development` (fase 1) | `docs/specs/SPEC-*.md` (rules, levels, profiles, progress, sync, game) |
| Plano | `planning-and-task-breakdown` | `tasks/rules/`, `tasks/levels/` |
| Código | `test-driven-development` | testes escritos antes, com a fase vermelha registrada no diário |
| Revisão | `code-review-and-quality` | item 7 do diário: 5 eixos, teste de mutação, 1 bug corrigido |
| Segurança | `security-and-hardening` | seção "Security" das specs da API |
| Trabalho autônomo | rotina na nuvem + loop no PC | `tasks/autonomo.md`, commits feitos de madrugada |

- **Ponto honesto para mostrar:** na nuvem as skills não estavam disponíveis. A rotina seguiu o processo à
  mão e **disse isso no diário**, e por isso o trabalho voltou para o PC. Mostra que você conferiu o agente.
- **Pendência:** refazer a revisão de segurança (item 3) com a skill de verdade antes da apresentação.

## 4. Qualidade e testes (2 min)
| Módulo | Testes | Cobertura de linhas |
|---|---|---|
| `rules` | 106 | 98,5 % |
| `levels` | 125 | 96,7 % |
| (atualizar: `game`, API, `sync`) | | |

- **Build com 0 avisos:** os avisos viram erro.
- **Testes de arquitetura:** nenhuma biblioteca referencia o Godot.
- **Teste aleatório:** 1 000 passos, conferindo a regra de ouro depois de cada um.
- **Teste de design por sala:** cada sala tem as peças da lição dela.
- **Teste de mutação:** o código foi estragado de propósito em 4 lugares, e os testes pegaram os 4.
- **Bugs achados revisando o trabalho da IA:** é o melhor slide deste bloco.
  1. Um teste quebrava só com a cobertura ligada (o coverlet injeta classes) → corrigido.
  2. O mesmo teste quebrou de novo no `levels`, desta vez pelas classes que o compilador cria → corrigido.
  3. Um pedido de troca vazio contava uma jogada no ranking → corrigido com teste antes.
  4. Os exemplos de pulo da spec não batiam com o volume do jogador → spec corrigida.

## 5. Evidências do processo (1 min 30)
- `docs/processo/DIARIO.md`: cada decisão, dizendo **quem decidiu**: você ou o agente sozinho.
- `docs/processo/RELATORIO-rules.md` e `RELATORIO-levels.md`: a tabela critério → teste.
- `docs/processo/PERGUNTAS-PARA-O-USUARIO.md`: o que o agente **não** decidiu sozinho.
- O histórico de commits (≈ 90 até agora), com um commit por tarefa e a coautoria da IA.

## 6. Demo (2 min)
1. Sala 1.2: a mira branca dentro dos 8 m e vermelha fora; trocar de posição com a caixa do outro lado do buraco.
2. Sala 1.3: bola → botão → caixa; a porta abre.
3. Cair no buraco: a sala reinicia, e o tempo e as trocas continuam somando.
4. Fim do nível: o resultado vai para a API e aparece no ranking (se a API estiver pronta; senão, mostrar
   o save local e a fila do `sync`).
- **Reserva:** um vídeo de 1 minuto com os mesmos passos.

---

## Prints e vídeos que você precisa capturar
- [ ] O terminal do Claude Code durante uma skill (por exemplo, a pergunta antes da spec do `levels`).
- [ ] O `dotnet test` com todos os testes verdes e o resumo de cobertura.
- [ ] Um teste vermelho antes do código (fase vermelha do TDD).
- [ ] A página da rotina em claude.ai/code/routines e os commits feitos de madrugada no GitHub.
- [ ] Os diagramas C4 renderizados no GitHub (o GitHub desenha o Mermaid sozinho).
- [ ] O simulador de `docs/site/index.html`.
- [ ] O jogo: a mira branca e vermelha, uma troca de posição, a morte no buraco, o HUD com tempo e trocas.
- [ ] O vídeo de reserva da demo (1 minuto).
