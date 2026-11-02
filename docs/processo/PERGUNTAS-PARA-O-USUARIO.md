# Perguntas para o usuário

Dúvidas que só você pode responder, anotadas pelo loop autônomo. Em cada uma eu segui com a suposição mais
simples e fácil de desfazer (entre parênteses). Responda quando puder; nada aqui travou o trabalho.

## Spec do `profiles` (`docs/specs/SPEC-profiles.md`, rascunho)
1. **Nomes com acento e espaço** (ex.: `João`, `Ana Maria`) podem? *(Suposição A1: não. Só letras sem
   acento, números, `_` e `-`, de 3 a 16 caracteres, para evitar nomes parecidos no ranking.)*
2. **Ícones:** quantos e quais desenhos? *(Suposição A2: 12 ícones, `icon-01` a `icon-12`; os desenhos
   ficam para a spec do `game`.)*
3. **Sessão e bloqueio:** a sessão vale 30 dias e o perfil fica bloqueado 15 minutos depois de 5 códigos
   errados seguidos. Tudo bem? *(Suposições A4 e A5.)*
4. **Onde a API vai rodar na apresentação:** no seu PC ou num servidor na internet? Isso decide HTTPS e
   CORS. *(Por enquanto: no PC, `dotnet run`; a revisão de segurança do item 3 trata disso.)*
5. **Apagar o perfil ou trocar o nome:** o jogador pode? *(Suposição A10: não nesta versão.)*

## Spec do `progress` (`docs/specs/SPEC-progress.md`, rascunho)
6. **Mortes no ranking:** mostrar o número de mortes, ou uma marca para quem terminou sem morrer?
   *(Suposição B7: as mortes e os reinícios de sala são guardados, mas não aparecem nem contam no ranking.)*
7. **Tempo mínimo plausível:** 1 s por sala basta, ou cada nível deve ter um tempo de referência no JSON?
   *(Suposição B4: 1 s por sala e no máximo 24 h.)*
8. **Tamanho do ranking no jogo:** os 20 primeiros, ou os 10 primeiros mais a sua posição?
   *(A API devolve até 100, padrão 20, e tem uma rota com a sua posição.)*

## Revisão de segurança das specs da API (item 3)
9. **Bloqueio usado para atrapalhar:** como os nomes aparecem no ranking, qualquer pessoa pode errar o
   código 5 vezes e bloquear o seu nome por 15 minutos num PC novo (no PC em que você já jogou a sessão
   continua valendo). Aceita, ou contar os erros por perfil **e** por IP? *(Suposição: aceitar; o código
   de 80 bits já torna a força bruta impossível, o bloqueio é só uma camada a mais.)*
10. **Resultado falso:** sem o servidor refazer a partida, um jogo modificado pode mandar um tempo bom
    mas plausível. Tudo bem só checar a plausibilidade e apagar à mão um resultado trapaceado?
    *(Suposição B9: sim, risco aceito e escrito na spec.)*
11. **Gerar código novo pede o código atual:** para quem roubar o token não conseguir tomar o perfil.
    O jogo manda o código do save, então você não digita nada. Tudo bem? *(Suposição A11: sim.)*

## Spec do `sync` (`docs/specs/SPEC-sync.md`, rascunho)
12. **Código e token no save sem criptografia:** ficam em texto no arquivo do seu PC (`user://save.json`).
    Tudo bem? *(Suposição C1: sim. Criptografar com o DPAPI do Windows só funcionaria no Windows.)*
13. **Jogar sem perfil:** pode jogar antes de criar um perfil, e os resultados são enviados quando você
    criar ou entrar num? *(Suposição C3: sim.)*
14. **Trocar de perfil no mesmo PC:** os melhores resultados locais do perfil anterior somem da tela e
    ficam só no servidor? *(Rascunho: sim; os envios pendentes do perfil anterior ficam esperando ele voltar.)*
