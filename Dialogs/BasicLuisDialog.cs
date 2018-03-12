using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;

namespace Microsoft.Bot.Sample.LuisBot
{
    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [Serializable]
    public class BasicLuisDialog : LuisDialog<object>
    {
        public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(
            ConfigurationManager.AppSettings["LuisAppId"], 
            ConfigurationManager.AppSettings["LuisAPIKey"], 
            domain: ConfigurationManager.AppSettings["LuisAPIHostName"])))
        {
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await this.ShowLuisResult(context, result);
        }

        [LuisIntent("Greetings")]
        public async Task Greetings(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Merhaba ben bankacılık dijital asistanınızım. 🤖");      

            PromptDialog.Choice(context, this.OnOptionAlisverisSelected, new List<string>() { "Evet", "Hayir", "Emin Degilim" }, "Son 1 Haftada 1000TL üzeri alışveriş yaptınız mı?", "Lütfen var olan seçenekleri seçiniz", 3);
        }

        // Go to https://luis.ai and create a new intent, then train/publish your luis app.
        // Finally replace "Gretting" with the name of your newly created intent in the following handler
        [LuisIntent("IssueDetected")]
        public async Task IssueDetected(IDialogContext context, LuisResult result)
        {
            //await this.ShowLuisResult(context, result);

            PromptDialog.Choice(context, this.OnOptionAlisverisSelected, new List<string>() { "Evet", "Hayir","Emin Degilim"}, "Son 1 Haftada 1000TL üzeri alışveriş yaptınız mı?", "Lütfen var olan seçenekleri seçiniz", 3);

        }

        private async Task OnOptionAlisverisSelected(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                string optionSelected = await result;

                switch (optionSelected)
                {
                    case "Evet":
                        await context.PostAsync($"Evet seçeneği bu örnek için eklenmedi");
                        break;
                    case "Hayir":
                        PromptDialog.Choice(context, this.OnOptionFraudSelected, new List<string>() { "Evet", "Hayir" }, "10 Şubat 2017 Tarihinde Dahlia Lounge Nevada'daki 2,482TL alışveriş size mi aitti?", "Lütfen var olan seçenekleri seçiniz", 3);
                        break;
                    case "Emin Degilim":
                        await context.PostAsync($"Emin değilim seçeneği bu örnek için eklenmedi");
                        break;
                }

            }
            catch (TooManyAttemptsException ex)
            {
                await context.PostAsync($"Ooops! Too many attemps :(. But don't worry, I'm handling that exception and you can try again!");
            }
        }

        private async Task OnOptionFraudSelected(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                string optionSelected = await result;

                switch (optionSelected)
                {
                    case "Evet":
                        await context.PostAsync($"'Evet' seçeneği bu örnek için eklenmedi");
                        break;
                    case "Hayir":
                        PromptDialog.Choice(context, this.OnOptionFinalizeSelected, new List<string>() { "Evet", "Hayir" }, "Kredi Kartınızı dondurmak ister misiniz?", "Lütfen var olan seçenekleri seçiniz", 3);
                        break;
                    case "Emin Degilim":
                        await context.PostAsync($"Hayır seçeneği bu örnek için eklenmedi");
                        break;
                }

            }
            catch (TooManyAttemptsException ex)
            {
                await context.PostAsync($"Ooops! Too many attemps :(. But don't worry, I'm handling that exception and you can try again!");
            }
        }

        private async Task OnOptionFinalizeSelected(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                string optionSelected = await result;

                switch (optionSelected)
                {
                    case "Evet":
                        await context.PostAsync(optionSelected);
                        await context.PostAsync($"İşleminiz gerçekleştiriliyor");
                        postHttp(optionSelected);
                        await context.PostAsync($"Kartınızın durumu mail olarak sizlere iletildi.");
                        break;
                    case "Hayir":
                        await context.PostAsync($"'Hayir' seçeneği bu örnek için eklenmedi");
                        break;
                }
            }
            catch (TooManyAttemptsException ex)
            {
                await context.PostAsync($"Ooops! Too many attemps :(. But don't worry, I'm handling that exception and you can try again!");

            }
        }

        // Logic Apps'e bağlanma
        // 
        static void postHttp(string processName)
        {
            string jsonStr = "{ \"process\":\"" + processName + "\"}";

            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
                var response = client.PostAsync("https://prod-34.westeurope.logic.azure.com:443/workflows/95a5fda3327b40f880a8ab196ffa469d/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=Wc3oGPU5DqKorhKT2hGI4itCNoNMzHBzWpfS0Xnbn8g", content);

                var result = response.Result;
            }
        }

        [LuisIntent("Cancel")]
        public async Task CancelIntent(IDialogContext context, LuisResult result)
        {
            await this.ShowLuisResult(context, result);
        }

        [LuisIntent("Help")]
        public async Task HelpIntent(IDialogContext context, LuisResult result)
        {
            await this.ShowLuisResult(context, result);
        }

        private async Task ShowLuisResult(IDialogContext context, LuisResult result) 
        {
            await context.PostAsync($"You have reached {result.Intents[0].Intent}. You said: {result.Query}");
            context.Wait(MessageReceived);
        }
    }
}