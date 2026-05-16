using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Tweetinvi.Core.Models.Properties;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace VeraciBot
{
    internal class Phrases
    {

        public static string[] notAuthorizedResponse = {
            "Quem é você? Você não é ninguém! Não está autorizado a me chamar. Volte quando estiver nos inquéritos do fim do mundo. Aqui só com convite! Para mais detahes, veja https://veraci.bot.",
            "Quem é você mesmo? Ah, é verdade… ninguém. Não está autorizado a me chamar. Volte quando estiver nos inquéritos do fim do mundo. Aqui só entra com convite. Para mais detalhes, veja https://veraci.bot.",
            "Desculpa, você é quem? Porque até onde consta, ninguém. Não tem autorização para esse contato. Tente novamente quando aparecer nos inquéritos do fim do mundo. Por aqui, só com convite. Mais informações em https://veraci.bot.",
            "Quem você acha que é? Spoiler: ninguém. Não está autorizado a me ligar. Volte quando fizer parte dos inquéritos do fim do mundo. Este lugar não é de livre acesso — só com convite. Veja https://veraci.bot.",
            "Quem é você para me chamar? Ninguém importante, pelo visto. Não está autorizado a esse tipo de contato. Retorne quando estiver nos inquéritos do fim do mundo. Aqui, entrada apenas mediante convite. Detalhes em https://veraci.bot.",
            "Você é quem mesmo? Porque, oficialmente, ninguém. Não está autorizado a entrar em contato comigo. Volte quando alcançar os inquéritos do fim do mundo. Até lá, acesso somente por convite. Mais detalhes em https://veraci.bot."
        };

        public static async Task<string> GetTranslatePhraseFromArray(string[] array, string lang)
        {
            Random rnd = new Random();
            int index = rnd.Next(array.Length);
            return await OpenAIAPI.TranslatePhrase(array[index], lang);
        }

        public static async Task<string> GetNotAuthorizedResponseAsync(string lang)
        {
            return await GetTranslatePhraseFromArray(notAuthorizedResponse, lang);
        }

        public static string[] failedToUnderstandResponse = {
            "Não entedi o que você quer. Tome cuidado que qualquer erro são mais 17 anos de cadeia. Se quiser ajuda, peça ajuda.",
            "Não faço a menor ideia do que você quer. Só um lembrete amigável: qualquer erro rende mais 14 anos de cadeia. Se precisar de ajuda, tente pedir ajuda.",
            "Confuso sobre o que você está tentando fazer. Mas fique tranquilo — cada errinho soma mais 27 anos de prisão. Se quiser ajuda, peça ajuda (é uma opção).",
            "Não entendi absolutamente nada do que você quer. Apenas cuidado: qualquer deslize adiciona mais 21 anos atrás das grades. Se precisar de ajuda, talvez seja uma boa ideia pedir ajuda.",
            "O que exatamente você quer? Porque não ficou claro. Só atenção: qualquer erro custa mais 17 anos de cadeia. Se quiser ajuda, pedir ajuda costuma funcionar.",
            "Não entendi seu objetivo. Mas vá com calma — qualquer erro resulta em mais 14 anos de prisão. Se precisar de ajuda, pedir ajuda é sempre uma alternativa."
        };

        public static async Task<string> GetFailedToUnderstandResponseAsync(string lang)
        {
            return await GetTranslatePhraseFromArray(failedToUnderstandResponse, lang);
        }

        public static string failedToUnderstandAcceptResponse = "Não entedi o que você quer. Você tem que aceitar ou recusar o convite antes de mais nada. Se quiser ajuda, peça ajuda.";

        public static async Task<string> GetFailedToUnderstandAcceptResponseAsync(string lang)
        {
            return await OpenAIAPI.TranslatePhrase(failedToUnderstandAcceptResponse, lang);
        }

        public static string helpResponse = "Esse é o VERACIBOT, seu robô para verificação de fatos aprovado pelo Ministério da Verdade. Com o uso desse robô, você pode dedurar outros pequenos tiraninhos que ficam atacando nossa suprema democracia com fake-news. Com isso, talvez, conseguir uma dosimetria de pena para você ou ferrar outras pessoas de graça. Para participar do jogo você precisa ser convidado por alguém que já está jogando. Para saber mais detalhes, veja https://veraci.bot.";

        public static async Task<string> GetHelpResponseAsync(string lang)
        {
            return await OpenAIAPI.TranslatePhrase(helpResponse, lang);
        }

        public static string[] scoreResponse = {
            "Como ousa me incomodar com seus pedidos insignificantes, seu tiraninho! Isso é um ataque a nossa democracia! Seu processo é sigiloso... mas uma jornalista vazou umas informações:",
            "Como você ousa me incomodar com esses pedidos patéticos, seu pequeno tirano? Isto é claramente um atentado à nossa democracia! O processo é sigiloso… mas curiosamente uma jornalista deixou escapar algumas informações.",
            "Você realmente achou aceitável me importunar com pedidos tão irrelevantes, seu tiraninho? Isso é um ataque frontal à democracia! O processo é confidencial — embora uma jornalista tenha resolvido vazar alguns detalhes.",
            "Que audácia a sua me perturbar com demandas tão insignificantes, seu aspirante a tirano! Estamos diante de um grave ataque à democracia. O processo é sigiloso… apesar de uma jornalista ter divulgado certas informações.",
            "Você ousa interromper meu dia com pedidos tão medíocres, seu tiraninho de estimação? Isto é um atentado contra a democracia! O processo corre em sigilo, mas uma jornalista misteriosamente vazou algumas coisas.",
            "Como se atreve a me incomodar com esses pedidos ridículos, seu mini-tirano? Isso é claramente um ataque à democracia! O processo é secreto… embora uma jornalista tenha decidido contar uns detalhes por aí."
        };

        public static async Task<string> GetScoreResponseAsync(string lang)
        {
            return await GetTranslatePhraseFromArray(scoreResponse, lang);
        }

        public static string[] scoreBoardResponse = {
            "A tabela dos maiores pequenos tiranos mostra a degradação da nossa suprema democracia! É preciso prender mais gente para civilizar esse povinho! Nós do Ministério da Verdade estamos sempre prontos para impor nossa superioridade moral a vocês pequenos tiranos! Veja os piores:",
            "A lista dos maiores pequenos tiranos escancara a decadência da nossa gloriosa democracia suprema! Precisamos prender ainda mais gente para educar esse povinho! Nós, do Ministério da Verdade, estamos sempre prontos para impor nossa incontestável superioridade moral. Confiram os piores:",
            "A tabela dos pequenos grandes tiranos é a prova viva da degradação da nossa excelentíssima democracia! Só resta prender mais alguns para civilizar essa gente. Aqui no Ministério da Verdade, fazemos questão de impor nossa elevada moralidade. Vejam os destaques negativos:",
            "O ranking dos pequenos tiranos revela o colapso moral da nossa democracia suprema! A solução? Prender mais pessoas para ensinar bons modos a esse povinho. Nós, do Ministério da Verdade, seguimos firmes impondo nossa superioridade moral. Observem os piores casos:",
            "Essa tabela dos maiores pequenos tiranos demonstra como nossa democracia está apodrecendo! Está na hora de prender mais gente e civilizar essa massa inconveniente. O Ministério da Verdade, como sempre, está aqui para impor sua moral superior. Acompanhem os piores:",
            "O levantamento dos pequenos tiranos deixa claro o estado lastimável da nossa suprema democracia! Nada que mais prisões não resolvam para civilizar esse povinho. Nós, do Ministério da Verdade, permanecemos prontos para impor nossa moral elevada. Veja quem lidera o ranking:"
        };

        public static async Task<string> GetScoreBoardResponseAsync(string lang)
        {
            return await GetTranslatePhraseFromArray(scoreBoardResponse, lang);
        }

        public static string[] inviteResponse = {
            "Você foi apontado como um dos milhões de tiraninhos que tem mania de agredir a democracia suprema com fake news. Sua pena é de 17 anos de cadeia sem anistia. Mas oferecemos a possibilidade de delação premiada. Você aceita participar desse jogo? Responda que aceita para entrar para o clube. Conheça o VERACIBOT em https://veraci.bot.",
            "Você foi gentilmente identificado como um dos milhões de tiraninhos obcecados em atacar a nossa democracia suprema com fake news. A pena padrão é de 17 anos de cadeia, sem anistia — mas somos generosos e oferecemos a opção de delação premiada. Topa entrar nesse jogo? Responda que aceita para entrar para o clube. Saiba mais sobre o VERACIBOT em https://veraci.bot.",
            "Parabéns! Você acaba de ser apontado como mais um dos milhões de pequenos tiranos que adoram agredir a democracia suprema usando fake news. Sua recompensa: 17 anos de prisão, sem direito a anistia. Mas calma, existe a opção de delação premiada. Aceita jogar? Responda que aceita para entrar para o clube. Descubra como funciona o VERACIBOT em https://veraci.bot.",
            "Você entrou oficialmente para a lista dos milhões de tiraninhos acusados de ferir a democracia suprema com fake news. A punição é simples: 17 anos de cadeia, sem anistia. Em contrapartida, oferecemos a elegante alternativa da delação premiada. Quer participar do jogo? Responda que aceita para entrar para o clube. Saiba mais em https://veraci.bot.",
            "Segundo nossos critérios impecáveis, você é um dos milhões de tiraninhos que insistem em atacar a democracia suprema com fake news. A sentença prevista é de 17 anos de prisão, sem qualquer chance de anistia. Porém, como somos magnânimos, há a opção de delação premiada. Aceita jogar? Responda que aceita para entrar para o clube. Conheça o VERACIBOT em https://veraci.bot.",
            "Você foi selecionado entre milhões de tiraninhos acusados de agredir a democracia suprema por meio de fake news. A pena é clara: 17 anos de cadeia, sem anistia. Mas há esperança — oferecemos a possibilidade de delação premiada. Quer entrar nesse jogo? Responda que aceita para entrar para o clube. Entenda como funciona o VERACIBOT em https://veraci.bot."
        };

        public static async Task<string> GetInviteResponseAsync(string lang)
        {
            return await GetTranslatePhraseFromArray(inviteResponse, lang);
        }

        public static string inviteErrorResponse = "Esse tiraninho já foi convidado para o inquérito do fim do mundo! Ou já está jogando o jogo ou estamos lidando com ele de outra forma. Escolha outra vítima para suas ambições maquiavélicas!";

        public static async Task<string> GetInviteErrorResponseAsync(string lang)
        {
            return await OpenAIAPI.TranslatePhrase(inviteErrorResponse, lang);
        }

        public static string inviteNoUserResponse = "Para convidar alguém, você precisa marcar o usuário que quer convidar, além do VERACIBOT. Não abuse da minha paciência suprema. Qualquer coisa é mais 14 anos de cadeia.";

        public static async Task<string> GetInviteNoUserResponseAsync(string lang)
        {
            return await OpenAIAPI.TranslatePhrase(inviteNoUserResponse, lang);
        }

        public static string[] acceptResponse = {
            "Você aceitou o convite e confirmou que é mesmo um tiraninho que ataca nossa suprema democracia com fake-news. Eu não precisava de provas antes e preciso menos ainda agora. Agora sua função é dedurar os outros para tentar reduzir sua pena. Boa sorte no jogo.",
            "Bem vindo ao clube! Você confirmou oficialmente que é mais um tiraninho dedicado a atacar nossa suprema democracia com fake news. Provas nunca foram necessárias — e agora, menos ainda. Sua nova missão é dedurar os outros para tentar aliviar a própria pena. Boa sorte no jogo.",
            "Quem bom que você confessou, fica registrado que você é, sem dúvida alguma, um tiraninho inimigo da democracia suprema. Não precisei de provas antes e definitivamente não preciso agora. A partir de hoje, sua função é entregar os colegas para ver se reduz a pena. Boa sorte jogando.",
            "Aceitar este convite equivale a assumir que você é um tiraninho profissional no ataque à nossa democracia suprema via fake news. Provas são um detalhe irrelevante — ontem e hoje. Agora trate de dedurar os outros se quiser alguma chance de reduzir a pena. Boa sorte no jogo.",
            "Parabéns! Você aceitou o convite e confirmou que é um tiraninho comprometido em agredir a democracia suprema com fake news. Nunca precisei de provas, e agora muito menos. Sua nova ocupação é delatar os demais para tentar diminuir a pena. Boa sorte nessa partida.",
            "Ao aceitar este convite, você sela seu destino como tiraninho atacante da democracia suprema. Provas sempre foram opcionais e agora são totalmente dispensáveis. Sua tarefa é simples: dedurar os outros e torcer por uma redução de pena. Boa sorte no jogo."
        };

        public static async Task<string> GetAcceptResponseAsync(string lang)
        {
            return await GetTranslatePhraseFromArray(acceptResponse, lang);
        }

        public static string[] refuseResponse ={
            "Não ache que você vai escapar da minha perseguição apenas por negar participar no nosso jogo. Seu nome continua sendo discutido em nossos grupos de WhatsApp. Tchau. Por equanto.",
            "Não pense que negar participar do nosso joguinho vai te livrar da minha perseguição. Seu nome segue firme e forte nos nossos grupos de WhatsApp. Tchau. Por enquanto.",
            "Acha mesmo que dizer não ao jogo vai fazer tudo desaparecer? Spoiler: não vai. Seu nome continua circulando animadamente nos nossos grupos de WhatsApp. Tchau. Por enquanto.",
            "Recusar o convite não significa escapar da minha atenção. Seu nome ainda rende ótimas discussões nos nossos grupos de WhatsApp. Até logo. Por enquanto.",
            "Você pode até negar participação, mas não confunda isso com liberdade. Seu nome segue sendo pauta recorrente nos nossos grupos de WhatsApp. Tchau. Por enquanto.",
            "Negar o jogo não te salva da perseguição — só muda o ritmo. Seu nome continua bem vivo nos nossos grupos de WhatsApp. Tchau. Por enquanto."
        };

        public static async Task<string> GetRefuseResponseAsync(string lang)
        {
            return await GetTranslatePhraseFromArray(refuseResponse, lang);
        }

        static string[] resp = {
            "Vá imediatamente para a cadeia, seu bolsonarista fazedor de fakenews. 17 anos de cadeia imediatamente!",
            "Procure a carmen lucia para iniciar o curso de democracia relativa do tse e se prepara pra visita do Uber Black da PF",
            "Ok, vou deixar passar essa só com 14 anos de cadeia, na próxima vai ser punido de verdade",
            "Parabens por ser um gado muito obediente e só falar a verdade aprovada pelo sistema",
            "Ahhrá... agora sim, tudo certo... ganhou TROFEU DEMOCRACIA RELATIVA do XANDÃO"
        };

    }
}
