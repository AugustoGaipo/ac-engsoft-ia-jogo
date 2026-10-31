# Contexto da sessão anterior (handoff)

Sessão de 2026-10-31, iniciada em `C:\Users\USUARIO`. Pasta do projeto: `C:\projetos\ac-engsoft-ia-jogo`.

## O trabalho
- Disciplina: Métodos e Aplicações de IA (Prof. Pedro Calais). Enunciado em `docs/enunciado/`.
- Atividade: aplicação de média/alta complexidade feita com agente de IA + um conjunto de agent skills.
- Entrega: slides + apresentação de ~10 min no último dia de aula.
- Critérios de complexidade: 3+ módulos, persistência em banco, regras de negócio não triviais,
  API/interface, autenticação/segurança, testes automatizados, documentação, modelos C4
  (Contexto e Container ANTES do código; Componentes e Código no final).
- Pesos: complexidade 15, especificação/arquitetura 15, uso das skills 20, qualidade/testes 20,
  evidências/relatório 20, demo/comunicação 10.
- "Jogo" não está na lista de domínios → **precisa de aprovação do professor**.

## Decisões até agora
- Conjunto de skills: **Addy Osmani Agent Skills** (`agent-skills:`), já instalado no Claude Code.
- Pasta do projeto criada com `docs/enunciado/` e `docs/evidencias/`; `git init` feito (branch `main`), sem commits ainda.

## Respostas do aluno (idea-refine, fase 1)
- Gênero: **jogo 3D de puzzle**
- Equipe/prazo: **sozinho, 1 semana**
- Stack: **Unity** (não domina, mas prefere)
- Sucesso: **nota + portfólio**

## Ambiente
- Instalados: Node.js, Unity 6 (não usado mais), gh (logado como AugustoGaipo).
- **.NET SDK 8.0.427** em `C:/Program Files/dotnet` (talvez precise pôr no PATH do Git Bash).
- **Godot 4.7.2 .NET**: `%LOCALAPPDATA%/Microsoft/WinGet/Packages/GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe/Godot_v4.7.2-stable_mono_win64/` (usar o `_console.exe` para rodar sem janela).

## Riscos apontados
- Sozinho + 1 semana + Unity sem domínio = alto risco.
- Unity sozinho não cobre persistência/auth/API/testes → precisa de backend separado.
- O agente escreve C#, mas não opera o Editor do Unity (cenas, prefabs) nem verifica visualmente.

## Variações propostas (aguardando reação do aluno)
1. **C# em todas as camadas** (recomendada): regras do puzzle em biblioteca C# pura testada com
   `dotnet test`; API ASP.NET Core + SQLite com login, progresso, ranking e validação
   antitrapaça da solução no servidor; Unity como cliente 3D fino.
2. **Puzzle diário** (estilo Wordle 3D): puzzle gerado por semente no servidor, ranking por jogadas.
3. **Inversão**: jogadores criam fases; o servidor roda um solver para provar que têm solução.
4. **Simplificação**: 3D no navegador (Three.js + TypeScript), sem Unity; mais fácil de verificar e publicar.
5. **Educativo**: programar um robô com comandos em uma grade 3D (domínio Educação).
6. **10x mais simples**: 5 fases fixas, login, progresso salvo, ranking por fase.

Recomendação: 1 + 6 (com um pouco da 2). Plano B se o prazo apertar: 4.

## Decisões da sessão 2 (2026-10-31)
- Ideia do jogo do aluno em `docs/CONTEXTOJOGO.md` (puzzle 3D em 1ª pessoa de **troca de propriedades**).
  Substitui as variações genéricas da fase 1.
- **Engine: Godot 4 com C#** (não mais Unity). Motivo: o aluno quer levar o jogo adiante, talvez para a Steam.
  Three.js foi considerado (melhor para o agente verificar), mas o Godot ganhou pelo visual e pelo futuro.
- Arquitetura proposta (direção B): biblioteca de regras de troca em **C# puro** fora da engine, testada
  com `dotnet test`; fases em JSON; API ASP.NET Core + SQLite (login, progresso, ranking por menos trocas,
  validação da legalidade das trocas); Godot como cliente.
- Na Steam, login/ranking/saves viriam do Steamworks → o backend serve à disciplina; o que se reaproveita
  é a biblioteca de regras e as fases.
- **Escopo definido:** 10 fases com 3 habilidades (posição, peso, energia cinética); ver DIARIO.md. Volume constante no peso.
  Momento adiado; mais 2 habilidades numéricas no futuro (arquitetura extensível).

## Onde paramos (sessão 2)
Engine escolhida. Falta: instalar .NET SDK 8 e Godot 4 .NET; confirmar o corte de escopo;
seguir com a fase 2 da idea-refine (premissas/riscos) e a fase 3 (one-pager em `docs/ideas/`).

## Onde paramos (sessão 1)
Skill `agent-skills:idea-refine`, **fim da fase 1**. Próximo passo: o aluno escolhe ou combina
variações → fase 2 (agrupar em 2-3 direções, testar cada uma, levantar premissas) →
fase 3 (one-pager em `docs/ideas/`).
Pendência resolvida (sessão 2): o professor já libera jogos que não sejam simples demais (ex.: cobrinha).

## Onde paramos (sessão 2, fim)
- Spec do `rules` aprovada; plano em `tasks/rules/plan.md` e `tasks/rules/todo.md` (cada módulo tem a sua pasta em `tasks/`); Tarefa 1 feita localmente.
- Tarefas 2–8 sendo feitas por uma **rotina na nuvem** (`trig_014m1a1SpcD9kaW9NajwNc1Z`,
  https://claude.ai/code/routines/trig_014m1a1SpcD9kaW9NajwNc1Z), a cada hora no minuto 24 UTC, Sonnet.
  Ela faz commit/push no GitHub e escreve `docs/processo/RELATORIO-rules.md` no fim.
- Ao voltar: `git pull`, revisar o diário e o relatório, e **desligar a rotina** quando acabar.

## Onde paramos (sessão 2, 2026-10-31 à noite)
- **Prazo: sexta, 2026-11-06.** Cronograma: dom → `levels` pronto (feito sábado) e specs da API; seg → API
  (`profiles` + `progress`) e spec do `game`; ter → `game` com o nível 1 no Godot; qua → níveis 2, 3 e 5,
  e `sync`; qui → C4 final, site da arquitetura, prints, slides e ensaio (sem código novo).
- **`rules` concluído** (rotina na nuvem, **pausada**). Relatório: `docs/processo/RELATORIO-rules.md`.
- **`levels` concluído** no PC: 125 testes, 96,7 % de cobertura, 4 níveis (1, 2, 3, 5) e 11 salas em
  `levels/`. Relatório: `docs/processo/RELATORIO-levels.md`.
- **Decisões do dia:**
  - progressão sala por sala em `docs/design/PROGRESSAO-NIVEIS.md` (aprovada), escopo de sexta no fim do arquivo;
  - níveis têm várias salas; a morte reinicia a sala e o tempo e as trocas continuam somando;
  - sem espaço → morte/estouro em todos os casos.
- **Pendências:**
  - confirmar a sala 1.4 (caixa travada);
  - revisão pequena da spec do `rules` (exemplos de pulo, barra de lançamento, pressão de pilha);
  - raio do ponto livre mais próximo.
- **Próximo passo:** specs do `profiles` e do `progress` (API ASP.NET Core + SQLite).
