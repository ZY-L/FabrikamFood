using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FabrikamFood
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                var userTxt = activity.Text;

                //To use with personalizing messages for users
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                //Authenticate user, use this information to set personalized info for delivery status and address

                //Allow user to view and order meals
                //Creating cards for the first time to give better visual output
                if (userTxt.ToLower().Equals("view menu"))
                {

                    Activity replyToConversation = activity.CreateReply("");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToConversation.Attachments = new List<Attachment>();
                    Dictionary<string, string> cardImageList = new Dictionary<string, string>();
                    cardImageList.Add("Southwest Steak & Salad", "https://raw.githubusercontent.com/ZY-L/FabrikamFood/Menu/FabrikamFood/Images/Steak.jpg");
                    cardImageList.Add("Beef & Mushroom Bowl", "https://raw.githubusercontent.com/ZY-L/FabrikamFood/Menu/FabrikamFood/Images/BeefMushroom.jpg");
                    cardImageList.Add("Mediterrenean Salad", "https://raw.githubusercontent.com/ZY-L/FabrikamFood/Menu/FabrikamFood/Images/Salad.jpg");
                    cardImageList.Add("Marinated Chicken & Veggie Kabobs", "https://raw.githubusercontent.com/ZY-L/FabrikamFood/Menu/FabrikamFood/Images/Skewers.jpg");
                    Dictionary<string, string> cardTextList = new Dictionary<string, string>();
                    cardTextList.Add("Southwest Steak & Salad", "Hand-sliced ethical choice top sirloin with corn, black bean & red pepper salsa, mixed greens and crispy tortilla strips with our light cilantro ranch dressing.");
                    cardTextList.Add("Beef & Mushroom Bowl", "Marinated Meyer Natural Angus beef with button mushrooms, peas, roasted carrots, brown rice, bread crumbs, and our velvety potato mushroom sauce.");
                    cardTextList.Add("Mediterrenean Salad", "Chickpeas, cucumber, roasted red pepper, kalamata olives, whole grain croutons, and crumbled feta over romaine with our Mediterranean vinaigrette.");
                    cardTextList.Add("Marinated Chicken & Veggie Kabobs", "Grilled skewers with natural chicken breast, red pepper, zucchini, and red onion in our Mediterranean marinade. Served over carrot and chickpea bulgur wheat with sundried tomato dressing.");
                    foreach (KeyValuePair<string, string> cardContent in cardImageList)
                    {
                        List<CardImage> cardImages = new List<CardImage>();
                        cardImages.Add(new CardImage(url: cardContent.Value));
                        List<CardAction> cardButtons = new List<CardAction>();
                        CardAction plButton = new CardAction()
                        {
                            Value = $"https://en.wikipedia.org/wiki/{cardContent.Key}",
                            Type = "openUrl",
                            Title = "WikiPedia Page"
                        };
                        cardButtons.Add(plButton);
                        HeroCard plCard = new HeroCard()
                        {
                            Title = $"{cardContent.Key}",
                            Subtitle = $"{cardTextList[cardContent.Key]}",
                            Images = cardImages,
                            Buttons = cardButtons
                        };
                        Attachment plAttachment = plCard.ToAttachment();
                        replyToConversation.Attachments.Add(plAttachment);
                    }
                    replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    var reply2 = await connector.Conversations.SendToConversationAsync(replyToConversation);
      
                    return Request.CreateResponse(HttpStatusCode.OK);

                }

                //Use Cognitive recommendations API for drinks

                //Get user delivery address and use Maps API to check if in Redmond

                //Stimulate pay and update DB

                Activity reply = activity.CreateReply($"You sent {userTxt}");
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
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
    }
}