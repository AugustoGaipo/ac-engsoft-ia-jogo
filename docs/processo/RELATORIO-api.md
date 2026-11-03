# Relatório da API (`profiles` + `progress`)

> Concluída em 2026-11-03. Specs: [`SPEC-profiles.md`](../specs/SPEC-profiles.md) ("P-SC n") e
> [`SPEC-progress.md`](../specs/SPEC-progress.md) ("G-SC n") · Plano: [`tasks/api/plan.md`](../../tasks/api/plan.md) ·
> Tarefas: [`tasks/api/todo.md`](../../tasks/api/todo.md) · Diário: [`DIARIO.md`](DIARIO.md).

## Resumo
- **O que é:** a API do jogo, em ASP.NET Core 8 com SQLite (`src/Swapping.Api`). Ela tem dois módulos
  e uma base compartilhada:
  - `Profiles/`: perfil com nome e ícone, código de acesso, login, bloqueio, token e logout;
  - `Progress/`: catálogo de níveis, envio de resultado, destravar os níveis em ordem, melhor resultado
    por modo, rankings por tempo e por trocas;
  - `Hosting/`: erros padronizados, limite de corpo, limites de pedidos, registro de segurança.
    `Data/` tem o banco.
- **Como foi feito:**
  - no PC, de 2026-11-02 a 2026-11-03, em 11 tarefas, cada uma com o seu commit;
  - skills usadas:
    - `spec-driven-development`: as duas specs foram aprovadas pelo usuário antes do código;
    - `security-and-hardening`: revisão das specs com a lista da skill;
    - `planning-and-task-breakdown`: o plano com 11 tarefas e 2 checkpoints;
    - `test-driven-development`: em todas as tarefas, teste vermelho → código → mutação;
    - `incremental-implementation`.

| Medida | Valor |
|---|---|
| Testes da API | **248 aprovados de 248** (xUnit + `WebApplicationFactory`), em 15 arquivos de teste |
| Testes do `rules` e do `levels` | 106 + 125, continuam passando (total da solução: **479**) |
| Cobertura de linhas, `Profiles/` | **98,3 %** (292 de 297); meta da spec ≥ 90 % |
| Cobertura de linhas, `Progress/` | **96,5 %** (250 de 259); meta da spec ≥ 90 % |
| Cobertura de linhas, `Swapping.Api` inteiro | 96,7 % (ramos 88,4 %) |
| Build | 0 avisos (`TreatWarningsAsErrors`) |
| Pacotes vulneráveis | **0** em todos os 6 projetos, contando os transitivos (ver "Auditoria") |
| Critérios de sucesso | **31 de 31** (`profiles`) e **27 de 27** (`progress`) cobertos |

A cobertura foi medida numa rodada só, com os 479 testes passando
(`dotnet test Swapping.sln --collect:"XPlat Code Coverage"`).

## O que foi feito em cada tarefa
| # | Tarefa | Principais peças | Testes da API ao fim |
|---|---|---|---|
| 1 | Base do projeto | `ApiProblem` (`problem+json` com `code`), `BodySizeLimit` (4 KB), `nosniff`, só `127.0.0.1`, arquivos de lock | 9 |
| 2 | Código de acesso e nome | `AccessCode` (Crockford, SHA-256, comparação em tempo constante), `ProfileNames` | 48 |
| 3 | Criar perfil | `POST /profiles`, `GET /icons`, JSON estrito, só hashes no banco | 75 |
| 4 | Token e logout | `BearerAuth`, `GET`/`PATCH /profiles/me`, `DELETE /sessions/current`, validade de 30 dias | 91 |
| 5 | Login e bloqueio | `POST /sessions`, mesma resposta para nome e código errados, bloqueio com um `UPDATE` atômico | 104 |
| 6 | Código novo e registro | `POST /profiles/me/access-code`, `SecurityLog`, `no-store` | 114 |
| 7 | Limites e transporte | 4 políticas de limite, HTTPS e HSTS fora do PC, sem CORS, `X-Forwarded-For` ignorado | 124 |
| 8 | Catálogo e ordens | `LevelDirectory`, `RankingOrder`, `ResultCheck`, `GET /levels` | 157 |
| 9 | Enviar resultado | `BestResultEntity` (2 linhas por perfil e nível), `POST /results`, `GET /progress/me` | 206 |
| 10 | Rankings | `GET /rankings/{levelId}`, `GET /rankings/{levelId}/me`, `RankingQuery`, `InOrder`/`AheadOf` | 244 |
| 11 | Arquitetura e relatório | `ArchitectureTests`, cobertura, auditoria, este relatório, C4, README | 248 |

