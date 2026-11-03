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
- **Simplificação:** a barreira 3 barra **todas** as variáveis, não uma por tipo.
- **Zonas:** além de paredes, blocos ou regiões inteiras com barreira. Pela regra de estar *dentro* da barreira, parede e zona viram a mesma coisa (um volume com filtro).
- **Energia:** só cinética (½·m·v²). A energia mecânica (movimento + altura) foi explicada pelo agente e descartada: cria casos impossíveis (receber menos energia do que a altura exige) e precisa de uma referência de altura.

---

## 2026-10-31 — C4 de Contexto e Container (antes do código)

- Criado [`docs/architecture/C4.md`](../architecture/C4.md) com os níveis 1 e 2 do C4, em Mermaid
  (o GitHub mostra os diagramas direto na página).
- **Contexto:** o jogador e o sistema Swapping; a Steam aparece tracejada, como possibilidade futura.
- **Containers:** cliente do jogo (Godot + C#, com as bibliotecas `rules`, `levels` e `sync` dentro),
  arquivos de fase (JSON), save local, API (ASP.NET Core) e banco (SQLite).
- **Decisões registradas no diagrama:** jogo offline primeiro; as regras rodam no cliente; o jogo e a
  API leem os mesmos arquivos de fase; segurança sem senha; bibliotecas não são containers (aparecem no
  nível 3, Componentes, no final).

---

## 2026-10-31 — Spec do módulo `rules`

**Skill usada:** `agent-skills:spec-driven-development` (fase 1: especificar)

- Escrita a [spec do `rules`](../specs/SPEC-rules.md): modelo de variáveis, as 3 propriedades com
  exemplos numéricos, a tabela de motivos para recusar uma troca, as barreiras, as contas de física,
  os pontos de extensão e 12 critérios de sucesso testáveis.
- Decisão de design do agente: distância, linha de visão e barreiras são **medidas pelo jogo**
  (raycasts) e passadas para a biblioteca, que só decide. Assim a biblioteca não depende do Godot e
  tudo pode ser testado com `dotnet test`.
- Aguardando a minha aprovação e a confirmação de 4 suposições.
- **3 meios de densidade:** ar, gás denso e água. Objetos menos densos que o meio sobem, inclusive no ar (ex.: o jogador flutua com o peso de um balão). Adicionado à spec do `rules` e ao banco de ideias (P1).
- **Respostas às suposições da spec:** o jogador boia (70 kg, 0,07 m³); restauração por barreira não
  conta como movimento; valores padrão mantidos.
- **Contorno no lugar de cor:** em vez de cores para as propriedades aceitas, um contorno no objeto
  que dá para segurar ou trocar.
- **3 estados por meio (ideia minha):** afunda, nada e boia, com faixas de densidade; segurar um
  objeto entra na conta. O agente explicou que na física real só a densidade exatamente igual é
  neutra, então a faixa é uma simplificação de jogo, e adotou as mesmas proporções em todos os meios.
- **Direção na restauração:** a direção viaja junto com a variável de energia. Se a caixa vai para a direita e recebe de volta uma variável que se movia para a esquerda, ela passa a ir para a esquerda. Uma regra só para troca e restauração: você se move do jeito que a sua variável de energia se movia.
- **Correção:** a regra das propriedades aceitas continua; a cor só sai do objeto e vai para o **contorno** (branco = aceita tudo).
- **Ação e reação:** lançar um objeto sem estar apoiado no chão empurra para o lado contrário (no ar desliza como no espaço; na água para logo). O momento entra no jogo pela física. Velocidade de nado por meio: o agente explicou que, pela física, ela seria parecida nos meios e o que muda é a resistência; decisão pendente.
- **Soltar ≠ lançar (correção minha, confirmada pelo agente):** só soltar não dá reação; a reação vem só da velocidade que o lançamento adiciona, não da gravidade. Viram duas ações no jogo.
- **Pulo:** propus pular mais alto quando leve, "como na Lua". O agente explicou que a gravidade acelera
  todos igual (Galileu): leve pula mais alto porque as pernas empurram menos massa, mas cai na
  velocidade normal; a queda só fica lunar perto da densidade do meio. Decisão: pulo fisicamente
  correto, com recado para comparar depois com um pulo ajustado de jogo.
- **Nado:** escolhida a opção física (mesma velocidade máxima; muda a resistência de cada meio).
- **Extintor (ideia minha):** item que dá empuxo contínuo, como o astronauta com extintor. Mesma física
  do lançamento, só que contínua; carga limitada.
- **Categoria quântica (ideia minha):** objetos ligados para sempre à variável original numa
  propriedade. Ex.: o gás do extintor tira peso de quem está com o peso dele; um objeto quântico na
  energia fica imóvel e repassa os empurrões para quem está com a energia dele. O modelo de variáveis
  aguenta isso sem gambiarra; entrou como ponto de extensão na spec.
- **Categoria mimética (em análise, ideia minha):** trocar posição com um objeto mimético faz o outro virar uma cópia da forma dele (só a forma). Com o jogador, a troca falha por um motivo misterioso. O agente sugeriu separá-la do quântico, porque tem outra regra.
- **Quântico na posição (ideia minha):** teleporte por distância. A troca funciona, mas se os dois se
  afastarem demais a ligação arrebenta e eles trocam de lugar de novo. Preferida às três opções do
  agente (joystick, movimento ligado, espelho).
- **Princípio do mundo (decisão minha):** o mundo é sempre o mesmo; só mudam valores matemáticos de
  forma fisicamente plausível, e as habilidades evoluem por sala. O agente apontou que a categoria
  mimética (mudar a forma) conflita com esse princípio.

---

## 2026-10-31 — Spec do `rules` aprovada; implementação autônoma

- Aprovei a spec do `rules` e autorizei o agente a implementar o módulo enquanto eu estava fora
  (plano, código com testes primeiro, commits e push).
- Combinado: decisões que precisariam de mim são tomadas pelo caminho mais simples e reversível e
  anotadas aqui para eu revisar.

### Tarefa 1 do `rules` — estrutura do projeto ✅
- Criados `Swapping.sln`, a biblioteca `src/Swapping.Rules` (.NET 8, sem dependências) e os testes
  `tests/Swapping.Rules.Tests` (xUnit do modelo padrão).
- `Directory.Build.props` liga "avisos como erros", para o critério 1 da spec (zero avisos) ser garantido
  pela própria compilação.
- `dotnet build` sem avisos; `dotnet test`: 1 teste de fumaça aprovado.
- Escolhi agendar o resto da implementação na nuvem (o agente da nuvem trabalha a partir do GitHub).

### Tarefa 2 do `rules` — fórmulas de física e meios ✅
- Criados `Vec3`, `Formulas` (energia cinética, velocidade a partir da energia, densidade, empuxo,
  estado de flutuação, pressão, recuo, pulo) e `Medium` (ar, gás denso, água com densidade e arrasto).
- Testes: 27/27 aprovados (inclui os critérios 3, 10, 11 e 12 da spec, o recuo e as bordas 0,1× e 1,1×).
  `dotnet build` sem avisos.
- **Decisões tomadas sozinho:**
  - O ambiente da nuvem bloqueou o instalador oficial do .NET (403); instalei o SDK 8 por `apt-get install dotnet-sdk-8.0`.
  - Exemplos de pulo da spec (0,50 / 1,00 / 1,84 / 0,12 m) só batem com ±0,01 se a densidade for ~1000 kg/m³
    (g_eff ≈ g). Com 7 kg e 0,07 m³ (ρ = 100) a fórmula daria 1,86 m. Os testes usam `density: 1000`;
    `JumpHeight` recebe a densidade como parâmetro. **Vale revisar a spec.**
  - Corpo não mais denso que o meio (ρ ≤ ρ_fluido): `JumpHeight` devolve +∞ (g_eff ≤ 0).
  - Bordas das faixas de flutuação usam tolerância relativa de 1e-9 para evitar erro de ponto flutuante.
  - Arrasto (1/s) padrão: ar 0,05, gás denso 0,1, água 3,0 — valores provisórios, ajustáveis; a velocidade
    máxima de nado (3 m/s) está em `Formulas.DefaultSwimSpeed`.
  - `Vec3` é um `readonly record struct`; `RecoilSpeed` recebe `onGround` e devolve 0 no chão.

### Tarefa 3 do `rules` — variáveis, containers e o invariante do mundo ✅
- Criados `IPropertyKind` + `PropertyKind` (posição, massa, energia cinética), `Variable` (id
  "dono.propriedade", tipo, dono, valor), `Container` (id, jogador/objeto, volume, área de contato,
  travado, propriedades aceitas) e `World` (registra containers, cria uma variável própria por
  propriedade, sabe quem usa qual, `ExchangeVariables` e `CheckInvariant`).
- Testes escritos antes do código (`WorldTests`): 36/36 aprovados (9 novos). `dotnet build` sem avisos.
- **Decisões tomadas sozinho:**
  - Ids de container são `string` (ex.: "player", "A"); o id da variável é "dono.propriedade"
    (`A.mass`, `A.energy`).
  - O valor da variável é um `VariableValue(Scalar, Vector)`: posição usa o vetor; massa usa o
    escalar (kg); energia usa o escalar (J) e o vetor como direção unitária do movimento.
  - `World.Add(container, posição, massa, velocidade)` calcula a energia inicial (½·m·v²) e a direção
    a partir da velocidade.
  - `AcceptedProperties = null` significa "aceita todas"; `Container` é um `record` imutável.
  - Id desconhecido lança `KeyNotFoundException`; id repetido lança `ArgumentException` (erros de
    programação, como manda a spec).
  - `PropertyKind.Position/Mass/KineticEnergy` são instâncias `static readonly` imutáveis (não é estado
    mutável estático).

---

## 2026-10-31 — Skills do Claude Code Game Studios

- A meu pedido, o agente instalou 8 skills do kit [Claude Code Game Studios](https://github.com/Donchitos/Claude-Code-Game-Studios)
  (licença MIT, commit `05ad7c8`): `/brainstorm`, `/quick-design`, `/design-review`, `/consistency-check`,
  `/playtest-report`, `/balance-check`, `/prototype` e `/scope-check`.
- Escolhi a opção "só algumas skills": **sem** os hooks automáticos, **sem** o `settings.json` e **sem** o `CLAUDE.md`
  do kit, para não mexer na rotina do `rules` nem no processo atual (spec, `tasks/`, este diário).
- Junto vieram só os arquivos de apoio que as skills usam: `hooks/yaml-helper.sh` (lê a configuração; sem o
  `project.yaml` do kit usa os padrões: revisão "solo", modo colaborativo), 3 scripts, documentos de apoio,
  4 modelos e 6 agentes que as skills chamam (diretores criativo, técnico e de arte, narrativo, designer de
  sistemas e de economia). Créditos em `.claude/THIRD-PARTY-NOTICES.md`.
- **Observação do agente:** as skills salvam os documentos nas pastas do kit (ex.: `design/gdd/`), não em
  `docs/specs/`. Dá para mover depois, se preferir.

### Tarefa 4 do `rules` — trocar posição, massa e energia ✅
- Criados `SwapRequest`, `IOperation` + `SwapOperation`, `SwapResult` (por enquanto só "aplicada"; a
  rejeição entra na Tarefa 5) e `BodyUpdate` (id, posição, massa, velocidade). No `World`:
  `SyncFromPhysics(id, posição, velocidade)`, `BodyUpdateFor(id)` e `BodyUpdatesFor(a, b)`.
- Testes escritos antes do código (`SwapTests`): 45/45 aprovados (9 novos), cobrindo o critério 2
  (primeira metade), 3 (por troca de massa), 4, 5 e o receptor de 0 J que para. `dotnet build` sem avisos.
- **Decisões tomadas sozinho:**
  - Uma regra única calcula o corpo a partir das variáveis em uso: velocidade = direção da variável de
    energia × √(2·E ÷ m). Daí saem as três trocas (posição mantém velocidades; massa mantém a energia e
    recalcula a velocidade; energia faz o receptor andar na direção do doador).
  - `SyncFromPhysics` grava a energia com a massa que o container usa no momento; parado (v = 0), a
    variável guarda a última direção (o plano já previa isso).
  - Os testes ficaram num único `SwapTests.cs` (como em `todo.md`), não em três classes separadas como
    sugere a estrutura da spec.
  - O SDK .NET 8 foi instalado de novo por `apt-get install dotnet-sdk-8.0` (o instalador oficial não
    gerou nada).

### Tarefa 5 do `rules` — legalidade da troca e tempo de recarga ✅
- Criados `RejectReason` (os 11 motivos da tabela), `SwapContext` (distância, linha de visão, barreira
  de variáveis entre eles, cada um dentro de uma barreira — tudo medido pelo jogo), `LevelRules`
  (propriedades liberadas, alcance por propriedade, recarga, chaves objeto↔objeto e troca simultânea) e
  `Cooldown`. `SwapOperation` agora consulta `world.Rules.Check(...)` e devolve
  `SwapResult.Rejected(motivo)` sem mexer em nada; `World` recebe as regras no construtor.
- Testes escritos antes do código (`LegalityTests`): 65/65 aprovados (20 novos) — um por motivo, a borda
  "distância = alcance é permitida", o critério 9 (recarga de 2 s: t = 1,5 recusada, t = 2,0 aplicada) e
  os padrões (8 m, 1 s). `dotnet build` sem avisos. Checkpoint "Core" concluído.
- **Decisões tomadas sozinho:**
  - Com várias falhas ao mesmo tempo, vale o primeiro motivo na ordem da tabela da spec.
  - Padrão do `LevelRules`: as três propriedades liberadas; objeto↔objeto e troca simultânea desligados.
  - O `SwapContext` vai dentro do `SwapRequest` (propriedade `Context`, padrão "tudo livre": distância 0,
    com visão, sem barreira), para manter a assinatura `Apply(world, request, now)` da spec.
  - A recarga fica no `World` (estado da partida) e só começa com troca **aplicada**; troca recusada não
    reinicia a recarga. Comparação de tempo com tolerância de 1e-9 s.
  - `PropertyNotAccepted` vale se **qualquer** dos dois não aceitar a propriedade; `Locked` também
    vale para qualquer um dos dois.
  - Os testes de `SwapTests` (Tarefa 4) passaram a usar regras permissivas (objeto↔objeto, simultânea
    e recarga 0), porque testam o efeito da troca e não a legalidade. Nenhuma asserção foi alterada.

---

## 2026-10-31 — Barra de lançamento e elasticidade

- **Barra de carregamento para lançar (ideia e decisões minhas):** segurar enche a barra, soltar lança.
  Escolhi a opção do agente em que a carga dá **energia** (como no pulo), v = √(2·E ÷ m_objeto) com
  limite de velocidade dos braços, em vez de velocidade fixa. Crescimento **linear**. Um **toque rápido
  conta como soltar** (sem recuo). Valores (energia máxima, limite, tempo de carga, carga mínima)
  decididos na hora/no playtest.
- **Elasticidade (ideia minha):** os objetos devem ter um grau de elasticidade para não perderem quase
  toda a energia ao bater na parede. O agente explicou que isso é o "quique" do material na física do
  Godot (ao quicar sobra e² da energia), que o `rules` não simula batidas e só lê a energia que a física
  devolve. Fica como atributo fixo por objeto; trocar a elasticidade continua sendo ideia futura.
- O agente não mudou a spec do `rules` nem o código: as duas ideias entram quando a spec do `game` (ou
  uma revisão da spec do `rules`, para a fórmula da carga) for feita.
- **Troca de elasticidade (decisão minha):** entrou como opção na rota de prioridade, no fim da P2
  (se não sobrar tempo, passa para a P3). A elasticidade fixa de cada objeto continua valendo antes disso.

---

## 2026-10-31 — Colisão e empilhamento

- **Colisão sólida (deixei explícito):** jogador e objetos colidem entre si, então dá para empilhar
  várias caixas, subir nelas e apoiar coisas. Fica a cargo da física do Godot (spec do `game`).
- Perguntas abertas levantadas pelo agente: a pressão sobre uma peça quebradiça soma o peso da pilha
  inteira? E o que acontece numa troca de posição quando o objeto maior não cabe no lugar do menor?

### Pilha e troca sem espaço (decisões minhas)
- **Pressão de pilha:** o vidro sente o peso da pilha inteira (3 caixas de 10 kg → 30 kg). Precisa de
  uma pequena revisão da spec do `rules`, junto com a fórmula da barra de lançamento.
- **Troca de posição sem espaço:** regra geral (a): o jogo mede se cabe; se não couber, a troca é
  recusada ("sem espaço"). O agente explicou que empurrar à força (b) não dá para garantir 100 %.
- **Casos forçados** (ex.: o objeto quântico de posição que volta sozinho quando a ligação arrebenta,
  que não pode ser recusado): ideia minha de "destruir e reconstruir" o objeto num ponto de
  renascimento. Minha dúvida: como saber que bugou sem destruir por engano objetos normais? Em discussão.
- **Intangível no teleporte forçado (ideia minha):** se o destino estiver ocupado, o objeto aparece
  intangível até o espaço liberar, em vez de ser destruído. Proposta do agente, em discussão: intangível
  **parado no lugar** (sem gravidade, semitransparente, não pode ser trocado); se o que ocupa é parede
  ou chão (nunca libera) ou se passar um tempo limite, vai para o ponto de renascimento.
- **Teleporte forçado com destino ocupado = falha de teleporte (decisão minha).** O sentido exato da
  falha ainda será confirmado.
- **Atravessar paredes em alta velocidade (preocupação minha):** o agente explicou que o problema vem da
  velocidade por passo de física (ex.: 315 J numa bola de 0,05 kg → ~112 m/s, ~1,9 m por passo a 60 Hz),
  e que a massa pesa mais na estabilidade das pilhas. Proteções sugeridas: detecção contínua de colisão
  (CCD) no Godot, espessura mínima de parede checada pelo validador do `levels`, zona de fora do mapa como
  última rede e, talvez, um limite de velocidade (mudaria a física; depende de mim).
- **Ponto livre mais próximo (ideia minha):** se o destino estiver ocupado (ex.: bola dentro de entulho),
  em vez de recusar, o objeto vai para o ponto mais próximo onde cabe. Condições sugeridas pelo agente:
  raio máximo de busca, ponto visível a partir do destino original (não atravessa parede), sem cruzar
  barreira nem sair do mapa, ordem de busca fixa (mesma situação → mesmo resultado). Sem ponto válido →
  falha. Em discussão: vale só para teleporte forçado ou também para trocas normais? Qual o raio?

### Tarefa 6 do `rules` — barreiras e restauração ✅
- Criados `BarrierFilter` (bloqueia objetos, bloqueia jogador, bloqueia variáveis; combináveis com
  `Combine`; consultas `BlocksPassageOf`, `RestoresOnEntry`, `BlocksSwaps`; prontos `ExitGrid` e
  `BlocksVariablesOnly`) e `Restoration` (`OnEnter`, `RestoreAll`, `Restore` por propriedade). No
  `World`: `OwnVariable(id, propriedade)` — a variável com que o container nasceu.
- Testes (`RestorationTests`): 76/76 aprovados (11 novos), cobrindo o critério 2 (segunda metade:
  A entra → jogador 3, A 2, B 10), o 6 (restauração de posição: a caixa continua andando, o outro
  fica parado), o 7 (restauração de energia inverte a direção), "restaurar quem já usa as próprias
  variáveis não muda nada" e "a grade de saída bloqueia objetos e variáveis e deixa o jogador passar".
  `dotnet build` sem avisos.
- **Decisões tomadas sozinho:**
  - `Restoration` é uma classe estática sem estado (não um `IOperation`): restauração não é troca,
    então ignora legalidade e recarga e não reinicia a recarga (há teste). A contagem de jogadas fica
    para a Tarefa 7.
  - `RestoreAll` devolve as atualizações de corpo do container e depois de cada antigo dono afetado, sem
    repetir, na ordem posição → massa → energia. Se nada mudar, devolve só o próprio container;
    `OnEnter` numa barreira que não bloqueia variáveis devolve lista vazia.
  - `BarrierFilter` é um `record struct` imutável (os prontos `ExitGrid`/`BlocksVariablesOnly` são
    `static readonly` imutáveis, não estado mutável). O filtro por propriedade fica para depois, como a
    spec pede ("all" por enquanto).
  - Soltar um objeto segurado ao cruzar a barreira 1 é detectado e feito pelo jogo; o `rules` só
    responde `BlocksPassageOf`.
  - O SDK .NET 8 foi instalado com `apt-get update && apt-get install dotnet-sdk-8.0` (o instalador
    oficial de novo não gerou nada).
- **Ponto livre mais próximo vale para os dois (decisão minha):** trocas de posição normais e teleporte
  quântico. O quântico procura numa distância maior; sem espaço nem longe, não é permitido.
- **Morte e renascimento por falta de espaço (ideia minha):** em vez de recusar, a troca acontece; se o
  objeto não tiver espaço, ele "morre" com efeito de estouro/explosão e fumaça (só som e imagem, sem
  fragmentos com colisão) e renasce como num portal, **com os atributos de antes da morte**. Se for o
  jogador, ele morre por sufocamento/sobreposição e a fase reinicia. **O jogo tem morte** (contei agora).
- **Resistência a dano pela densidade do personagem (ideia minha):** a detalhar.
- Em discussão com o agente: a morte substitui o "não permitir" do quântico? Do que se morre? Vida ou
  morte na hora? Reiniciar conta no ranking?
- **Morte em todos os casos (decisão minha):** sem espaço nem no ponto mais próximo, vale estouro +
  renascimento (objeto) ou morte (jogador) tanto na troca normal quanto no quântico. Nenhuma troca é
  recusada por falta de espaço; o "não permitir" do quântico sai.
- **Estouro do objeto (decisão minha):** a troca de posição acontece antes do estouro; os outros
  atributos continuam os de antes da morte (em troca simultânea, só a posição é trocada). Interpretação
  do agente a confirmar.
- **Morte do jogador (decisão minha):** volta ao ponto de renascimento da fase. Para começar: queda
  (altura/colisão), sufocamento/sobreposição e afogamento. Explosão, implosão e outras foram para a P2
  do banco de ideias.
- **Correções e confirmações (minhas):**
  - Estouro: numa troca simultânea **as duas propriedades trocam** (não só a posição); o outro lado
    recebe antes do estouro. Se for preciso para a troca acontecer a tempo, uma intangibilidade bem
    rápida é permitida (só se necessário).
  - Objeto que estoura renasce **parado** (energia cinética zerada), no ponto de renascimento dele.
  - Jogador morre → a fase reinicia e **tudo volta ao normal**; ele renasce no ponto de renascimento dele.
  - Morte por falta de espaço é só sufocamento/sobreposição (sem esmagamento por enquanto).
  - Afogamento: está nadando e não sai da água antes da **barra de ar** acabar.
  - Queda mata: sim.
  - **Densidade e dano:** mais denso pode levar **mais** dano de queda, mas tem **mais resistência** aos
    outros danos (tiro, explosão — tudo que não mata na hora). Ou seja, existe vida para os danos que
    não matam na hora.

### Tarefa 7 do `rules` — registro de trocas, contagem de jogadas e teste aleatório do invariante ✅
- Criado `SwapLog` (em `Operations/SwapLog.cs`) com `SwapLogEntry` (primeiro, segundo, propriedades,
  instante em s). O `World` ganhou `Log` e `MoveCount`; o `SwapOperation` registra cada troca aplicada
  logo depois de registrar a recarga.
- Testes (`InvariantTests`): 83/83 aprovados (7 novos): mundo novo com 0 jogadas, troca aplicada
  registrada e contando 1, troca simultânea contando 1, troca recusada não registrada, restaurações
  (`OnEnter`, `RestoreAll`, `Restore`) não mudam a contagem, ordem do registro e o critério 13:
  1 000 passos aleatórios (semente fixa 20261031; 6 containers; 70 % trocas com 1 a 3 propriedades,
  30 % restaurações totais ou de uma propriedade) com o invariante checado depois de cada passo e a
  contagem final igual ao número de trocas aplicadas. `dotnet build` sem avisos.
- **Decisões tomadas sozinho:**
  - `MoveCount` = número de entradas do registro (cada troca aplicada vale 1, simultânea também);
    não há contador separado, então os dois nunca divergem.
  - A entrada copia a lista de propriedades da requisição (o registro não muda se a lista original mudar).
  - O teste aleatório usa `System.Random` com semente fixa **só no projeto de testes**; a biblioteca
    continua sem aleatoriedade. Usa regras permissivas (objeto↔objeto, simultânea, recarga 0) para
    exercitar todas as combinações; a única recusa possível é `SameContainer`, e o teste confere isso.
  - O SDK .NET 8 foi instalado de novo com `apt-get install dotnet-sdk-8.0` (o instalador oficial
    foi bloqueado pelo proxy, 403).

### Tarefa 8 do `rules` — pontos de extensão, cobertura e documentação ✅
- Criados os pontos de extensão que faltavam, cada um com uma única implementação:
  `ISwapDurationPolicy` → `PermanentSwapDuration` (toda troca é permanente) e `ICooldownModifier` →
  `NoCooldownModifier` (mantém a recarga da fase). `LevelRules` ganhou `SwapDuration`,
  `CooldownModifier`, `EffectiveCooldownSeconds` (usado na checagem de recarga) e a flag
  `EnableEntangledProperties` (desligada, ninguém lê ainda). `IPropertyKind` e `IOperation` já existiam;
  as flags objeto↔objeto e troca simultânea já existiam e são usadas na legalidade.
- `ArchitectureTests`: o assembly se chama `Swapping.Rules`, não referencia nenhum assembly Godot,
  o `.csproj` não tem `PackageReference`/`ProjectReference`/`Reference` nem a palavra "Godot"
  (critério 14), não há campo estático mutável no assembly, cada ponto de extensão tem exatamente a
  sua implementação, e os padrões (permanente, sem modificador, flags desligadas).
- Testes: 94/94 aprovados (11 novos). `dotnet build` com 0 avisos (critério 1; `TreatWarningsAsErrors`).
- **Cobertura de linhas do `Swapping.Rules`: 98,3 %** (ramos: 89,8 %) — critério 15 (≥ 90 %) atendido.
  Linhas não cobertas: 12–13 de `Container.cs` (código gerado pelo `record`), `PropertyKind.ToString`
  e `Variable.ToString`.
- README: seção "Compilar e testar" com os comandos de build, teste, cobertura e formatação.
- Todos os 15 critérios de sucesso têm teste aprovado (o 1 é garantido pelo build e o 15 pela medição
  acima). O relatório completo (`docs/processo/RELATORIO-rules.md`) será escrito pela próxima execução
  da rotina, que encontra todas as tarefas marcadas.
- **Decisões tomadas sozinho:**
  - `ISwapDurationPolicy.RevertAfterSeconds(request)` devolve `null` = permanente (o jeito mais simples
    de representar "dura para sempre"); `ICooldownModifier.Modify(segundos)` recebe e devolve segundos.
  - A recarga passou a usar `EffectiveCooldownSeconds` (modificador identidade → mesmo comportamento;
    os testes de recarga continuam passando).
  - A flag de propriedades entrelaçadas ficou em `LevelRules` (por fase), não em `Container`; dá para
    mover depois sem quebrar nada, porque ninguém usa.
  - Teste extra (não pedido): nenhum campo estático mutável no assembly, para fiscalizar a fronteira
    "Never: static mutable state" da spec.
  - `TestResults/` entrou no `.gitignore` (saída da cobertura).
  - O README do repositório está em português, então a seção nova também.
- **Vida, resistência, tiro e explosão ficam de fora (decisão minha):** só como ideia a pensar, em
  "Ideias pensadas", fora da rota de prioridade. A linha de "mais causas de morte" saiu da P2.
- **Barra de ar (decisão minha):** 15 s para começar, enche de novo ao sair da água; ajustar jogando.

### Revisão local do `rules` (sessão no PC)
- Baixei tudo o que a rotina fez e rodei de novo: build com 0 avisos e 94/94 testes aprovados.
- **Bug achado na revisão:** com a cobertura ligada (`--collect:"XPlat Code Coverage"`), o teste
  `RulesAssembly_HasNoStaticMutableFields` falhava. O coverlet injeta no assembly classes de contagem
  (`Coverlet.Core.Instrumentation.Tracker...`) com campos estáticos, e o teste as confundia com estado
  do jogo. Corrigido no teste, que agora ignora o namespace `Coverlet`; o código do `rules` não mudou.
  Agora são 94/94 com e sem cobertura, e a cobertura é de 98,3 % das linhas.
- Lição para a apresentação: a medição de cobertura que a rotina reportou passava, mas um teste quebrava
  justamente durante essa medição. Por isso vale revisar o trabalho autônomo antes de confiar nele.

## 2026-10-31 — Morte e ranking
- **Morrer não zera o tempo (decisão minha):** quando o jogador morre e a fase reinicia, o relógio da fase
  continua correndo no ranking. Entra na spec do `progress` (e na do `game`, que mede o tempo).
- **Níveis têm várias salas (decisão minha):** o ranking mede o nível inteiro, com o tempo e as trocas de
  todas as salas somados. A morte reinicia só a sala, e o tempo e as trocas continuam somando. O menu tem
  "reiniciar sala" (igual à morte) e "reiniciar nível" (tempo e trocas voltam a zero, tentativa nova).
- **Observação do agente:** o `MoveCount` do `World` (no `rules`) conta as trocas de um mundo. Se a sala
  reiniciar com um mundo novo, quem guarda a soma do nível é o `game`/`progress`, e não o `rules`.
  Isso entra na spec desses módulos, sem mudar o código do `rules`.

## 2026-10-31 — Progressão dos níveis
- Pedi uma divisão de quais mecânicas entram em cada nível. O agente propôs a curva sala por sala em
  `docs/design/PROGRESSAO-NIVEIS.md`: cada sala ensina uma coisa nova e a última de cada nível junta tudo.
  Os níveis garantidos no corte (1, 3, 5, 10) ensinam sozinhos o essencial de cada propriedade.
- **Tempo e trocas na tela desde o começo (decisão minha):** o cronômetro e o contador aparecem já na
  sala 1.1, não só na 1.3.
- **Escopo de sexta (decisão minha):** jogável até a energia cinética, com barreiras, buracos e paredes.
  O agente avisou que os níveis 1 a 6 inteiros (~27 salas) não cabem em 6 dias e propôs uma sala curta
  por ideia: nível 1 completo, barreiras 1/2/3 (2.1–2.3), peso (3.1–3.2) e energia (5.1–5.2), num total
  de 11 salas. Cair num buraco é morte por queda. A ordem de corte e os extras ficaram registrados em
  `docs/design/PROGRESSAO-NIVEIS.md`.

## 2026-10-31 — Módulo `rules` concluído
- A rotina foi pausada a meu pedido antes de escrever o relatório, então o agente escreveu no PC o
  `docs/processo/RELATORIO-rules.md`, com cada tarefa, a tabela critério → teste (15 de 15), as decisões
  tomadas sem mim, os problemas e o que está pendente.
- Na revisão apareceu uma divergência na spec: os exemplos de pulo só batem com densidade ~1 000 kg/m³.
  Com o volume fixo do jogador, 7 kg pula 1,86 m, e não 1,84 m. Isso entra na revisão da spec.
- Progressão dos níveis aprovada.

## 2026-10-31 — Spec do módulo `levels` (rascunho)
- Com a skill `agent-skills:spec-driven-development`. Antes de escrever, o agente perguntou duas coisas:
  - **O cenário fica no editor do Godot (decisão minha).** O JSON guarda só o que é do jogo: regras,
    salas, objetos, botões, portas, barreiras e zonas de queda (buracos). Consequência: o validador não
    enxerga as paredes, então a checagem da espessura de parede vai para o `game`.
  - **Os 4 níveis de sexta (1, 2, 3 e 5, 11 salas) entram já no `levels` (decisão minha).**
- O rascunho está em `docs/specs/SPEC-levels.md`: formato, 20 códigos de erro do validador, conversão
  para o `rules` (`BuildWorld` por sala, que também serve para reiniciar a sala), um teste de design por
  sala e 14 critérios de sucesso. Seis suposições ficaram listadas para eu confirmar.
- **Spec do `levels` aprovada (decisão minha)**, com as 6 suposições: uma cena do Godot por nível, uma grade de saída
  no fim de cada sala, buracos como zonas de queda no JSON, porta que abre com todos os botões apertados,
  densidade escrita entre 1 e 22 600 kg/m³ e bola com área de contato pequena.
- **Plano do `levels`** feito com a skill `agent-skills:planning-and-task-breakdown`: 8 tarefas em
  `tasks/levels/`. Primeiro vêm o modelo e o parser, depois o validador em duas partes, depois a ligação
  com o `rules`, os níveis 1 e 2, os níveis 3 e 5, e por último a arquitetura, a cobertura e a documentação.
- **Organização das tarefas (o agente decidiu, a meu pedido):** cada módulo tem a sua pasta em `tasks/`.
  O plano do `rules`, já concluído, foi movido (não apagado) para `tasks/rules/`.
- Lição do `rules` levada para o plano: a cobertura tem que ser medida **com todos os testes passando na
  mesma rodada**, e os testes de reflexão ignoram as classes do coverlet desde o começo.

### Tarefa 1 do `levels` — projetos e modelo ✅
- Feita no PC, com TDD: primeiro o teste vermelho (`ModelTests`, sem compilar), depois o modelo.
- Criados `Swapping.Levels` (só referencia o `Swapping.Rules`) e `Swapping.Levels.Tests`, os dois na solução.
  Modelo: `Box` (as faces que se encostam não contam como sobreposição), `ObjectDef` (volume, área de
  contato e densidade calculados a partir do tamanho), `ButtonDef`, `DoorDef`, `BarrierDef` (com o preset
  `exitGrid`), `KillZoneDef`, `SpawnDef` (área do jogador de 0,6 × 1,8 × 0,6 m), `RoomDef`, `LevelFile`,
  `LevelError` e os 21 códigos de erro da spec.
- Testes: 12/12 novos e 94/94 do `rules`; build com 0 avisos.
- **Decisões tomadas sozinho:**
  - A posição do **objeto** é o centro dele. A do **botão** é o centro da face de baixo, porque é uma
    placa no chão. O exemplo da spec já seguia isso: caixa em y = 0,5 com 1 m de altura, botão em y = 0.
  - A massa e o volume do jogador (70 kg, 0,07 m³) viraram constantes em `LevelFile`, já que não vão no arquivo.

### Tarefa 2 do `levels` — leitor do JSON ✅
- Primeiro os testes (`ParserTests` + `Fixtures` com o exemplo da spec como nível-base válido), depois o
  `LevelParser` com o `JsonDocument` do .NET (sem pacote NuGet).
- O leitor confere só o **formato**: campos obrigatórios, tipos, vetores de 3 números e palavras conhecidas
  (`box`/`ball`, `any`/`objects`, `exitGrid`). Cada problema vira um erro `InvalidJson` com o caminho
  (ex.: `rooms[0].spawn.position`). Nenhuma exceção escapa e todos os problemas aparecem de uma vez.
- Testes: 27/27 do `levels` (15 novos), 94/94 do `rules`. Build com 0 avisos. Checkpoint "Foundation" concluído.
- **Decisões tomadas sozinho:**
  - Defini os campos opcionais e os padrões (listas vazias, `accepts` nulo, `locked` falso, velocidade
    zero, `bounce` 0) e os pontos de referência das posições, e registrei tudo na spec.
  - O arquivo aceita comentários `//` e vírgula sobrando no fim, porque os níveis são escritos à mão.

### Tarefa 3 do `levels` — validador, parte 1 (valores) ✅
- Primeiro os testes (`ValidatorValueTests`): cada um parte do nível-base válido, quebra um campo e
  exige **só** o erro esperado, no caminho certo. Depois o `LevelValidator`.
- Códigos cobertos: `UnsupportedSchema`, `BadId`, `DuplicateId`, `NumberOutOfRange`, `UnknownProperty`,
  `NotPositive`, `OutOfRange01`, `ImplausibleDensity`, `EmptyBox`, `NotAccepted` e `EmptyBarrier`.
  Inclui o critério 5 (ids de sala fora de ordem; id repetido entre salas) e o 6 (densidade 300 vale;
  0,001 kg e 30 000 kg num cubo de 1 m são implausíveis).
- Testes: 63/63 do `levels` (36 novos), 94/94 do `rules`. Build com 0 avisos.
- **Decisões tomadas sozinho:**
  - O id `player` é reservado (o `World` do `rules` usa esse id para o jogador). Uma peça com esse nome
    dá `DuplicateId`.
  - Uma lista `accepts` vazia dá `NotAccepted` (aceitaria nada; para isso existe `locked`).
  - Massa ou tamanho inválido não gera também o erro de densidade, para não repetir o mesmo problema.
  - Recarga 0 s é permitida, porque os testes do `rules` usam esse valor.

### Tarefa 4 do `levels` — validador, parte 2 (espaço e ligações) ✅
- Primeiro os testes (`ValidatorSpatialTests`, 25 novos: vermelhos com 19 falhas), depois as checagens.
- Códigos: `OutsideRoom`, `RoomsOverlap`, `UnknownReference`, `DoorWithoutButton`, `UnusedButton`,
  `MissingExitGrid` (critério 7), `Overlap` e `SpawnUnsafe`. Um teste confere que todo código de erro
  aparece em algum teste (critério 3). O `CatalogOrder` fica de fora até a Tarefa 6.
- Testes: 88/88 do `levels`, 94/94 do `rules`. Build com 0 avisos.
- **Decisões tomadas sozinho:**
  - "Saída coberta pela grade" = uma barreira que é **exatamente** grade de saída (barra objetos e variáveis,
    deixa o jogador passar) e encosta por dentro na caixa da saída. Uma barreira que também barra o
    jogador não serve, porque ninguém sairia.
  - Salas que só se encostam são válidas (as fases são contínuas). Caixa vazia ou sala sem tamanho pulam
    as checagens de espaço, para não gerar uma enxurrada de erros repetidos.
  - O ponto de renascimento dentro de uma zona que barra **variáveis** é permitido (só não pode ser
    numa zona de queda nem numa barreira que barra o jogador).
  - Objeto encostado num botão não é sobreposição. Objeto em cima do botão logo no começo dá erro.

### Tarefa 5 do `levels` — ligação com o `rules` ✅
- Testes (`MappingTests`) escritos antes do `RulesMapping`. Desta vez não rodei a fase vermelha antes de
  escrever o código (os testes nem compilariam sem a classe), mas todos foram feitos antes dele.
- `ToLevelRules(nível, sala)` aplica as regras da sala por cima das do nível, só nos campos que a sala define.
  `BuildWorld(nível, sala)` cria um `World` novo com o jogador e os objetos da sala.
- Critérios 8 (sala sem propriedade liberada recusa toda troca), 9 (sala 3.1: a troca de massa com a
  caixa deixa o jogador com 300 kg e o invariante vale) e 10 (montar de novo = reiniciar a sala).
- Testes: 96/96 do `levels` (8 novos), 94/94 do `rules`. Build com 0 avisos. Checkpoint "Core" concluído.
- **Decisões tomadas sozinho:**
  - No `World`, a posição do jogador é o **centro do corpo** (pés + 0,9 m), como a dos objetos. Assim a
    troca de posição troca centro com centro. O `game` converte para os pés do `CharacterBody3D`.
  - Área de contato do jogador: 0,6 × 0,6 = 0,36 m².
  - Sala que não existe lança exceção (erro de programação, como no `rules`), e não gera um erro de nível.

### Tarefa 6 do `levels` — catálogo e níveis 1 e 2 ✅
- Primeiro os testes do catálogo (`CatalogTests`), depois o `LevelCatalog`, a única classe que lê arquivos.
  Ele confere a ordem dos números (pode pular número), se o arquivo existe e se o id bate com o arquivo.
- Depois os **testes de design** das salas 1.1–1.4 e 2.1–2.3 (`RoomDesignTests`): vermelhos, com 9 falhas,
  antes de existirem os arquivos. Só então escrevi `levels/level_1.json`, `level_2.json` e `catalog.json`.
  Todo nível entregue também precisa montar um mundo que respeite o invariante (`ShippedLevelsTests`).
- Testes: 118/118 do `levels`, 94/94 do `rules`. Build com 0 avisos.
- **Decisões tomadas sozinho:**
  - **Segurança:** no catálogo, o arquivo tem que ser um nome simples na pasta `levels/`. Nada de
    `../`, subpasta ou unidade (`C:`). Assim um catálogo alterado não consegue ler arquivos de fora.
  - **Sala 1.4:** o nível 1 só libera a posição, então a caixa que "não aceita posição" é uma caixa
    **travada**. Um `accepts` com outra propriedade daria `NotAccepted`. O teste e a spec foram ajustados.
    **Vale você confirmar:** o contorno colorido por propriedade só faz sentido de verdade a partir do
    nível 3, quando há mais de uma propriedade liberada.
  - **Sala 1.3:** para pôr a caixa no botão alto, coloquei uma bola em cima da plataforma (2 m). A
    sequência é: trocar com a bola (sobe), ficar em cima do botão e trocar com a caixa lá de baixo (ela
    aparece no botão).
  - Salas lado a lado ao longo de +X, chão em y = 0 e buracos até y = -3 (zona de queda no fundo).
    `yawDegrees: -90` olha para +X (convenção do Godot).
  - **Recado para a spec do `game`:** a troca de posição troca **centros**. A caixa tem centro a 0,5 m
    do chão e o jogador a 0,9 m, então o jogador que vai para o lugar da caixa fica com os pés 0,4 m
    dentro do chão. O `game` precisa subir o corpo para o ponto livre mais próximo (já decidido).

### Tarefa 7 do `levels` — níveis 3 e 5 ✅
- Primeiro os testes de design das salas 3.1, 3.2, 5.1 e 5.2 e o do catálogo com os 4 níveis e as 11 salas
  (vermelhos, 5 falhas). Depois `levels/level_3.json`, `level_5.json` e o catálogo atualizado.
- Testes: 122/122 do `levels`, 94/94 do `rules`. Build com 0 avisos.
- **Decisões tomadas sozinho (rascunho, para ajustar jogando):**
  - **3.1:** é o exemplo da spec. Botão de 150 kg e caixa de 300 kg; o jogador pega o peso da caixa e sobe no botão.
  - **3.2:** caixa leve de 7 kg e saída numa plataforma de 1,5 m. Com 70 kg o jogador pula 0,5 m; com 7 kg, ~1,8 m.
  - **5.1:** bola de 20 kg a 8 m/s (640 J), num corredor próprio, quicando (`bounce` 0,95). Quem pega a
    energia sai a ~4,3 m/s na direção em que a bola ia. É preciso trocar na hora em que ela vai para
    a saída, senão o jogador é jogado para trás.
  - **5.2:** a caixa fica num corredor fechado em que o jogador não entra (cenário). O único jeito de
    movê-la até o botão é correr e passar a energia para ela.

### Tarefa 8 do `levels` — arquitetura, cobertura e documentação ✅
- `ArchitectureTests` do `levels`: só referencia o `Swapping.Rules`, sem Godot e sem NuGet (critério 13),
  e nenhum campo estático mutável.
- **Bug achado na medição:** com a cobertura ligada, o teste de campo estático acusou os campos que o
  compilador cria para as lambdas (`<>c`). Corrigido para ignorar o código gerado pelo compilador, no
  `levels` e no `rules`. A regra do plano ("medir com todos os testes passando") pegou o problema.
- Cobertura do `Swapping.Levels`: **96,7 % das linhas** (ramos 91,9 %), na mesma rodada em que os 125
  testes passaram (critério 14). O `rules` continua com 94/94.
- README: seção do `levels`, da pasta `levels/` e de como criar ou mudar um nível.

## 2026-10-31 — Módulo `levels` concluído
- Os 14 critérios estão cobertos. O relatório completo está em `docs/processo/RELATORIO-levels.md`.
- Fica para eu confirmar: na sala 1.4, a caixa que recusa a posição é uma caixa travada.

## 2026-11-01 — Caixa que não aceita troca
- **Esclarecimento meu:** uma caixa que não aceita a troca de posição **continua se mexendo pela física**.
  Dá para pegar, empurrar e arremessar; ela só não entra na troca. "Travado" e `accepts` limitam só as
  trocas. Está escrito na spec do `levels` e no `level_1.json` (sala 1.4), para o `game` não entender
  "travado" como "preso no lugar".

### Autônomo — item 1: rascunho da spec do `profiles` ✅
- Escrevi `docs/specs/SPEC-profiles.md` no formato das specs do `rules` e do `levels`. Status:
  **rascunho, aguardando sua revisão**; nenhum código foi escrito.
- A skill `agent-skills:spec-driven-development` não estava disponível neste ambiente (rotina na nuvem),
  então segui o mesmo processo à mão: objetivo, stack, comandos, estrutura, estilo, testes, limites,
  critérios de sucesso testáveis (18), suposições e perguntas abertas.
- Conteúdo: perfil com nome e ícone; código de acesso de 16 símbolos (Crockford Base32, 80 bits) gerado
  pelo servidor, que guarda só o SHA-256; login com nome + código devolve um token de sessão (também só o
  hash no banco); bloqueio por tentativas; gerar código novo; limites de envio (inclusive a política
  `uploads` que o `progress` vai usar); trocar o ícone.
- Criei `docs/processo/PERGUNTAS-PARA-O-USUARIO.md` com 5 perguntas.
- **Decisões tomadas sozinho (todas reversíveis, A1–A10 na spec):**
  - Nome de 3 a 16 caracteres, só letras sem acento, números, `_` e `-`, único sem diferenciar maiúsculas
    (evita nomes parecidos no ranking).
  - 12 ícones fixos (`icon-01` a `icon-12`).
  - SHA-256 em vez de hash lento: o código é aleatório com 80 bits, não dá para adivinhar fora do servidor.
  - Token de sessão separado do código, válido 30 dias; o código só viaja no login.
  - 5 códigos errados seguidos → 15 minutos de bloqueio. As sessões que já existem continuam valendo,
    para um atacante não tirar o dono do próprio PC.
  - Limites: criar perfil 5/hora por IP, login 10/min por IP, envios 30/min por perfil, resto 120/min.
  - O "nome já existe" (409) revela o nome, o que é aceitável porque os nomes aparecem no ranking. O
    login dá a mesma resposta para nome inexistente e código errado.
  - `profiles` e `progress` como duas pastas de **um** projeto `Swapping.Api` (mesmo host e mesmo banco),
    com teste de arquitetura separando as duas. Banco criado com `EnsureCreated()`, sem migrations.
  - Pacotes NuGet novos listados na spec (EF Core SQLite e `Mvc.Testing`): aprovar a spec aprova os dois.
  - Sem apagar perfil e sem trocar nome nesta versão.

### Autônomo — item 2: rascunho da spec do `progress` ✅
- Escrevi `docs/specs/SPEC-progress.md` no mesmo formato (status: **rascunho, aguardando sua revisão**;
  nenhum código). A skill `agent-skills:spec-driven-development` também não estava disponível; segui o processo à mão.
- Conteúdo: níveis liberados na ordem do `levels/catalog.json` (o mesmo arquivo do jogo, lido com o
  `LevelCatalog` do `levels`); envio de uma tentativa terminada; melhor resultado por nível; ranking nos
  dois modos (tempo → trocas e trocas → tempo); tabela das regras de reinício (morte e "reiniciar sala"
  continuam somando, "reiniciar nível" zera e começa outra tentativa); checagens de plausibilidade;
  espaço para o fantasma. 17 critérios de sucesso testáveis.
- 3 perguntas novas em `docs/processo/PERGUNTAS-PARA-O-USUARIO.md` (6 a 8).
- **Decisões tomadas sozinho (B1–B8 na spec, todas reversíveis):**
  - Duas linhas de melhor resultado por perfil e nível, uma por modo, porque a tentativa mais rápida e a
    de menos trocas podem ser diferentes. Não guardo o histórico de tentativas.
  - Basta um resultado qualquer no nível anterior para liberar o próximo. Buracos na numeração do
    catálogo são pulados (sexta: 1 → 2 → 3 → 5).
  - Reenviar um resultado igual ou pior devolve 200 ("não melhorou"), e não erro: a fila do `sync` pode
    descartá-lo. Nível ainda travado dá 409, que o `sync` deve tentar de novo mais tarde.
  - Plausibilidade: no mínimo 1 s por sala, no máximo 24 h, até 10 000 trocas. Sem tempo de referência por nível.
  - Desempate final por quem chegou primeiro, pela hora do **servidor** (o relógio do PC não fura a fila).
  - Ranking público, só com nome, ícone, tempo e trocas. Mortes e reinícios de sala são guardados mas não contam.
  - Fantasma: só a coluna `GhostId` (vazia) e o desenho das rotas (até 256 KB, 20 amostras/s × 5 números);
    nesta versão as rotas não existem e um campo `ghost` no envio é recusado.
  - Nenhum pacote NuGet novo além dos da spec do `profiles`; a API referencia o `Swapping.Levels` e não
    sobe se o catálogo tiver erro.

### Autônomo — item 3: revisão de segurança das specs do `profiles` e do `progress` ✅
- A skill `agent-skills:security-and-hardening` não estava disponível neste ambiente (rotina na nuvem);
  segui o mesmo processo à mão: para cada ameaça, uma proteção e um critério de sucesso testável.
- `SPEC-profiles.md` ganhou a seção **Security** (S1–S13) e os critérios 19–28. `SPEC-progress.md`
  ganhou a seção **Security** (P1–P10) e os critérios 18–26. As duas continuam **rascunho, aguardando
  sua revisão**; nenhum código foi escrito.
- Ameaças pedidas e o que ficou:
  - **Força bruta no código:** 80 bits + bloqueio 5/15 min + 10 logins/min por IP (uns 3 × 10¹⁸ anos
    em média). Achado: tentativas em paralelo podiam passar do limite de 5 lendo o contador ao mesmo
    tempo → o contador agora sobe numa **única atualização SQL atômica** (critério 19).
  - **Enumeração de nomes:** risco aceito, porque os nomes já são públicos no ranking; o login responde
    igual para nome inexistente e código errado, e o limite de criação (5/hora por IP) impede varrer
    nomes pelo `409`. O bloqueio pode ser usado para atrapalhar um nome conhecido (pergunta 9).
  - **Resultado falso ou em excesso:** o perfil vem só do token (campo `profileId` no corpo → 400);
    30 envios/min por perfil; no máximo 2 linhas por perfil e nível, envie quantas vezes for.
    O resultado falso plausível é **risco aceito** (sem replay no servidor), escrito na spec (B9, pergunta 10).
  - **Injeção de dados:** só LINQ do EF Core (teste que procura `FromSqlRaw` e afins no código), nomes
    com lista de caracteres permitidos (também evita BBCode no `RichTextLabel` do Godot), JSON com
    profundidade máxima 8, sem campos desconhecidos, números só inteiros; `levelId` conferido no catálogo
    antes de qualquer consulta.
  - **CORS:** o jogo é um programa nativo, não uma página, então **nenhuma política de CORS** (tudo
    negado) e o token vai no cabeçalho, nunca em cookie (sem CSRF).
- **Decisões tomadas sozinho (reversíveis):**
  - **A11:** gerar código novo exige o código atual no corpo, senão quem roubasse o token derrubaria as
    outras sessões e tomaria o perfil (pergunta 11). Mudei a linha da rota na tabela de endpoints.
  - **A12:** HTTPS + HSTS fora do `Development`; no `Development` (o PC da apresentação) o Kestrel escuta
    só em `127.0.0.1`. Isso responde em parte a pergunta 4. `X-Forwarded-For` não é confiável por padrão.
  - `Cache-Control: no-store` nas respostas com código ou token; `X-Content-Type-Options: nosniff` em todas.
  - **B10:** código de erro novo `InvalidQuery` para `mode`/`top` inválidos no ranking.
  - Dois índices para os dois modos do ranking, e a posição do jogador calculada com `COUNT`.
  - Corrigi o número do critério de força bruta para a conta certa (2⁷⁹ ÷ 175 200 tentativas por ano).
- 3 perguntas novas em `docs/processo/PERGUNTAS-PARA-O-USUARIO.md` (9 a 11).

### Autônomo — item 4: revisão da spec do `rules` (critério 12, exemplos de pulo) ✅
- Refiz as contas com o volume fixo do jogador (0,07 m³), ou seja, densidade = massa ÷ 0,07, no ar (1,2 kg/m³):
  70 kg → 0,5006 m; 35 kg → 1,0024 m; **7 kg → 1,859 m**; 300 kg → 0,1167 m.
  Só o caso de 7 kg mudava de verdade: com 7 kg a densidade cai para 100 kg/m³, o empuxo do ar pesa um
  pouco mais e o pulo vai de 1,84 para **1,86 m**. Os outros três continuam dentro de ±0,01.
- `SPEC-rules.md`: o texto do pulo e o critério 12 agora dizem que a densidade vem do volume fixo e
  trazem 1,86 m para 7 kg, com uma nota explicando de onde vinha o 1,84. **A fórmula não mudou.**
- Teste novo `FormulaTests.JumpHeight_InAir_WithPlayerFixedVolume` (4 casos, densidade calculada com
  `Formulas.Density(m, 0,07)`). O teste antigo `JumpHeight_InAir` (densidade 1 000) **ficou como estava**,
  porque continua certo para um corpo de densidade 1 000; nenhum teste foi apagado ou afrouxado.
- Build com 0 avisos; testes 98/98 do `rules` (eram 94) e 125/125 do `levels`, também na rodada com
  cobertura (`Swapping.Rules` 98,3 % das linhas, `Swapping.Levels` 96,7 %).
- **Decisão tomada sozinho:** corrigi também o número em `docs/design/PROGRESSAO-NIVEIS.md` (sala 3.2:
  "~1,86 m"). É só o texto do número; a sala em `levels/level_3.json` não foi tocada e continua passando
  no teste de design (saída a pelo menos 1,2 m).

### Autônomo — item 5: rascunho da spec do `sync` ✅
- Feito **no PC**, com a skill `agent-skills:spec-driven-development` carregada de verdade (a rotina da
  nuvem foi pausada porque lá as skills não existem; os itens 1 a 4 foram feitos na nuvem seguindo o
  processo à mão).
- `docs/specs/SPEC-sync.md` (rascunho, aguardando aprovação). Biblioteca C# pura, sem referência a projeto
  nem NuGet. Ela guarda o save local (perfil, melhores resultados por modo, fila de envio, recusados) e
  registra a partida na hora, sem rede. Depois manda a fila em ordem, com uma tabela que diz o que fazer
  com cada resposta da API (200, 401, 409, 423/429, 400/404/422, erro de rede/5xx), espera crescente entre
  tentativas (5 s dobrando até 10 min) e no fim junta o progresso do servidor com o local.
  São 16 critérios de sucesso.
- **Decisões tomadas sozinho** (C1–C7 na spec, com as perguntas 12–14 em `PERGUNTAS-PARA-O-USUARIO.md`):
  - **Fila enxuta:** guarda no máximo 2 envios por nível e perfil (o melhor de cada modo), porque o
    servidor só guarda o melhor.
  - **Erros permanentes** (400, 404, 422) vão para uma lista de recusados e não travam a fila.
  - **Save:** gravado de forma atômica (arquivo temporário e troca) e regravado a cada mudança. Save
    corrompido é guardado à parte e o jogo começa um novo.
  - **Sem relógio nem temporizador** dentro da biblioteca: o jogo diz a hora e quando enviar.
  - **Token vencido:** entra de novo uma vez sozinho com o nome e o código salvos; se falhar, avisa `NeedsLogin`.

### Autônomo — item 6: rascunho da spec do `game` (nível 1) ✅
- Feito no PC com a skill `agent-skills:spec-driven-development`. `docs/specs/SPEC-game.md` (rascunho).
- O que a primeira versão jogável faz: as 4 salas do nível 1 em primeira pessoa; segurar, arremessar e
  trocar de posição; morte em buraco ou queda; reiniciar sala e nível; tempo e trocas na tela desde a 1.1.
- O passo a passo da troca: mirar (com um teste a seco que pinta a mira de branco ou vermelho), medir o
  contexto, aplicar pelo `rules`, abrir espaço com o ponto livre mais próximo (senão morte ou estouro) e,
  a cada passo da física, `SyncFromPhysics`.
- Levei para a spec as pendências dos relatórios: detecção contínua de colisão, paredes de pelo menos
  0,3 m, troca de centros, caixa travada que ainda se mexe e morte por queda. São 12 critérios de sucesso.
- **Decisões tomadas sozinho** (G1–G6 na spec, perguntas 15–20):
  - Uma biblioteca nova `Swapping.GameLogic`, **sem Godot**, para tudo que dá para testar fora da engine
    (cronômetro e contagem, estado da mira, cor do contorno, busca do ponto livre, morte por queda). Os
    scripts do Godot ficam finos.
  - Um **teste de fumaça sem janela** (`--smoke-test level-1`) que monta cada sala e executa a solução
    planejada. Ele roda pelo `dotnet test` e é pulado onde o Godot não existe (na nuvem).
  - Os JSON dos níveis são copiados para `game/levels/` na compilação, para o jogo e a API lerem os
    mesmos arquivos.
  - Tudo que é gosto (cores, pulo, sensibilidade, efeitos) ficou em "Perguntas abertas".

### Autônomo — item 7: revisão de código do `rules` e do `levels` ✅
- Feita no PC com a skill `agent-skills:code-review-and-quality` (5 eixos: correção, legibilidade,
  arquitetura, segurança, desempenho). Revisei principalmente o `rules`, que foi escrito pela nuvem e que
  eu ainda não tinha lido linha a linha.
- **Teste de mutação** (a skill pede para provar que os testes pegam erros, não só ler). Troquei de propósito
  4 condições do código, rodei os testes e restaurei o arquivo. **As 4 foram pegas.**
  - `Box.Overlaps`: `<` virou `<=`. 15 testes falharam.
  - `World.SyncFromPhysics`: a direção não é mais guardada quando o objeto para. 1 falhou.
  - `RulesMapping`: a recarga da sala é ignorada. 1 falhou.
  - `LevelRules`: `distância > alcance` virou `>=`. 1 falhou.
- **Achados corrigidos** (obrigatórios, cada um com teste escrito antes da correção, que falhou primeiro):
  - **Pedido de troca vazio ou com a mesma propriedade repetida:** era "aplicado", contava 1 jogada e
    iniciava a recarga sem mudar nada. Isso inflaria a contagem do ranking por um erro do `game`. Agora o
    `SwapRequest` recusa os dois casos com exceção (erro de programação, como manda a spec), inclusive pelo
    `with`.
  - **Massa ≤ 0, NaN ou infinita em `World.Add`**, e **NaN ou infinito vindo da física** em
    `SyncFromPhysics`: um valor assim estragaria as variáveis para sempre (v = √(2E/m)). Agora dá
    exceção e as variáveis ficam intactas.
  - Testes: 106/106 do `rules` (8 novos em `RequestValidationTests`), 125/125 do `levels`, com e sem
    cobertura. Cobertura do `rules`: 98,5 % das linhas.
- **Achados anotados, sem mudança:**
  - **FYI:** `RulesMapping.BuildWorld` supõe um nível já validado; com uma propriedade desconhecida ele
    lança exceção. O `game` e a API sempre validam antes (a API nem sobe com catálogo inválido).
  - **Consider:** o `LevelCatalog` lê os arquivos sem limite de tamanho. São arquivos do próprio jogo, então
    o risco é baixo. Vale um limite se um dia vierem níveis de fora (mods).
  - **FYI:** `EnableEntangledProperties` não é usada por ninguém, de propósito (ponto de extensão da spec);
    não é código morto.
  - **Arquitetura:** dependências num só sentido (`levels` → `rules`), nenhuma referência a Godot, nenhum
    estado global. **Desempenho:** sem problemas para o tamanho das salas (dezenas de objetos).

### Autônomo — item 8: C4 nível 3 (Componentes), parcial ✅
- Acrescentei em `docs/architecture/C4.md` o diagrama de Componentes do **Game Client**, desenhado a partir do
  código que existe: as classes principais do `levels` e do `rules`, e as setas de quem chama quem. O
  `game` aparece tracejado, porque ainda não existe. Há também uma tabela com os testes e a cobertura de
  cada componente.
- **Decisão tomada sozinho:** marquei o diagrama como **parcial** e com data. O `sync`, o `GameLogic` e os
  componentes da API entram quando forem construídos, e o nível 4 (Código) fica para o fim, como pede o
  enunciado.

### Autônomo — item 9: site que explica a arquitetura ✅
- `docs/site/index.html`: uma página só, que abre direto no navegador, sem internet e sem instalar nada.
  Em português, com analogias:
  - variáveis = **crachás**;
  - módulos = **restaurante** (`rules` = chef, `levels` = cardápio, `game` = garçom, `sync` = caderneta
    do fiado, `profiles` = portaria, `progress` = quadro de recordes);
  - barreiras = **aeroporto e alfândega**;
  - testes = **inspetor que tenta quebrar**.
- Tem um **simulador clicável** do exemplo da spec (10/2/3 → 3/10/2 → A atravessa → 3/2/10). Ele usa a
  mesma lógica do `World`; conferi a conta rodando o script no Node. Também mostra o passo a passo de uma
  troca e uma tabela "ideia do banco → onde está no código".
- **Decisão tomada sozinho:** é um arquivo local (não publiquei em lugar nenhum) e tem modo escuro
  automático. Para abrir: dois cliques em `docs/site/index.html`.

### Autônomo — item 10: roteiro da apresentação ✅
- `docs/apresentacao/ROTEIRO.md` (rascunho). Divide os 10 minutos pelos critérios e pesos: 1 min de
  complexidade, 1 min 30 de spec e arquitetura, 2 min de skills, 2 min de qualidade, 1 min 30 de
  evidências e 2 min de demo.
- Inclui a tabela "fase → skill → arquivo de evidência", os 4 bugs achados revisando o trabalho da IA, o
  passo a passo da demo com um vídeo de reserva e a **lista de prints** que só você pode capturar.
- **Decisão tomada sozinho:** a demo fica no fim, porque vale 10 pontos e é o que mais pode falhar.
- **Pendência anotada:** refazer a revisão de segurança (item 3) com a skill de verdade. Na nuvem ela foi
  feita à mão.

## 2026-11-02 — Lista autônoma concluída
- Os 10 itens de `tasks/autonomo.md` estão marcados. A nuvem fez os itens 1 a 4 sem as skills, seguindo o
  processo à mão; o PC fez os itens 5 a 10 com as skills. O loop do PC se cancela sozinho.

### Autônomo — item 11: revisão de segurança refeita com a skill ✅
- Feita no PC com a skill `agent-skills:security-and-hardening` carregada de verdade. Ela segue a ordem
  modelo de ameaças primeiro (fronteiras de confiança, bens, STRIDE), depois o checklist (OWASP, segredos,
  dependências, privacidade, LLM).
- **O que a nuvem já tinha feito certo** (S1–S13 e P1–P10, sem mudança): força bruta e bloqueio (com
  contador atômico contra pedidos em paralelo), comparação de hash em tempo constante, consultas
  parametrizadas, validação da entrada com limite de tamanho e campos desconhecidos recusados, erros sem
  detalhes internos, sem CORS, `X-Forwarded-For` não confiável, HTTPS fora do PC da apresentação.
- **O que faltava e entrou** (cada item com um critério de sucesso testável):
  - **S14 / P11 — repúdio (o "R" do STRIDE):** um registro pequeno de eventos de segurança (bloqueio,
    código novo, login recusado, limite atingido, resultado recusado). Grava o perfil, o IP e o nome do
    evento, **nunca** o código, o token ou o corpo do pedido. Assim um resultado falso achado no ranking
    pode ser ligado ao perfil que o enviou.
  - **S15 — dependências (cadeia de suprimentos):** versões fixas, arquivo de travamento do NuGet
    (`packages.lock.json`) e `dotnet list package --vulnerable` antes da apresentação.
  - **S16 — privacidade (LGPD):** classificação dos dados. Nome e ícone são públicos; o IP só fica na
    memória e **nunca é gravado** no banco; não há e-mail nem senha. Apagar um perfil é um passo manual em SQL
    documentado, até você responder a pergunta 5.
  - **`sync` — transporte:**
    - não aceita `http://` fora do próprio PC;
    - nunca desliga a checagem de certificado (um "atalho" comum que deixaria o token ser roubado);
    - não segue redirecionamentos;
    - limita o tamanho das respostas (256 KB) e do save (1 MB).
- **Não se aplica** (anotado na spec): cookies e CSRF (o cliente é nativo e o token vai no cabeçalho), XSS
  em HTML (não há página web), upload de arquivo e SSRF (não existem nesta versão), recursos de IA no jogo.

### Autônomo — item 12: os rascunhos combinam entre si? ✅
- Feito com a skill `agent-skills:spec-driven-development`, na parte "manter a spec viva". Cruzei
  `SPEC-profiles`, `SPEC-progress`, `SPEC-sync` e `SPEC-game`.
- **O que já combinava:**
  - todas as respostas de `POST /results` (200, 400, 401, 404, 409, 422, 423, 429, 5xx) têm uma ação na
    tabela do `sync`;
  - os campos do resultado têm o mesmo nome no `progress` e no `sync`;
  - as duas ordens do ranking são iguais no servidor e no save local;
  - o `409 LevelLocked` é tentado de novo, como o `progress` pede;
  - o reenvio é seguro (resposta 200 "não melhorou");
  - a fila (no máximo 2 por nível, 8 com os 4 níveis de sexta) cabe com folga no limite de 30 envios
    por minuto;
  - o `http://` aceito só no próprio PC bate com o `127.0.0.1` da API na apresentação.
- **Diferenças achadas e corrigidas nos rascunhos:**
  1. A spec do `profiles` (S9) pedia que o `game` mostrasse os nomes num `Label` simples, mas a spec do
     `game` não falava disso. Agora fala.
  2. A spec do `sync` não dizia o que fazer com os erros de **criar ou entrar** num perfil (`InvalidName`,
     `NameTaken`, `InvalidCredentials`...). Agora diz: não vão para a fila, voltam direto para o jogo
     mostrar, e no sucesso os envios sem perfil ganham o id novo.
  3. A fila podia mandar uma partida pior que o melhor local. Agora só entra na fila a partida que
     melhorou o melhor local em pelo menos um modo.
  4. Os nomes `bestTime`/`bestMoves` da API e `time`/`moves` do save local agora têm a ligação escrita.
     Também ficou escrito o que o `game` entrega ao `sync`: os mesmos campos do `POST /results`.

## 2026-11-02 (segunda) — specs da API aprovadas e plano

### Aprovação das specs do `profiles` e do `progress`
- **Quem decidiu: o usuário**, numa pergunta com 4 itens:
  - nomes sem acento e sem espaço (A1);
  - a API roda no PC da apresentação, só em `127.0.0.1`;
  - as mortes são guardadas, mas não aparecem no ranking (B7);
  - os outros padrões estão aprovados como estavam: sessão de 30 dias, bloqueio de 15 min depois de
    5 erros, código novo só com o código atual, resultado falso barrado só pela plausibilidade, top 20,
    e nada de apagar o perfil.
- As respostas estão em `PERGUNTAS-PARA-O-USUARIO.md` (perguntas 1 e 3 a 11).

### Plano da API (`agent-skills:planning-and-task-breakdown`)
- `tasks/api/plan.md` e `tasks/api/todo.md`: **11 tarefas** em 4 fases (base, `profiles`, `progress` e
  fechamento).
- **Decisão do agente:** um só plano para os dois módulos, porque eles são duas pastas de um só projeto
  (A8). Os ids dos módulos continuam `profiles` e `progress`.
- Cada tarefa é uma fatia vertical: entidade → serviço → rota → teste HTTP. As regras puras (código,
  nome, ordem do ranking e plausibilidade) vêm antes, cada uma com testes pequenos.
- **Por que as proteções do host entram na tarefa 1:** com elas no começo, todas as rotas seguintes já
  nascem com o corpo limitado a 4 KB, o JSON estrito, os erros sem detalhes internos e o `nosniff`.

### API — tarefa 1: base do projeto ✅
- Feito com `agent-skills:test-driven-development`. Projetos `src/Swapping.Api` (ASP.NET Core, minimal
  APIs) e `tests/Swapping.Api.Tests` (xUnit + `WebApplicationFactory`), na solução.
- **Pacotes** (aprovados junto com a spec): `Microsoft.EntityFrameworkCore.Sqlite` 8.0.33 e
  `Microsoft.AspNetCore.Mvc.Testing` 8.0.33, as versões 8.0 mais novas, iguais ao runtime instalado.
  Com os arquivos de lock (`packages.lock.json`) e a restauração em modo travado (S15).
- **Fase vermelha:** 9 testes, 6 falhando. Os 3 que passaram de primeira:
  - os 2 dos arquivos de lock, que o `restore` criou junto com os projetos;
  - o "corpo de 4 KB exatos passa", que já dava 404 sem nenhum código.
- **O que a base faz:**
  - os erros saem em `application/problem+json` com um campo `code`, sem detalhes internos;
  - toda resposta tem `nosniff`;
  - um corpo acima de 4 KB é recusado, mesmo sem o cabeçalho de tamanho;
  - nos testes, o banco é o SQLite em memória;
  - ao rodar com `dotnet run`, a API escuta só em `127.0.0.1`.
- **Erro meu, corrigido:** um teste conferia se o esquema do banco era criado ao subir, mas sem nenhuma
  tabela o `EnsureCreated` sempre diz "criei". Ele foi trocado por um teste que confere que os testes usam o
  banco em memória (nenhum `.db` é criado). A checagem do esquema foi para a tarefa 3, quando já houver
  tabelas.
- **Passou para a tarefa 3:** JSON malformado, profundidade acima de 8 e campos desconhecidos. Para
  testar isso é preciso uma rota que leia um corpo.
- **Conferido de verdade:** `dotnet run` sobe em `http://127.0.0.1:5257`, e um `curl` recebe o 404 com
  `code` e `nosniff`.
- 240 testes verdes (106 + 125 + 9), 0 avisos.

### API — tarefa 2: código de acesso e nome ✅ (checkpoint da base)
- `AccessCode` (gera, normaliza, confere o formato, calcula o hash e compara em tempo constante) e
  `ProfileNames` (as regras de nome da A1, o corte dos espaços e a chave sem maiúsculas). As duas são
  classes puras, sem banco e sem HTTP.
- **Fase vermelha:** primeiro só as assinaturas, que lançam `NotImplementedException`. 39 testes falharam.
- **Testes que vão além da spec:**
  - 1 000 códigos usam os 32 símbolos do alfabeto (pega erro na extração dos bits);
  - um `А` cirílico, que parece um `A` latino, é recusado;
  - um nome com tabulação é recusado.
- **Erro meu num teste, não no código:** eu esperava `"0011"` para `"OIlL"`, mas lá há um `O` e três
  letras que valem 1, então o certo é `"0111"`. O teste foi corrigido e o código não mudou.
- 279 testes verdes (106 + 125 + 48), 0 avisos.

### API — tarefa 3: criar perfil ✅
- `POST /profiles` e `GET /icons`, com as entidades `ProfileEntity` e `SessionEntity`, o `ProfileService`
  e as opções lidas da configuração.
- **Fase vermelha:** 27 testes novos, 25 falhando (só havia as tabelas, sem nenhuma rota).
- **Decisões do agente:**
  - **JSON estrito.** O padrão "Web" do ASP.NET Core **aceita número dentro de string** (`"60000"`), e
    isso foi desligado já agora, pensando no `POST /results` (P5). Também ficaram recusados os campos
    desconhecidos e a profundidade acima de 8.
  - **Corpo ilegível → 400 vazio**, que vira `InvalidBody`, inclusive em `Development`. No padrão de
    `Development`, o ASP.NET Core lançaria uma exceção.
  - **Datas no SQLite como número.** O SQLite não sabe comparar `DateTimeOffset`, e o bloqueio e a
    validade do token precisam disso.
  - **Nome repetido:** a API checa antes e o índice único do banco checa de novo, caso dois pedidos
    criem o mesmo nome ao mesmo tempo.
- **Testes que leem o banco "cru"** (`DatabaseDump`): procuram o código e o token em texto em todas as
  colunas de todas as tabelas, e conferem que nenhuma coluna tem cara de IP ou e-mail (P-SC 5 e 31).
- **Teste de erro 500:** um relógio que lança exceção. A resposta tem `code: ServerError` e nenhum
  rastro da exceção.
- **Mutações:**
  - tirar a recusa de campos desconhecidos → 3 testes caíram;
  - tirar a checagem de nome repetido → o índice único ainda pega (é o que se espera).
- 306 testes verdes (106 + 125 + 75), 0 avisos.

### API — tarefa 4: token, "meu perfil" e logout ✅
- `BearerAuth`: um manipulador de autenticação do ASP.NET Core que calcula o hash do token, procura uma
  sessão ainda válida e põe o id do perfil no pedido. `GET /profiles/me`, `PATCH /profiles/me` (só o
  ícone) e `DELETE /sessions/current`.
- **Fase vermelha:** 16 testes novos, 16 falhando.
- **Testes além da spec:**
  - o código de acesso não serve como token;
  - `Basic …` e `bearer` sem o token são recusados;
  - o `PATCH` não troca o nome (`name` é um campo desconhecido → `InvalidBody`, A10);
  - "meu perfil" devolve só os 4 campos públicos.
- **Mutação:** com `>=` no lugar de `>` na validade, o teste "para de valer aos 30 dias" caiu, como
  deveria.
- 322 testes verdes (106 + 125 + 91), 0 avisos.

### API — tarefa 5: entrar e bloqueio ✅
- `POST /sessions`: nome + código → token novo. Nome desconhecido, nome inválido e código errado recebem
  a **mesma resposta, byte a byte**. Para isso, o `traceId` saiu dos erros, já que ele mudava a cada
  pedido e tornava as duas respostas diferentes.
- **Bloqueio:** 5 erros seguidos → `423 ProfileLocked` com `Retry-After` em segundos. Quando o bloqueio
  acaba, o contador volta a zero. A contagem do erro é **um único `UPDATE … WHERE FailedAttempts < 5`**
  no banco (S2), e não "ler, somar 1 e gravar".
- **Fase vermelha:** 12 testes novos, 11 falhando.
- **Problema achado pela mutação (o melhor do dia):**
  - tirei o `FailedAttempts < 5` do `UPDATE` e o teste "20 tentativas ao mesmo tempo" **continuou
    verde**;
  - motivo: os pedidos acabavam rodando quase em fila, e depois do 5º erro os outros já viam o perfil
    bloqueado antes de chegar no contador, então a corrida nunca acontecia;
  - correção: um teste novo que força o pior caso, 20 contagens em paralelo, cada uma com o seu próprio
    contexto do banco, como se todas já tivessem passado da checagem do bloqueio;
  - com a mutação, os 2 testes caem; sem ela, os 2 passam;
  - o teste HTTP rodou 5 vezes seguidas sem falhar.
- **Banco em arquivo para os testes com pedidos em paralelo:** a conexão única em memória não aceita
  vários pedidos ao mesmo tempo. Com essa opção, a factory cria um `.db` temporário e o apaga no fim.
- 335 testes verdes (106 + 125 + 104), 0 avisos.

### API — tarefa 6: código novo e registro de segurança ✅
- `POST /profiles/me/access-code` só funciona com o código atual (A11). O código antigo para de valer e
  todas as outras sessões são revogadas; a sessão que pediu continua valendo. Código errado ou ausente
  conta como erro para o bloqueio, então um token roubado não consegue chutar o código para sempre.
- `SecurityLog` (categoria `Swapping.Security`, S14) registra: login recusado, início do bloqueio e
  código regenerado. Cada evento leva o id do perfil e o IP, e nada vai para o banco.
- `Cache-Control: no-store` nas 3 rotas que devolvem código ou token (S7).
- **Fase vermelha:** 10 testes novos, 9 falhando. O de "nenhum log tem segredo" já passava, porque ainda
  não havia log nenhum.
- **Logger de teste:** guarda todas as linhas de log da API, inclusive as do ASP.NET Core e do EF Core,
  no nível `Trace`. O teste procura os 2 códigos, o código errado (com e sem traços) e os 2 tokens em
  todas elas.
- **Mutações:**
  - revogar também a sessão que pediu o código novo → 1 teste caiu;
  - pôr o cabeçalho `Authorization` no registro de segurança → o teste "nenhum log tem segredo" caiu.
- 345 testes verdes (106 + 125 + 114), 0 avisos.

### API — tarefa 7: limites e transporte ✅ (checkpoint: `profiles` completo)
- **Limites de pedidos** (janela fixa, em memória, números lidos de `Profiles:RateLimits`):
  - criar perfil: 5 por hora por IP;
  - entrar: 10 por minuto por IP;
  - envios de resultado: 30 por minuto por perfil;
  - o resto: 120 por minuto por IP.
  Ao passar do limite: `429` com `Retry-After` e um evento `RateLimitHit` no registro de segurança.
- **Transporte:**
  - fora de `Development`, HTTPS obrigatório (redireciona) e HSTS;
  - nenhuma política de CORS;
  - o `X-Forwarded-For` só é lido se houver proxies configurados (nenhum por padrão).
- **Decisão do agente:** nos testes, a factory começa com os limites altíssimos, e só `RateLimitTests`
  usa os números de produção. Senão, os testes de bloqueio (10 logins seguidos) bateriam no limite por
  acaso.
- **Fase vermelha:** 10 testes novos, 7 falhando. Os 3 de CORS e de `Development` já passavam, porque ali
  o certo é justamente não fazer nada.
- **Mutação:** tirar o limite do login (ele cairia no limite geral de 120) → 2 testes caíram.
- **Conferido com a API rodando de verdade** (`dotnet run` + `curl`): criar um perfil, depois 11 logins
  errados → 5 × `401`, 5 × `423` e `429` no 11º.
- **Checkpoint:** todos os critérios do `profiles` têm teste, menos o 17, o 18 e o 30, que ficam para a
  tarefa 11 (arquitetura, cobertura e auditoria). 355 testes verdes (106 + 125 + 124), 0 avisos.

### API — tarefa 8: catálogo, ordem do ranking e plausibilidade ✅
- `RankingOrder` (os 2 modos como dados, com os desempates) e `ResultCheck` (≥ 1 s por sala, ≤ 24 h,
  contagens de 0 a 10 000) são classes puras. `LevelDirectory` carrega o `levels/catalog.json` com o
  `LevelCatalog.Load` do módulo `levels`, o mesmo arquivo que o jogo lê. `GET /levels` lista os níveis.
- **A API não sobe com um catálogo quebrado ou vazio.** O catálogo é carregado logo depois do
  `Build()`, antes de atender qualquer pedido.
- **Fase vermelha:** 33 testes novos, 33 falhando (só as assinaturas).
- **Dois tropeços no helper de teste, não no código:**
  1. os arquivos de nível têm comentários `//`, e o `JsonNode.Parse` precisa da opção de pular
     comentários;
  2. eu tinha usado os ids `t-1`, `t-2` e `t-4`, mas o validador do `levels` exige `level-N` e salas
     `N.k`. O catálogo de teste agora usa `level-1`, `level-2` e `level-4` e renumera as salas.
- **O catálogo de teste com o buraco 1 → 2 → 4** é montado numa pasta temporária a partir dos níveis
  reais. Assim, mexer no conteúdo dos níveis de sexta não quebra os testes do `progress`.
- 388 testes verdes (106 + 125 + 157), 0 avisos.

### API — tarefa 9: enviar resultado e ver meu progresso ✅
- `POST /results` (token obrigatório, limite `uploads` de 30 por minuto por perfil) e `GET /progress/me`
  (todos os níveis do catálogo, na ordem, com `unlocked`, `bestTime` e `bestMoves`).
- `BestResultEntity`:
  - chave (perfil, nível, modo), então no máximo 2 linhas por perfil e nível;
  - `GhostId` anulável e sempre nulo nesta versão;
  - os 2 índices dos rankings já criados (o teste deles é da tarefa 10);
  - apagar o perfil apaga as linhas.
- **Ordem das verificações:** nível desconhecido (404) → nível travado (409) → resultado implausível
  (422). As recusas 409 e 422 gravam um evento `ResultRefused` com o perfil, o nível, o código e os
  números, nunca o corpo.
- **Decisões do agente (reversíveis):**
  - `unlockedNext` só é `true` no envio que abriu o próximo nível pela primeira vez. Um reenvio, ou o
    último nível, dá `false`;
  - `levelId: null` → 400 `InvalidBody` (o System.Text.Json do .NET 8 não barra `null` sozinho);
  - se dois primeiros envios do mesmo nível chegam juntos, a chave primária deixa só um entrar, e o
    outro lê de novo e compara. Esse caminho **não tem teste** (o envio do `sync` é em fila, um por vez).
- **Fase vermelha:** 49 testes novos, 46 falhando. Os 3 que já passavam deviam passar: as rotas do
  fantasma não existem (404), e a coluna `GhostId` veio do esqueleto da entidade.
- **Um teste errado, não o código:** eu esperava que um tempo menor com as mesmas trocas não melhorasse
  o modo `moves`. Mas ali o tempo é o desempate, então melhora. O teste passou a usar 13 trocas.
- **Mutações:** as duas primeiras nem compilaram (avisos contam como erro), então refiz:
  - todos os níveis destravados → 2 testes caíram;
  - um resultado que não é melhor também sobrescreve → 3 testes caíram;
  - `LevelLocked` sem evento de segurança → 1 teste caiu.
- 437 testes verdes (106 + 125 + 206), 0 avisos.

### API — tarefa 10: rankings ✅
- `GET /rankings/{levelId}?mode=time|moves&top=N` (público) e `GET /rankings/{levelId}/me?mode=…`
  (token). Cada linha do ranking tem só `rank, name, icon, timeMs, swaps`; o `…/me` devolve
  `rank, timeMs, swaps, totalPlayers` ou 404 `NoResult`.
- **A ordem é uma só, escrita duas vezes:** em memória (`RankingOrder.For`, já testada) e para o
  banco (`InOrder` para o top N e `AheadOf` para a posição com um `COUNT`, sem carregar a tabela).
  Um teste com empates na primeira e na segunda chave confere, nos dois modos, que a posição do
  `…/me` é a mesma da lista completa.
- **Leitura da query (`RankingQuery`), estrita:** `mode` só `time` ou `moves` (exatos, sem `TIME`);
  `top` só dígitos de 1 a 100 (padrão 20); parâmetro repetido → 400 `InvalidQuery`. Parâmetros
  desconhecidos são ignorados. O `levelId` é conferido no catálogo **antes** de qualquer consulta,
  então `..%2F..%2Fetc` e `x' OR 1=1` dão 404 `UnknownLevel` nas duas rotas.
- **Decisão do agente (reversível):** depois da data do servidor, o desempate final é o id do
  perfil, só para a lista sair sempre igual. Dois envios no mesmo tique do relógio (100 ns) só
  acontecem em teste; nesse caso o `…/me` daria a mesma posição aos dois.
- **Fase vermelha:** 38 testes novos, 37 falhando. O que já passava devia passar: os 2 índices
  vieram da tarefa 9.
- **Mutações (3 de 3 pegas):**
  - data em ordem decrescente no modo `time` → 2 testes caíram;
  - `AheadOf` do modo `moves` contando a mesma data como "na frente" → 3 testes caíram;
  - `top` aceitando até 1000 → 1 teste caiu.
- 475 testes verdes (106 + 125 + 244), 0 avisos.

### API — tarefa 11: arquitetura, cobertura, auditoria e relatório ✅ (checkpoint: API completa)
- **Testes de arquitetura** (`ArchitectureTests`, 4 testes):
  - nenhum arquivo de `Profiles/` cita o namespace ou um tipo de `Progress/`;
  - nenhum campo, propriedade ou assinatura de `Profiles/` usa um tipo de `Progress/` (por reflexão);
  - a API não referencia Godot e só tem o pacote `Microsoft.EntityFrameworkCore.Sqlite`;
  - nenhum arquivo de `src/Swapping.Api` roda SQL cru (`FromSqlRaw`, `ExecuteSqlRaw`, `SqlQueryRaw` e
    parecidos).
  - **Mutação:** um arquivo em `Profiles/` com uma propriedade `RankingMode` e um `ExecuteSqlRaw` →
    os 3 testes certos caíram.
- **Cobertura** (rodada única, 479 testes passando): `Profiles/` **98,3 %**, `Progress/` **96,5 %**
  (meta ≥ 90 %), a API inteira 96,7 %. O que falta: os ramos `default` que lançam exceção, o catálogo
  vazio e a nova tentativa depois de dois envios simultâneos.
- **Auditoria** (`dotnet list package --vulnerable --include-transitive`):
  - nenhum pacote vulnerável no `rules`, no `levels` e na API;
  - nos 3 projetos de teste, o xunit 2.5.3 puxa o `NETStandard.Library` 1.6.1, que traz o
    `System.Net.Http` 4.3.0 e o `System.Text.RegularExpressions` 4.3.0, os dois com aviso **alto**;
  - **decisão do agente:** fixar as versões corrigidas (4.3.4 e 4.3.1) num `tests/Directory.Build.props`.
    No .NET 8 essas DLLs nem são carregadas, mas assim a auditoria fica limpa;
  - auditoria de novo: **0 em 6 projetos**. O `restore --locked-mode` passa.
- **Tropeço:** no meio do restore, o `api.nuget.org` não resolveu (falha de rede passageira) e o
  arquivo de lock ficou pela metade. Na segunda tentativa deu certo, e o `--locked-mode` confirmou.
- **Conferido com a API rodando de verdade** (`dotnet run` + `curl`, banco temporário, níveis reais):
  - 4 níveis listados;
  - 2 perfis enviaram resultados;
  - `time` → Bia, Ana; `moves` → Ana, Bia; `…/me` da Ana → 2º de 2;
  - `TIME;DROP` → 400, `..%2F..%2Fetc` → 404.
- **Relatório** em `docs/processo/RELATORIO-api.md`: os 31 critérios do `profiles` e os 27 do
  `progress`, cada um ligado aos testes que o cobrem. C4 com o diagrama de Componentes da API e
  README com as rotas.
- 479 testes verdes (106 + 125 + 248), 0 avisos.
