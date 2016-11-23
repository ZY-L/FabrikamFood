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
using FabrikamFood.Models;

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
                Activity replyToConversation = activity.CreateReply("Should go to conversation, sign-in card");
                replyToConversation.Recipient = activity.From;
                replyToConversation.Type = "message";
                replyToConversation.Attachments = new List<Attachment>();
                List<CardAction> cardButtons = new List<CardAction>();
                
                SigninCard plCard = new SigninCard(text: "You need to authorize me", buttons: cardButtons);
                Attachment plAttachment = plCard.ToAttachment();
                replyToConversation.Attachments.Add(plAttachment);
                var reply = await connector.Conversations.SendToConversationAsync(replyToConversation);

                //Allow user to view and order meals
                if (userTxt.ToLower().Equals("view menu"))
                {

                    replyToConversation = activity.CreateReply("");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToConversation.Attachments = new List<Attachment>();
                    List<Item> food = new List<Item>();
                    food.Add(new Item("Southwest Steak & Salad",
                                      "Hand-sliced ethical choice top sirloin with corn, black bean & red pepper salsa, mixed greens and crispy tortilla strips with our light cilantro ranch dressing.",
                                      "https://raw.githubusercontent.com/ZY-L/FabrikamFood/Menu/FabrikamFood/Images/Steak.jpg",
                                      10.50));
                    food.Add(new Item("Beef & Mushroom Bowl",
                                      "Marinated Meyer Natural Angus beef with button mushrooms, peas, roasted carrots, brown rice, bread crumbs, and our velvety potato mushroom sauce.",
                                      "https://raw.githubusercontent.com/ZY-L/FabrikamFood/Menu/FabrikamFood/Images/BeefMushroom.jpg",
                                      9.50));
                    food.Add(new Item("Mediterrenean Salad",
                                      "Chickpeas, cucumber, roasted red pepper, kalamata olives, whole grain croutons, and crumbled feta over romaine with our Mediterranean vinaigrette.",
                                      "https://raw.githubusercontent.com/ZY-L/FabrikamFood/Menu/FabrikamFood/Images/Salad.jpg",
                                      8.00));
                    food.Add(new Item("Marinated Chicken & Veggie Kabobs",
                                      "Grilled skewers with natural chicken breast, red pepper, zucchini, and red onion in our Mediterranean marinade. Served over carrot and chickpea bulgur wheat with sundried tomato dressing.",
                                      "https://raw.githubusercontent.com/ZY-L/FabrikamFood/Menu/FabrikamFood/Images/Skewers.jpg",
                                      6.50));
                    foreach (Item product in food)
                    {
                        List<CardImage> cardImages = new List<CardImage>();
                        cardImages.Add(new CardImage(url: product.image));
                        List<CardAction> menuButtons = new List<CardAction>();
                        CardAction buyButton = new CardAction()
                        {
                            Value = $"Buy {product.id}",
                            Type = "postBack",
                            Title = $"Buy for ${product.price}"
                        };
                        menuButtons.Add(buyButton);
                        HeroCard itemCard = new HeroCard()
                        {
                            Title = $"{product.id}",
                            Subtitle = $"{product.description}",
                            Images = cardImages,
                            Buttons = menuButtons
                        };
                        plAttachment = plCard.ToAttachment();
                        replyToConversation.Attachments.Add(plAttachment);
                    }
                    replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    var menureply = await connector.Conversations.SendToConversationAsync(replyToConversation);
      
                    return Request.CreateResponse(HttpStatusCode.OK);

                }

                //Use Cognitive recommendations API for drinks

                //Get user delivery address and use Maps API to check if in Redmond

                //Stimulate pay, update DB and print receipt




                Activity replylast = activity.CreateReply($"You sent {userTxt}");
                await connector.Conversations.ReplyToActivityAsync(replylast
                    );
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