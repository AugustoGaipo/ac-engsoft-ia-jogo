# Perguntas para o usuário

Dúvidas que só você pode responder, anotadas pelo loop autônomo. Em cada uma eu segui com a suposição mais
simples e fácil de desfazer (entre parênteses). Responda quando puder; nada aqui travou o trabalho.

**2026-11-02:** você respondeu as perguntas 1 e 3 a 11, e as specs do `profiles` e do `progress` foram aprovadas.
Faltam a 2 (ícones) e as 12 a 20 (`sync` e `game`).

## Spec do `profiles` (`docs/specs/SPEC-profiles.md`, aprovada)
1. ✅ **Nomes com acento e espaço** (ex.: `João`, `Ana Maria`) podem? *(Suposição A1: não. Só letras sem
   acento, números, `_` e `-`, de 3 a 16 caracteres, para evitar nomes parecidos no ranking.)*
   **Resposta (2026-11-02):** não; fica a suposição A1.
2. **Ícones:** quantos e quais desenhos? *(Suposição A2: 12 ícones, `icon-01` a `icon-12`; os desenhos
   ficam para a spec do `game`.)*
3. ✅ **Sessão e bloqueio:** a sessão vale 30 dias e o perfil fica bloqueado 15 minutos depois de 5 códigos
   errados seguidos. Tudo bem? *(Suposições A4 e A5.)*
   **Resposta (2026-11-02):** sim, 30 dias e 5 erros / 15 min.
4. ✅ **Onde a API vai rodar na apresentação:** no seu PC ou num servidor na internet? Isso decide HTTPS e
   CORS. *(Por enquanto: no PC, `dotnet run`; a revisão de segurança do item 3 trata disso.)*
   **Resposta (2026-11-02):** no PC da apresentação, só em `127.0.0.1`.
5. ✅ **Apagar o perfil ou trocar o nome:** o jogador pode? *(Suposição A10: não nesta versão.)*
   **Resposta (2026-11-02):** não nesta versão (A10).

## Spec do `progress` (`docs/specs/SPEC-progress.md`, aprovada)
6. ✅ **Mortes no ranking:** mostrar o número de mortes, ou uma marca para quem terminou sem morrer?
   *(Suposição B7: as mortes e os reinícios de sala são guardados, mas não aparecem nem contam no ranking.)*
   **Resposta (2026-11-02):** guardar e não mostrar (B7).
7. ✅ **Tempo mínimo plausível:** 1 s por sala basta, ou cada nível deve ter um tempo de referência no JSON?
   *(Suposição B4: 1 s por sala e no máximo 24 h.)*
   **Resposta (2026-11-02):** 1 s por sala basta (B4).
8. ✅ **Tamanho do ranking no jogo:** os 20 primeiros, ou os 10 primeiros mais a sua posição?
   *(A API devolve até 100, padrão 20, e tem uma rota com a sua posição.)*
   **Resposta (2026-11-02):** os 20 primeiros.

## Revisão de segurança das specs da API (item 3)
9. ✅ **Bloqueio usado para atrapalhar:** como os nomes aparecem no ranking, qualquer pessoa pode errar o
   código 5 vezes e bloquear o seu nome por 15 minutos num PC novo (no PC em que você já jogou a sessão
   continua valendo). Aceita, ou contar os erros por perfil **e** por IP? *(Suposição: aceitar; o código
   de 80 bits já torna a força bruta impossível, o bloqueio é só uma camada a mais.)*
   **Resposta (2026-11-02):** aceito junto com os padrões; o bloqueio fica só por perfil.
10. ✅ **Resultado falso:** sem o servidor refazer a partida, um jogo modificado pode mandar um tempo bom
    mas plausível. Tudo bem só checar a plausibilidade e apagar à mão um resultado trapaceado?
    *(Suposição B9: sim, risco aceito e escrito na spec.)*
   **Resposta (2026-11-02):** sim; só a plausibilidade, e apagar à mão (B9).
11. ✅ **Gerar código novo pede o código atual:** para quem roubar o token não conseguir tomar o perfil.
    O jogo manda o código do save, então você não digita nada. Tudo bem? *(Suposição A11: sim.)*
   **Resposta (2026-11-02):** sim (A11).

## Spec do `sync` (`docs/specs/SPEC-sync.md`, rascunho)
12. **Código e token no save sem criptografia:** ficam em texto no arquivo do seu PC (`user://save.json`).
    Tudo bem? *(Suposição C1: sim. Criptografar com o DPAPI do Windows só funcionaria no Windows.)*
13. **Jogar sem perfil:** pode jogar antes de criar um perfil, e os resultados são enviados quando você
    criar ou entrar num? *(Suposição C3: sim.)*
14. **Trocar de perfil no mesmo PC:** os melhores resultados locais do perfil anterior somem da tela e
    ficam só no servidor? *(Rascunho: sim; os envios pendentes do perfil anterior ficam esperando ele voltar.)*

## Spec do `game`, nível 1 (`docs/specs/SPEC-game.md`, rascunho)
15. **Controles:** botão direito troca, `E` segura e solta, botão esquerdo arremessa, `R` reinicia a sala.
    Tudo bem? *(Suposição G1.)*
16. **Morte por queda:** cair a mais de 12 m/s (uns 7 m de altura) mata? *(Suposição G3, ajustar jogando.)*
17. **Ponto livre mais próximo:** procurar até 1,5 m de distância na troca normal? *(Suposição G2.)*
18. **Cores do contorno:** qual cor para "só posição", "só peso" e "só energia"? *(No nível 1 só aparecem
    branco = aceita tudo e cinza = travado.)*
19. **Pulo:** manter o pulo fisicamente correto (70 kg → 0,5 m) ou comparar com um pulo "de jogo"? *(Fica
    para você decidir jogando, como já estava anotado no banco de ideias.)*
20. **Arremesso nesta primeira versão:** com força fixa, e a barra de carregamento entra junto com o nível 5?
    *(Suposição G4.)*
