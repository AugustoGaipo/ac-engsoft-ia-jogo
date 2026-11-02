# Trabalho autônomo (a partir de 2026-11-01 à noite)

Lista do loop autônomo: **uma rodada por hora**, cada uma com cerca de **30 minutos de trabalho** e 30 de descanso.
Cada rodada pega o **primeiro item não marcado** e o faz por completo. Só começa o próximo se ainda sobrar
tempo dos 30 minutos, e **nunca deixa um item pela metade** num commit. Ao terminar um item, marca `[x]`,
registra no `docs/processo/DIARIO.md` (em português, com as decisões tomadas sozinho), faz commit e push.
Não há horário para parar: o loop só para quando **todos os itens estiverem marcados**.

Regras:
- Nada que precise do usuário:
  - **não** perguntar nada;
  - **não** implementar código de módulo novo (as specs ficam como **rascunho, aguardando aprovação**);
  - **não** mexer nas salas dos níveis.
- Se mexer em código: `dotnet build Swapping.sln` com 0 avisos e `dotnet test Swapping.sln` todo verde antes do commit.
- Dúvida que só o usuário pode responder: anotar em `docs/processo/PERGUNTAS-PARA-O-USUARIO.md` e seguir em frente.
- Commit em português, terminando com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Itens (em ordem)
- [x] 1. **Rascunho da spec do `profiles`** (`docs/specs/SPEC-profiles.md`), com a skill
      `agent-skills:spec-driven-development`: API ASP.NET Core + SQLite (EF Core).
      - Perfil sem senha: nome e ícone, mais um código de acesso gerado pelo servidor, que guarda só o hash.
      - Bloqueio por tentativas, gerar código novo e limite de envios.
      - Seguir o formato das specs do `rules` e do `levels`, com critérios de sucesso testáveis.
      - Listar as suposições e as perguntas abertas.
- [x] 2. **Rascunho da spec do `progress`** (`docs/specs/SPEC-progress.md`).
      - Níveis liberados em ordem (lidos do `levels/catalog.json`) e melhor resultado por nível.
      - Ranking nos dois modos (tempo→trocas e trocas→tempo).
      - Regras de reinício: a morte e "reiniciar sala" continuam somando; "reiniciar nível" zera.
      - Espaço para o fantasma no futuro. Mesmo formato e suposições listadas.
- [x] 3. **Revisão de segurança das duas specs da API**, com a skill `agent-skills:security-and-hardening`.
      - Ameaças: força bruta no código, enumeração de nomes, envio de resultado falso ou em excesso,
        injeção de dados e CORS.
      - Acrescentar a cada spec uma seção "Security" com as proteções e um critério de sucesso para cada uma.
- [x] 4. **Revisão da spec do `rules`** (critério 12, exemplos de pulo).
      - Os valores 0,50 / 1,00 / 1,84 / 0,12 m só batem com densidade ~1 000 kg/m³.
      - Corrigir o texto da spec com os valores certos para o volume fixo do jogador (0,07 m³) e
        acrescentar um teste com a densidade real (7 kg → ~1,86 m), sem mudar a fórmula.
- [x] 5. **Rascunho da spec do `sync`** (`docs/specs/SPEC-sync.md`): biblioteca C# pura com o save local
      em JSON e a fila de envio para a API (reenvio seguro, porque o servidor guarda o melhor resultado).
- [x] 6. **Rascunho da spec do `game`, só o nível 1** (`docs/specs/SPEC-game.md`).
      - Godot 4.7 .NET, `CharacterBody3D` e `RigidBody3D`.
      - O que levar dos relatórios:
        - a troca de posição troca centros e sobe o corpo para o ponto livre mais próximo;
        - detecção contínua de colisão;
        - morte por queda e por zona de queda;
        - reinício de sala;
        - contorno por propriedade aceita;
        - números na tela, tempo e trocas desde a sala 1.1.
      - Tudo o que for gosto (cores, sensação do pulo) vai para "Perguntas abertas".
- [ ] 7. **Revisão de código do `rules` e do `levels`**, com a skill `agent-skills:code-review-and-quality`.
      - Corrigir só o que for claro e pequeno, sempre com teste.
      - Anotar o resto como achados no diário.
- [ ] 8. **C4 nível 3 (Componentes), parcial**, em `docs/architecture/C4.md`.
      - Componentes que já existem dentro do Game Client: `rules` e `levels`, com as classes principais e as setas.
      - Marcar como "parcial: `game`, `sync` e a API entram quando existirem".
- [ ] 9. **Site que explica a arquitetura** (só para o usuário).
      - Simples e compreensível, com analogias do dia a dia, ligando cada ideia do banco de ideias ao código.
      - Um arquivo HTML em `docs/site/`; não precisa ser bonito.
- [ ] 10. **Roteiro da apresentação** (`docs/apresentacao/ROTEIRO.md`): 10 minutos divididos pelos
      critérios e pesos do trabalho.
      - Tabela de evidências: cada skill usada e onde, testes e cobertura, bugs achados na revisão e as
        decisões do diário.
      - Lista dos prints e trechos de vídeo que o usuário precisa capturar.
