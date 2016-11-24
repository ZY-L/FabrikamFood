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
using FabrikamFood.DataModels;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

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
                List<Food> foodMenu = await AzureManager.AzureManagerInstance.GetFood();
                List<Food> shopCart = new List<Food>();
                Food buyItem = null;

                //To use with personalizing messages for users
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                //Authenticate user, use this information to set personalized info for delivery status and address

                //Allow user to view and order meals
                if (userTxt.ToLower().Equals("view menu"))
                {
                    Activity replyToConversation = activity.CreateReply("");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToConversation.Attachments = new List<Attachment>();

                    foreach (Food product in foodMenu)
                    {
                        List<CardImage> cardImages = new List<CardImage>();
                        cardImages.Add(new CardImage(url: product.image));
                        List<CardAction> menuButtons = new List<CardAction>();
                        CardAction buyButton = new CardAction()
                        {
                            Value = $"{product.id}",
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
                        Attachment plAttachment = itemCard.ToAttachment();
                        replyToConversation.Attachments.Add(plAttachment);
                    }
                    replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    var menureply = await connector.Conversations.SendToConversationAsync(replyToConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);

                }

                //Use Cognitive recommendations API for drinks

                //Check if dish available, save to shopping cart and get user delivery address

                if (userTxt.ToLower().Equals("southwest steak & salad")||
                    userTxt.ToLower().Equals("beef & mushroom bowl")||
                    userTxt.ToLower().Equals("mediteranean salad")||
                    userTxt.ToLower().Equals("marinated chicken & veggie kabobs")) //This would be simplified with LUIS
                {
                    foreach (Food product in foodMenu)
                    { 
                        if (userTxt.Equals(product.id))
                        {
                            buyItem = product;
                        }
                    }
                    if(buyItem.quantity >= 1)
                    {
                        Activity reply = activity.CreateReply("Excellent choice sir, and to which address may I deliver the meal to?");
                        await connector.Conversations.ReplyToActivityAsync(reply);

                        Food cartItem = null;
                        cartItem = shopCart.Find(item => item.id == buyItem.id);

                        if (cartItem != null){
                            cartItem.quantity += 1;
                        }else
                        {
                            Food order = new Food()
                            {
                                id = buyItem.id,
                                price = buyItem.price,
                                quantity = buyItem.quantity,
                                image = buyItem.image,
                                description = buyItem.description
                            };
                            shopCart.Add(order); //Could also save cart to another database but not sure how efficient this would be
                        }

                    }else //This isn't working atm - not sure why, have also tested when quantity < 1
                    {
                        Activity reply = activity.CreateReply($"y greatest apologies sir, the chef's busy making more {buyItem.id} at the moment. Perhaps another dish may tickle your fancy?");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                }

                //Check if deliver address is in Redmond using Maps API


                //Stimulate pay, update DB and print receipt

                if (userTxt.ToLower().Equals("purchase"))
                {
                    
                }

                
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