## Critérios do `profiles` → testes
| # | Critério | Testes que cobrem |
|---|---|---|
| 1 | Build e testes sem avisos | `TreatWarningsAsErrors`; `HostTests.Unknown_route_answers_404_as_problem_details_with_a_code` (o teste de fumaça) |
| 2 | `POST /profiles` → 201 com código no formato e um token que funciona | `CreateProfileTests.Creating_a_profile_returns_201_with_a_code_and_a_token`, `SessionTests.The_token_from_creation_opens_my_profile` |
| 3 | 1 000 códigos diferentes e no formato | `AccessCodeTests.A_thousand_generated_codes_are_all_different_and_well_formed`, `Generated_codes_use_every_symbol_of_the_alphabet` |
| 4 | Normalização de caixa, traços, espaços e `O`/`I`/`L` | `AccessCodeTests.Normalize_ignores_case_dashes_and_spaces`, `Normalize_reads_O_as_zero_and_I_and_L_as_one` |
| 5 | O banco só tem SHA-256 | `CreateProfileTests.The_database_keeps_only_SHA_256_hashes_of_the_code_and_the_token`, `AccessCodeTests.The_hash_is_SHA_256_of_the_normalized_code_and_never_the_code` |
| 6 | Regras de nome e corte dos espaços | `ProfileNamesTests.Valid_names`, `Invalid_names`, `Leading_and_trailing_spaces_are_trimmed`, `Length_is_counted_after_trimming`; `CreateProfileTests.An_invalid_name_is_InvalidName`, `The_name_is_saved_trimmed` |
| 7 | `ANA` depois de `Ana` → 409 `NameTaken` | `CreateProfileTests.The_same_name_in_another_case_is_NameTaken` |
| 8 | Ícone fora da lista → 400; `GET /icons` | `CreateProfileTests.An_icon_not_in_the_list_is_UnknownIcon`, `Get_icons_lists_the_12_icons` |
| 9 | Login com o código em qualquer caixa, com ou sem traços | `LoginTests.The_right_code_logs_in_with_a_new_token`, `The_code_works_in_any_case_and_without_dashes_and_the_name_in_any_case` |
| 10 | Código errado e nome desconhecido → a mesma resposta | `LoginTests.A_wrong_code_and_an_unknown_name_get_the_same_answer` |
| 11 | 6ª tentativa → 423 com `Retry-After`; aos 15 min volta a zero | `LoginTests.After_5_wrong_codes_even_the_right_code_is_ProfileLocked`, `The_lock_still_holds_at_14_59_and_ends_at_15_minutes`, `After_the_lock_ends_wrong_codes_count_from_zero_again` |
| 12 | Token antigo vale durante o bloqueio | `LoginTests.A_token_made_before_the_lock_still_works_while_locked` |
| 13 | Código novo: o antigo para, as outras sessões caem | `RegenerateTests.A_new_code_replaces_the_old_one_and_revokes_the_other_sessions` |
| 14 | Token aos 30 dias − 1 s vale, aos 30 dias não; logout | `SessionTests.A_token_works_until_one_second_before_30_days`, `A_token_stops_working_at_30_days`, `Logout_answers_204_and_the_token_stops_working` |
| 15 | Limites de criar perfil e de login | `RateLimitTests.The_6th_profile_creation_from_one_IP_within_an_hour_is_429`, `The_11th_login_within_a_minute_is_429` |
| 16 | Corpo > 4 KB, JSON malformado, campo desconhecido → 400; sem rastro de exceção | `HostTests.A_body_over_4_KB_is_refused_with_InvalidBody`, `A_body_over_4_KB_without_a_length_header_is_refused_too`; `CreateProfileTests.Malformed_bodies_are_InvalidBody`, `Unknown_fields_are_InvalidBody_and_nothing_is_created`, `A_server_error_is_a_500_without_exception_details` |
| 17 | `Profiles` não usa `Progress`; nada de Godot | `ArchitectureTests.No_Profiles_source_file_names_the_Progress_namespace_or_any_of_its_types`, `No_Profiles_type_has_a_Progress_type_in_its_fields_properties_or_signatures`, `The_API_references_no_Godot_assembly_or_package` |
| 18 | Cobertura de `Profiles/` ≥ 90 % | medição: 98,3 % |
| 19 | 20 logins errados ao mesmo tempo → 5 × 401 e 15 × 423 | `LoginTests.Twenty_concurrent_wrong_logins_give_exactly_5_failures_and_15_locks`, `Twenty_racing_failure_counts_lock_after_exactly_5` |
| 20 | Código novo sem o atual → 401 e conta como erro | `RegenerateTests.A_wrong_current_code_is_InvalidCredentials_and_counts_as_a_failure`, `Without_the_current_code_it_is_InvalidCredentials_and_counts_as_a_failure` |
| 21 | HTTPS e HSTS em produção; `127.0.0.1` no PC | `TransportTests.In_production_plain_http_is_redirected_to_https`, `In_production_https_answers_carry_HSTS`; `HostTests.Development_launch_settings_listen_only_on_127_0_0_1` |
| 22 | Nenhuma linha de log tem código ou token | `RegenerateTests.No_log_line_ever_contains_a_code_or_a_token` |
| 23 | `no-store` nas 3 rotas com segredo; `nosniff` em tudo | `RegenerateTests.Answers_that_carry_a_code_or_a_token_are_never_cached`, `HostTests.Every_response_has_nosniff` |
| 24 | Sem SQL cru; `Robert'); DROP TABLE…` é nome inválido | `ArchitectureTests.No_source_file_runs_raw_SQL`; `CreateProfileTests.An_invalid_name_is_InvalidName` (a tabela continua existindo) |
| 25 | `[b]x[/b]`, `<i>x`, `a b` → `InvalidName` | `ProfileNamesTests.Invalid_names`, `CreateProfileTests.An_invalid_name_is_InvalidName` |
| 26 | Profundidade > 8; `id`/`accessCodeHash` no corpo → 400 | `CreateProfileTests.Malformed_bodies_are_InvalidBody`, `Unknown_fields_are_InvalidBody_and_nothing_is_created` |
| 27 | Sem CORS | `TransportTests.A_web_page_from_another_origin_gets_no_CORS_permission` |
| 28 | `X-Forwarded-For` falso não ganha cota nova | `RateLimitTests.A_fake_X_Forwarded_For_does_not_give_a_new_allowance` |
| 29 | Evento por bloqueio e por código novo; nenhum segredo | `RegenerateTests.A_lockout_writes_one_security_event`, `Refused_logins_and_code_regeneration_write_security_events`, `No_log_line_ever_contains_a_code_or_a_token` |
| 30 | Arquivos de lock, `--locked-mode`, auditoria no diário | `HostTests.NuGet_lock_files_exist`; `dotnet restore --locked-mode` sem erro; auditoria abaixo e no diário |
| 31 | Nenhuma coluna com IP, e-mail ou segredo em texto | `CreateProfileTests.No_column_holds_an_IP_an_email_or_a_plain_secret` |

