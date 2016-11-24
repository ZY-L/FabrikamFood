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

                //Check if delivery address is in Redmond using Google Geolocation API
                if (char.IsDigit(userTxt[0]))
                {
                    List<String> addressElements = userTxt.Split(' ').ToList();
                    string uri = "";
                    foreach(String element in addressElements)
                    {
                        uri += element;
                        uri += "+";
                    }
                    uri = uri.TrimEnd('+');

                    AddressObject.RootObject root;
                    HttpClient client = new HttpClient();
                    string x = await client.GetStringAsync(new Uri("https://maps.googleapis.com/maps/api/geocode/json?address=" + uri + "&key=AIzaSyBkKQAw4mvqPpJvG-aYo5usC8G0P8AkS28"));
                    root = JsonConvert.DeserializeObject<AddressObject.RootObject>(x);

                    List<AddressObject.Result> results = root.results;
                    Boolean check_address = false;
                    int index = 0;
                    foreach(AddressObject.Result entry in results)
                    {
                        check_address = entry.formatted_address.Contains("Redmond");
                        index += 1;
                    }
                       
                    //Activity reply2 = activity.CreateReply($"You've just entered a {check_address} address {index}");
                    //await connector.Conversations.ReplyToActivityAsync(reply2);

                    if (check_address)
                    {
                        Activity success = activity.CreateReply("");
                        success.Recipient = activity.From;
                        success.Type = "message";
                        success.Attachments = new List<Attachment>();
                        List<CardImage> cardImages = new List<CardImage>();
                        cardImages.Add(new CardImage(url: "https://raw.githubusercontent.com/ZY-L/FabrikamFood/master/FabrikamFood/Images/success.jpg"));
                       
                        List<CardAction> cardButtons = new List<CardAction>();
                        CardAction plButton = new CardAction()
                        {
                            Value = "purchase",
                            Type = "postBack",
                            Title = "Pay via credit card"
                        };
                        cardButtons.Add(plButton);
                        HeroCard plCard = new HeroCard()
                        {
                            Title = "Very good, sir.",
                            Subtitle = "Your meal shall arrive shortly, let us proceed with the payment.",
                            Images = cardImages,
                            Buttons = cardButtons
                        };
                        Attachment plAttachment = plCard.ToAttachment();
                        success.Attachments.Add(plAttachment);
                        var deliveryreply = await connector.Conversations.SendToConversationAsync(success);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    else
                    {
                        Activity fail = activity.CreateReply("Apologies sir, I cannot deliver to that address.");
                        await connector.Conversations.ReplyToActivityAsync(fail);
                    }


                    

                    //AddressObject.Location city;
                    //HttpClient client = new HttpClient();
                    //string x = await client.GetStringAsync(new Uri("https://maps.googleapis.com/maps/api/geocode/json?address=" + address + "&key=AIzaSyBkKQAw4mvqPpJvG-aYo5usC8G0P8AkS28"));
                    //city = JsonConvert.DeserializeObject<AddressObject.Location>(x);

                }

                //Calculate delivery distance and hence time with Google Distance Matrix API


                //Stimulate pay, update DB and print receipt

                //Remember to add checks for if address valid and stuff in shopping cart

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