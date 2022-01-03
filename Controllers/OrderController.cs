using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SampleAspNetCore.Models;
using nimbbl.checkout;
using HomeController;

namespace SampleAspNetCore.Controllers
{
    public class OrderController : Controller
    {
        NimbblClient client = new NimbblClient("https://api.nimbbl.tech/api/'", "access_key_pKx7rdG51VWgy3q2", "access_secret_Wy7VPWryBYm9D3pw");
            var token = await client.GenerateToken();
            var order = await client.CreateOrder(new Order{
                Invoice_Id = "1234",
                Amount_Before_Tax = 4,
                Currency = "INR",
                Device_User_Agent = "Nimbbl DotNet Web SDK",
                Order_From_Ip="x.x.x.x",
                Tax=0,
                Total_Amount = 4,
                User = new User() {
                    Mobile_Number="9xxxxxxxxx",
                    Email = "rakesh@example.com",
                    First_Name = "Rakesh",
                    Last_Name = "Kumar"
                },
                Shipping_Address = new Address() {
                    Address_Type = "residential",
                    Area = "My area",
                    City = "My city",
                    Pincode = "400001",
                    State = "Maharashtra",
                    Street = "My Street"
                },
                Order_Line_Items = new OrderLineItem[]{
                    new OrderLineItem{
                        amount = 4,
                        description = "Product description",
                        image_url = "Image Url",
                        rate=4,
                        referrer_platform_sku_id="site",
                        title="Edu pack",
                        total_amount=4,
                        quantity= 1
                    }
                }
            });
        public async Task<Order> CreateOrder(Order order)
        {
            try
            {
                var createdOrder = await _restClient.PostAsync<Order>(_url_create_order, order);
                return createdOrder;
            }
            catch (System.Exception)
            {
                throw;
            }
        }
        public async Task<Order> FetchOrder(string id)
        {
            try
            {
                var fetchOrder = await _restClient.PostAsync<Order>(_url_fetch_order, id);
                return fetchOrder;
            }
            catch (System.Exception)
            {
                throw;
            }
        }
        public async Task<Order> All(string id)
        {
            List<Entity> entities = base.All(options);
            List<Order> orders = new List<Order>();
            foreach (Entity entity in entities)
            {
                orders.Add(entity as Order);
            }
            return orders;
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}