## Critérios do `progress` → testes
| # | Critério | Testes que cobrem |
|---|---|---|
| 1 | Build e testes sem avisos | `TreatWarningsAsErrors` |
| 2 | Perfil novo: só o 1º nível aberto, sem melhores | `SubmitResultTests.A_new_profile_has_only_the_first_level_unlocked_and_no_bests` |
| 3 | Destravar 1 → 2 → 4 (pula o buraco) | `SubmitResultTests.A_result_for_level_1_unlocks_level_2`, `A_result_for_level_2_unlocks_level_4_skipping_the_gap`, `The_last_level_unlocks_nothing`, `UnlockedNext_is_true_only_on_the_upload_that_unlocked_the_next_level` |
| 4 | Nível travado → 409; fora do catálogo → 404 | `SubmitResultTests.A_result_for_a_locked_level_is_409_LevelLocked_and_nothing_is_stored`, `A_result_for_a_level_not_in_the_catalog_is_404_UnknownLevel` |
| 5 | Melhor tempo e melhor em trocas separados | `SubmitResultTests.The_best_time_and_the_best_moves_are_kept_separately`, `A_tie_in_the_first_key_is_broken_by_the_second` |
| 6 | Reenvio pior → 200, nada muda | `SubmitResultTests.A_resent_result_that_is_not_better_is_200_and_leaves_the_rows_unchanged` |
| 7 | As duas ordens (C, B, A e B, A, C) | `RankingOrderTests.Time_mode_sorts_by_time_then_swaps`, `Moves_mode_sorts_by_swaps_then_time`; `RankingTests.The_time_ranking_orders_by_time_then_by_swaps`, `The_moves_ranking_orders_by_swaps_then_by_time` |
| 8 | Empate total → quem enviou primeiro | `RankingOrderTests.Equal_results_are_ordered_by_who_got_it_first`; `RankingTests.With_identical_results_the_first_upload_ranks_higher_in_both_modes` |
| 9 | Linhas só com `rank, name, icon, timeMs, swaps` | `RankingTests.The_answer_has_the_level_the_mode_and_entries_with_only_five_fields` |
| 10 | `top` 0/101 → 400; padrão 20 | `RankingTests.By_default_a_ranking_has_at_most_20_entries_and_top_changes_it`, `A_bad_mode_or_top_is_400_InvalidQuery` |
| 11 | `…/me`: posição, `totalPlayers`, 404 `NoResult` | `RankingTests.My_rank_and_the_number_of_players_in_both_modes`, `My_rank_matches_my_position_in_the_full_ranking`, `My_rank_without_a_result_is_404_NoResult` |
| 12 | Limites de plausibilidade | `ResultCheckTests` (5 testes, todos os limites); `SubmitResultTests.An_implausible_result_is_422_and_nothing_is_stored`, `The_plausibility_floor_uses_the_room_count_of_the_level` |
| 13 | Campo `ghost` ou desconhecido → 400; rotas do fantasma → 404 | `SubmitResultTests.A_body_that_is_not_exactly_a_result_is_400_InvalidBody`, `The_ghost_routes_answer_404` |
| 14 | 31º envio no minuto → 429 | `SubmitResultTests.The_31st_upload_by_one_profile_within_a_minute_is_429` |
| 15 | Com o `levels/` real: 1, 2, 3, 5 com 4, 3, 2, 2 salas | `LevelsEndpointTests.With_the_shipped_levels_folder_the_API_lists_the_4_Friday_levels` |
| 16 | Coluna `GhostId` anulável | `SubmitResultTests.The_best_result_table_has_a_nullable_GhostId_column` |
| 17 | Cobertura de `Progress/` ≥ 90 % | medição: 96,5 %; e os `ArchitectureTests` |
| 18 | `profileId` no corpo → 400; o resultado de A nunca aparece em B | `SubmitResultTests.A_body_that_is_not_exactly_a_result_is_400_InvalidBody`, `A_result_sent_with_A_token_is_never_shown_under_B`; `RankingTests.A_result_sent_with_A_token_never_appears_under_B` |
| 19 | 1 000 envios → 2 linhas | `SubmitResultTests.A_thousand_uploads_leave_exactly_two_rows` |
| 20 | Sem token válido → 401 e nenhuma linha | `SubmitResultTests.Without_a_valid_token_the_answer_is_401_and_no_row_is_written` |
| 21 | `..%2F..%2Fetc`, `x' OR 1=1` → 404; `TIME;DROP`, `abc` → 400 | `RankingTests.An_id_not_in_the_catalog_is_404_UnknownLevel`, `A_bad_mode_or_top_is_400_InvalidQuery`, `My_rank_with_a_bad_mode_is_400_InvalidQuery` |
| 22 | `1.5`, `"60000"`, `3000000000` → 400 | `SubmitResultTests.A_body_that_is_not_exactly_a_result_is_400_InvalidBody` |
| 23 | Os 2 índices dos rankings no modelo | `RankingTests.The_model_has_one_index_per_ranking_order` |
| 24 | Sem CORS nos rankings e no `POST /results` | `RankingTests.A_page_from_another_origin_gets_no_CORS_permission_on_rankings_or_results` |
| 25 | Depois do logout, o token não envia | `SubmitResultTests.After_logout_the_token_cannot_send_results` |
| 26 | `PUT /results/{levelId}/time/ghost` → 404 | `SubmitResultTests.The_ghost_routes_answer_404` |
| 27 | 422 e 409 gravam um evento de segurança sem o corpo | `SubmitResultTests.An_implausible_result_writes_one_security_event_without_the_body`, `A_result_for_a_locked_level_writes_one_security_event`, `An_accepted_result_writes_no_security_event` |

