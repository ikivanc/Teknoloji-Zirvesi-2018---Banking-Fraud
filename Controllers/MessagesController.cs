using System;
using System.Threading.Tasks;
using System.Web.Http;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Web.Http.Description;
using System.Net.Http;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using System.Net.Http.Headers;

namespace Microsoft.Bot.Sample.LuisBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        static readonly string APIKEY = "Your_Translator_API_KEY";

        static readonly string TRANSLATETO = "en";


        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [ResponseType(typeof(void))]
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            // check if activity is of type message
            if (activity.GetActivityType() == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                var input = activity.Text;

                //Prompt Choice Sorularından gelen soruları cevaplarını çevirmemesi için aşağıdaki kontrolü ekliyoruz
                if (activity.Text != "Evet" && activity.Text != "Hayir" && activity.Text != "Emin Degilim")
                {
                    Task.Run(async () =>
                    {
                        var accessToken = await GetAuthenticationToken(APIKEY);
                        var output = await Translate(input, TRANSLATETO, accessToken);
                        Console.WriteLine(output);
                        activity.Text = output;
                        await Conversation.SendAsync(activity, () => new BasicLuisDialog());
                    }).Wait();
                }
                else
                {
                    await Conversation.SendAsync(activity, () => new BasicLuisDialog());
                }

                //await Conversation.SendAsync(activity, () => new BasicLuisDialog());
            }
            else
            {
                username = await getUsernameViaFunction(activity.Recipient.Id);
                HandleSystemMessage(activity);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }


        static async Task<string> Translate(string textToTranslate, string language, string accessToken)
        {
            string url = "http://api.microsofttranslator.com/v2/Http.svc/Translate";
            string query = $"?text={System.Net.WebUtility.UrlEncode(textToTranslate)}&to={language}&contentType=text/plain";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.GetAsync(url + query);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return "ERROR: " + result;

                var translatedText = XElement.Parse(result).Value;
                return translatedText;
            }
        }

        static async Task<string> GetAuthenticationToken(string key)
        {
            string endpoint = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                var response = await client.PostAsync(endpoint, null);
                var token = await response.Content.ReadAsStringAsync();
                return token;
            }
        }


        string username = String.Empty;
      
        private  Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels

                IConversationUpdateActivity update = message;
                var client = new ConnectorClient(new Uri(message.ServiceUrl), new MicrosoftAppCredentials());
                if (update.MembersAdded != null && update.MembersAdded.Count == 1)
                {
                    foreach (var newMember in update.MembersAdded)
                    {
                        if (newMember.Id != message.Recipient.Id)
                        {
                            var reply = message.CreateReply();
                            reply.Text = $"Merhaba {username}!";
                            client.Conversations.ReplyToActivityAsync(reply);
                        }
                    }
                }
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

        // Logic Apps'e bağlanma
        // 
        public static async Task<string> getUsernameViaFunction(string botID)
        {
            string body = "{ \"name\":\"" + botID + "\"}";

            using (var client = new HttpClient())
            {
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("Your_Function_Endpoint", content);

                var result = await response.Content.ReadAsStringAsync();

                return result;
            }
        }
    }
}