using Microsoft.WindowsAzure.MobileServices;
using FabrikamFood.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FabrikamFood
{
    public class AzureManager
    {

        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<Food> foodMenu;
        private IMobileServiceTable<ShoppingCart> shopCart;

        private AzureManager()
        {
            this.client = new MobileServiceClient("https://fabrikambutler.azurewebsites.net/");
            this.foodMenu = this.client.GetTable<Food>();
            this.shopCart = this.client.GetTable<ShoppingCart>();
        }

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureManager();
                }

                return instance;
            }
        }

        public async Task<List<Food>> GetFood()
        {
            return await this.foodMenu.ToListAsync();
        }

    }
}