**Nenhum critério ficou descoberto.**

## Auditoria das dependências (P-SC 30)
- `dotnet list Swapping.sln package --vulnerable --include-transitive` em 2026-11-03:
  - os 3 projetos que vão para o jogo ou para o servidor (`Swapping.Rules`, `Swapping.Levels`,
    `Swapping.Api`) **não tinham nenhum pacote vulnerável**;
  - os 3 projetos de **teste** tinham 2 pacotes transitivos com aviso alto: `System.Net.Http` 4.3.0
    (GHSA-7jgj-8wvc-jh57) e `System.Text.RegularExpressions` 4.3.0 (GHSA-cmhx-cq75-c4mj). O
    `dotnet nuget why` mostrou a origem: xunit 2.5.3 → `NETStandard.Library` 1.6.1.
- **Correção:**
  - `tests/Directory.Build.props` fixa as versões corrigidas (4.3.4 e 4.3.1) nos 3 projetos de teste;
  - o `packages.lock.json` foi refeito;
  - a auditoria foi rodada de novo: **0 pacotes vulneráveis nos 6 projetos**.
  - No .NET 8 essas DLLs antigas nem são carregadas, então o risco real já era nulo. A correção serve
    para a auditoria ficar limpa e qualquer aviso novo aparecer de cara.
