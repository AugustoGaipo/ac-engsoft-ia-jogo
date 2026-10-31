# Relatório do módulo `rules`

> Módulo concluído em 2026-10-31. Spec: [`docs/specs/SPEC-rules.md`](../specs/SPEC-rules.md) ·
> Plano: [`tasks/rules/plan.md`](../../tasks/rules/plan.md) · Tarefas: [`tasks/rules/todo.md`](../../tasks/rules/todo.md) ·
> Diário: [`DIARIO.md`](DIARIO.md).

## Resumo
- **O que é:** a biblioteca `Swapping.Rules` em C# puro (.NET 8). Ela decide o que uma troca faz e se é
  permitida. Não depende de Godot, de arquivos, do relógio, de aleatoriedade nem de pacotes NuGet.
- **Resultado:**

| Medida | Valor |
|---|---|
| Testes | **94 aprovados de 94** (xUnit), em 8 arquivos |
| Cobertura de linhas | **98,3 %** (ramos 89,8 %); meta da spec ≥ 90 % |
| Build | 0 avisos (`TreatWarningsAsErrors` ligado) |
| Critérios de sucesso | **15 de 15** cobertos |

- **Como foi feito:** com as skills `agent-skills:spec-driven-development` (spec aprovada antes do código),
  `planning-and-task-breakdown` (8 tarefas) e `test-driven-development` (teste antes do código).
- **Quem fez cada parte:**
  - Tarefa 1: no PC.
  - Tarefas 2–8: uma **rotina autônoma na nuvem**, de hora em hora, das 14:40 às 17:30 (horário de Brasília).
  - Revisão final: no PC.

## O que foi feito em cada tarefa
| # | Tarefa | Principais classes | Testes ao fim |
|---|---|---|---|
| 1 | Estrutura da solução | `Swapping.sln`, `Directory.Build.props`, projetos `src`/`tests` | 1 |
| 2 | Fórmulas de física e meios | `Vec3`, `Formulas` (energia, densidade, empuxo, pressão, recuo, pulo), `Medium` | 27 |
| 3 | Variáveis, containers e invariante | `PropertyKind`, `Variable`, `Container`, `World` (`ExchangeVariables`, `CheckInvariant`) | 36 |
| 4 | Trocas de posição, massa e energia | `SwapRequest`, `SwapOperation`, `SwapResult`, `BodyUpdate`, `World.SyncFromPhysics` | 45 |
| 5 | Legalidade e recarga | `LevelRules`, `SwapContext`, `RejectReason` (11 motivos), `Cooldown` | 65 |
| 6 | Barreiras e restauração | `BarrierFilter` (`ExitGrid`), `Restoration` | 76 |
| 7 | Registro, contagem e teste aleatório | `SwapLog`, `World.MoveCount`, 1 000 passos com semente fixa | 83 |
| 8 | Pontos de extensão, cobertura e docs | `ISwapDurationPolicy`, `ICooldownModifier`, `ArchitectureTests`, README | 94 |

A ideia central que sai do código: **uma regra só calcula o corpo a partir das variáveis em uso**
(velocidade = direção da variável de energia × √(2·E ÷ m)). As três trocas e as restaurações saem dela.

## Critérios de sucesso → testes
| # | Critério | Testes que cobrem |
|---|---|---|
| 1 | Build e testes sem avisos | `TreatWarningsAsErrors` no `Directory.Build.props` (o build falha com qualquer aviso) |
| 2 | Massa 10/2/3 → 3/10/2; A entra na barreira → 3/2/10 | `SwapTests.MassSwap_Chained_PlayerGetsBsMass`, `RestorationTests.MassRestoration_AEntersBarrier_GetsItsOwnMassBack` |
| 3 | 2 kg a 3 m/s → 8 kg → 1,5 m/s | `FormulaTests.MassChange_KeepsEnergy`, `SwapTests.MassSwap_KeepsEnergy_RecomputesSpeed` |
| 4 | Bola 1 kg a 4 m/s ↔ jogador 70 kg → ≈ 0,478 m/s | `FormulaTests.EnergyOfBall_GivesPlayerSpeed`, `SwapTests.EnergySwap_ReceiverMovesTheWayTheGiverWasMoving` |
| 5 | Troca de posição mantém as velocidades | `SwapTests.PositionSwap_ExchangesPlaces_KeepsVelocities` |
| 6 | Restauração de posição | `RestorationTests.PositionRestoration_MovingBoxSwapsPlacesWithRestingHolder` |
| 7 | Restauração de energia inverte a direção | `RestorationTests.EnergyRestoration_ReversesDirection` |
| 8 | Um teste por motivo de recusa | `LegalityTests`: `SameContainer`, `ModeNotEnabled_ObjectToObject`, `PropertyNotUnlocked`, `PropertyNotAccepted`, `Locked`, `OutOfRange`, `NoLineOfSight`, `BlockedByBarrier`, `InsideVariableBarrier`, `TooManyProperties`, `OnCooldown_AfterAnyProperty` (+ `DistanceEqualToRange_IsAllowed`) |
| 9 | Recarga de 2 s: t = 1,5 recusada, t = 2,0 aplicada | `LegalityTests.Cooldown2s_At1_5IsRejected_At2_0IsApplied` |
| 10 | Pressão 9 800 Pa; quebra vidro de 5 000, não o de 10 000 | `FormulaTests.Pressure_MatchesSpec` |
| 11 | Nada na água; afunda com a pedra; voa no ar com 0,05 kg | `FormulaTests.PlayerInWater_Swims`, `PlayerHoldingRock_Sinks`, `LightPlayerInAir_Swims` (+ bordas 0,1× e 1,1×) |
| 12 | Pulo: 0,50 / 1,00 / 1,84 / 0,12 m | `FormulaTests.JumpHeight_InAir`, `JumpTakeoffSpeed_IsCappedByLegs` (ver "Problemas", item 2) |
| 13 | Invariante em 1 000 passos aleatórios | `InvariantTests.Invariant_HoldsAfterEachOf1000RandomSteps` |
| 14 | Sem referência a Godot | `ArchitectureTests.RulesAssembly_ReferencesNoGodotAssembly`, `RulesProject_HasNoPackageOrProjectReferences` |
| 15 | Cobertura de linhas ≥ 90 % | medição com `dotnet test --collect:"XPlat Code Coverage"`: 98,3 % |

