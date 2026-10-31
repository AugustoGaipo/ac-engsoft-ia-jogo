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
- Instalados: Unity 6 (6000.6.7f1) via Unity Hub, Node.js. **.NET SDK não instalado.**

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
- Corte de escopo proposto (aguardando confirmação): 5 fases (posição 1–2, peso 3–4, arma 5);
  jogador↔objeto e momento como extras; energia cinética só na biblioteca/testes.

## Onde paramos (sessão 2)
Engine escolhida. Falta: instalar .NET SDK 8 e Godot 4 .NET; confirmar o corte de escopo;
seguir com a fase 2 da idea-refine (premissas/riscos) e a fase 3 (one-pager em `docs/ideas/`).

## Onde paramos (sessão 1)
Skill `agent-skills:idea-refine`, **fim da fase 1**. Próximo passo: o aluno escolhe ou combina
variações → fase 2 (agrupar em 2-3 direções, testar cada uma, levantar premissas) →
fase 3 (one-pager em `docs/ideas/`).
Pendência: mandar e-mail ao professor pedindo aprovação do domínio "jogo".