- `dotnet restore Swapping.sln --locked-mode` passa sem erro, então ninguém troca um pacote sem mudar o
  arquivo de lock.

## A API rodando de verdade (2026-11-03)
`dotnet run --project src/Swapping.Api` com um banco temporário e os níveis reais, e depois `curl`:
```
GET  /levels                          → level-1 (4 salas), level-2 (3), level-3 (2), level-5 (2)
POST /results (Ana: 95 000 ms, 6 trocas; Bia: 80 000 ms, 9 trocas) → {"improvedTime":true,"improvedMoves":true,"unlockedNext":true}
GET  /rankings/level-1?mode=time      → 1 Bia, 2 Ana
GET  /rankings/level-1?mode=moves     → 1 Ana, 2 Bia
GET  /rankings/level-1/me?mode=time   → {"rank":2,"timeMs":95000,"swaps":6,"totalPlayers":2}
GET  /rankings/level-1?mode=TIME;DROP → 400 InvalidQuery
GET  /rankings/..%2F..%2Fetc?mode=time → 404 UnknownLevel
```

## Decisões tomadas sem o usuário
Estão no diário, uma seção por tarefa. As que mais importam:

**Segurança e formato**
- **JSON estrito em todo corpo:** número dentro de string, campo desconhecido e profundidade acima de 8
  são recusados. `levelId: null` também (o .NET 8 não barra sozinho).
- **Login sem pistas:** nome desconhecido, nome inválido e código errado recebem a mesma resposta, byte
  a byte. Por isso o `traceId` saiu das respostas de erro.
- **Datas no SQLite como número**, porque o SQLite não sabe comparar `DateTimeOffset`.
- **Limites altíssimos nos testes**, menos nos `RateLimitTests`, que usam os números de produção.

**Progresso e rankings**
- **`unlockedNext`** só é `true` no envio que abriu o próximo nível pela primeira vez.
- **Parâmetros da query** (`mode`, `top`) só com o valor exato e sem repetição. Os parâmetros
  desconhecidos são ignorados.
- **Último desempate do ranking:** o id do perfil, depois da hora do servidor, só para a lista sair
  sempre na mesma ordem. Dois envios no mesmo tique de 100 ns só acontecem em teste.

## Problemas encontrados
1. **Uma mutação que sobreviveu (tarefa 5):** tirei a condição `FailedAttempts < 5` do `UPDATE`, e o
   teste dos 20 logins ao mesmo tempo continuou verde, porque os pedidos acabavam rodando quase em fila.
   Um teste novo força o pior caso (20 contagens em paralelo, cada uma com o seu próprio contexto do
   banco) e pega a mutação.
2. **Testes errados, não o código:** `"OIlL"` normaliza para `"0111"`, não `"0011"` (tarefa 2). E um
   tempo menor com as mesmas trocas **melhora** o modo `moves`, porque ali o tempo é o desempate (tarefa 9).
3. **Uma falha de rede passageira** no restore da tarefa 11 (`api.nuget.org` não resolveu) deixou um
   arquivo de lock pela metade. Ele foi refeito na segunda tentativa e conferido com `--locked-mode`.

## O que fica sem teste (e por quê)
- **Dois primeiros envios do mesmo nível ao mesmo tempo:** a chave primária deixa só um entrar, e o
  outro lê de novo e compara (`ProgressService`, 4 linhas). O `sync` envia em fila, um por vez.
- **Os ramos `default` dos `switch`** que lançam `ArgumentOutOfRangeException` para um modo que não
  existe, e o catálogo **vazio** (o catálogo quebrado tem teste).

## Pendências
- **Fantasma** (B8): só a coluna `GhostId` e o desenho das rotas. As rotas respondem 404.
- **Resultado falso plausível** (P1, risco aceito): sem replay no servidor, a API não prova que a
  partida aconteceu. Uma linha suspeita pode ser apagada à mão no banco.
- As perguntas abertas das duas specs continuam em
  [`PERGUNTAS-PARA-O-USUARIO.md`](PERGUNTAS-PARA-O-USUARIO.md).