**Nenhum critério ficou descoberto.** Os critérios 1 e 15 são verificados pelo build e pela medição de
cobertura, não por um teste xUnit.

## Decisões tomadas sem o usuário
Todas estão no diário, uma seção por tarefa. As que mais importam:

**Modelo de dados**
- **Ids:** os containers usam `string` (`"player"`, `"A"`); as variáveis usam `"dono.propriedade"` (`A.mass`).
- **Valor da variável:** um par escalar + vetor. A energia guarda os joules no escalar e a direção unitária
  no vetor. Com o objeto parado, a variável guarda a última direção.
- **Aceitação:** `AcceptedProperties = null` quer dizer "aceita todas". `Container`, `Vec3` e
  `BarrierFilter` são imutáveis.

**Legalidade e recarga**
- **Várias falhas ao mesmo tempo:** vale o primeiro motivo na ordem da tabela da spec.
- **Quem trava a troca:** `Locked` e `PropertyNotAccepted` valem se **qualquer um** dos dois lados falhar.
- **Recarga:** só começa com uma troca aplicada. Troca recusada e restauração não reiniciam a recarga.
- **`SwapContext`:** vai dentro do `SwapRequest`, com o padrão "tudo livre", para manter a assinatura
  `Apply(world, request, now)` da spec.

**Restauração e contagem**
- **`Restoration`:** é uma classe estática sem estado, não um `IOperation`. Restaurar não é trocar, então
  ignora a legalidade e não conta jogada.
- **`MoveCount`:** é o número de entradas do registro, então os dois nunca divergem. A troca simultânea vale 1.

**Pontos de extensão e testes**
- **Duração e recarga:** `ISwapDurationPolicy` devolve `null` para "permanente". `ICooldownModifier`
  recebe e devolve segundos.
- **Flag de propriedades entrelaçadas:** ficou em `LevelRules`, por fase. Ninguém usa ainda.
- **Teste extra, não pedido:** nenhum campo estático mutável no assembly, para fiscalizar a fronteira da spec.
- **Ajuste nos testes da Tarefa 4:** passaram a usar regras permissivas, porque testam o efeito da troca e
  não a legalidade. Nenhuma asserção foi alterada.

**Valores e ambiente**
- **Arrasto provisório:** ar 0,05, gás denso 0,1 e água 3,0 (1/s), ajustáveis jogando.
- **SDK na nuvem:** o ambiente bloqueou o instalador oficial do .NET (403), então a rotina usou
  `apt-get install dotnet-sdk-8.0`.

## Problemas encontrados
1. **Teste quebrava só com a cobertura ligada (achado na revisão no PC):**
   - `RulesAssembly_HasNoStaticMutableFields` falhava porque o coverlet injeta classes de contagem com
     campos estáticos no assembly.
   - O teste agora ignora o namespace `Coverlet` (commit `62e0820`). O código do jogo não mudou.
   - A rotina tinha medido 98,3 % e registrado 94/94, mas não rodou os testes **durante** a medição.
     Por isso vale revisar o trabalho autônomo.
2. **Exemplos de pulo da spec (critério 12):**
   - Os valores 0,50 / 1,00 / 1,84 / 0,12 m só batem com ±0,01 se a densidade do corpo for ~1 000 kg/m³
     (g_eff ≈ g).
   - Com o volume fixo do jogador (0,07 m³), 7 kg dá ρ = 100 kg/m³, e a fórmula correta dá **1,86 m**.
   - Os testes usam `density: 1000`, e `JumpHeight` recebe a densidade como parâmetro, então o código
     está certo. **A spec precisa de um ajuste nos exemplos.**
3. **Corpo menos denso que o meio:** `JumpHeight` devolve +∞ (g_eff ≤ 0). É uma escolha simples; o
   `game` precisa tratar esse caso (o corpo boia ou flutua em vez de pular).

## O que está pendente
- **Revisão pequena da spec do `rules`:**
  - corrigir os exemplos de pulo (problema 2);
  - fórmula da barra de lançamento (a carga dá energia, com limite pela velocidade dos braços);
  - pressão de pilha (o vidro sente o peso da pilha inteira).
- **Valores provisórios para ajustar jogando:** arrasto dos meios e recarga dos níveis 7–10.
- **Responsabilidades fora do `rules`**, decididas depois da spec e registradas no diário. Ficam no
  `game`/`progress`:
  - morte e renascimento;
  - ponto livre mais próximo;
  - soma do tempo e das trocas do nível inteiro entre salas.
- **Rotina na nuvem:** pausada em 2026-10-31, depois da Tarefa 8.
