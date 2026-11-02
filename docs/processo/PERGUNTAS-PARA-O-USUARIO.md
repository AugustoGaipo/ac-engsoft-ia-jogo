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
