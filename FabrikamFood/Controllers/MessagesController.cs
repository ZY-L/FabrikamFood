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

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {

            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                //To use with personalizing messages for users | Remembers address |yet to implement shopping cart, name/gender
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                var userTxt = activity.Text;
                List<Food> foodMenu = await AzureManager.AzureManagerInstance.GetFood();
                List<Food> shopCart = new List<Food>();
                Food buyItem = null;
                string address = userData.GetProperty<string>("Address");
                Boolean check_address = false;

                if (userTxt.ToLower().Contains("clear"))
                {
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                }

                //TO DO: Add a first greeting - might only be implementable when using dialogs/when I upgrade to LUIS
                //Activity greeting = activity.CreateReply("Hello sir, you look hungry. The Fabrikam chefs are have prepared an assortment of dishes, would you like to view our menu?");
                //await connector.Conversations.ReplyToActivityAsync(greeting);

                //TO DO: Authenticate user, use this information to set personalized info for delivery status and address

                //Allow user to view and order meals
                if (userTxt.ToLower().Equals("view menu"))
                {
                    Activity menu = activity.CreateReply("Hello sir, here is the assortment of dishes the Fabrikam chefs have prepared today");
                    await connector.Conversations.ReplyToActivityAsync(menu);

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
                }

                //TO DO: Use Cognitive recommendations API for drinks

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
                        Food cartItem = null;
                        cartItem = shopCart.Find(item => item.id == buyItem.id);

                        if (cartItem != null){
                            cartItem.quantity += 1;
                        }
                        else
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
                            if(userData.GetProperty<string>("Address") != null)
                            {
                                Activity alreadyAddress = activity.CreateReply($"Excellent choice sir, shall I deliver it to {userData.GetProperty<string>("Address")}? If not, where to?");
                                await connector.Conversations.ReplyToActivityAsync(alreadyAddress);
                            }else
                            {
                                Activity reply = activity.CreateReply("Excellent choice sir, and to which address may I deliver the meal to?");
                                await connector.Conversations.ReplyToActivityAsync(reply);
                            }                          
                        }
                    }
                    else //This isn't working atm - not sure why, have also tested when quantity < 1
                    {
                        Activity reply = activity.CreateReply($"y greatest apologies sir, the chef's busy making more {buyItem.id} at the moment. Perhaps another dish may tickle your fancy?");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                }

                //TO DO: Check if delivery address is in Redmond using Google Geolocation API
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
                    foreach (AddressObject.Result entry in results)
                    {
                        //If any returned address is in Redmond, then validate the address and proceed
                        if (entry.formatted_address.Contains("Redmond"))
                        {
                            check_address = true;
                            userData.SetProperty<string>("Address", entry.formatted_address);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData); // Update user data to the bot 
                            userTxt = "yes";
                        }
                    }
                    if (!check_address)
                    {
                        Activity fail = activity.CreateReply("Apologies sir, I cannot deliver to that address.");
                        await connector.Conversations.ReplyToActivityAsync(fail);
                    }
                }
                
                //Proceed with payment from using current delivery address or newly inputted address
                if (check_address && userTxt.ToLower().Equals("yes"))
                {
                    Activity success = activity.CreateReply("");
                    success.Recipient = activity.From;
                    success.Type = "message";
                    success.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "https://raw.githubusercontent.com/ZY-L/FabrikamFood/master/FabrikamFood/Images/success.jpg"));

                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction creditButton = new CardAction()
                    {
                        Value = "purchase",
                        Type = "postBack",
                        Title = "Pay via credit card"
                    };
                    cardButtons.Add(creditButton);
                    CardAction paypalButton = new CardAction()
                    {
                        Value = "purchase",
                        Type = "postBack",
                        Title = "Pay via Paypal"
                    };
                    cardButtons.Add(paypalButton);
                    HeroCard plCard = new HeroCard()
                    {
                        Title = "Very good, sir.",
                        Subtitle = $"Your meal shall arrive at shortly, let us proceed with the payment.",
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    success.Attachments.Add(plAttachment);
                    var deliveryreply = await connector.Conversations.SendToConversationAsync(success);
                }
                
                //TO DO: Calculate delivery distance and hence time with Google Distance Matrix API

                //Stimulate pay and refresh for new transaction, TO DO: update DB and print receipt
                if (userTxt.ToLower().Equals("purchase"))
                {
                    Activity done = activity.CreateReply("Excellent sir, the purchase is complete! I shall notify you when the food arrives.");
                    await connector.Conversations.ReplyToActivityAsync(done);
                    check_address = false;
                    buyItem = null;
                    shopCart.Clear();
                    
                    /*
                    Activity replyToConversation = activity.CreateReply("");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();

                    List<ReceiptItem> receiptList = new List<ReceiptItem>();

                    double total = 0;

                    foreach (Food item in shopCart)
                    {
                        ReceiptItem products = new ReceiptItem()
                        {
                            Title = $"{item.id}",
                            Subtitle = null,
                            Text = null,
                            Image = new CardImage(url: item.image),
                            Price = $"{item.price}",
                            Quantity = $"{item.quantity}",
                            Tap = null
                        };
                        receiptList.Add(products);
                        total += item.price * item.quantity;
                    }
                    ReceiptCard plCard = new ReceiptCard()
                    {
                        Title = "Purchase complete!",
                        Items = receiptList,
                        Total = $"{total}",
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    var reply = await connector.Conversations.SendToConversationAsync(replyToConversation);
                    //Clear cart for future transactions
                    total = 0;
                    shopCart.Clear();
                    //}*/

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