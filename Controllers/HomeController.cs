using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SampleAspNetCore.Models;
using nimbbl.checkout;

namespace SampleAspNetCore.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            //NimbblClient client = new NimbblClient("<base_url>", "<access_key>", "<access_secret>");
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

            var orderfetch = await client.FetchOrder("https://api.nimbbl.tech/api/v2/get-order/:order-id");
            //var request = new RestRequest(Method.POST);
            //request.AddHeader("Content-Type", "application/json");
            //request.AddHeader("Authorization", "Bearer <Token>");
            //request.AddParameter("application/json", "{\"amount_before_tax\": 4,\"currency\":\"INR\",\"invoice_id\":\"BrQv9nkxDEqWR3zg\",\"device_user_agent\": \"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.128 Safari/537.36\",\"order_from_ip\": \"x.x.x.x\",\"tax\": 0,\"user\": {\"mobile_number\":\"9999999999\",\"email\":\"rakesh.kumar@example.com\",\"first_name\":\"Rakesh\",\"last_name\":\"Kumar\"    },\"shipping_address\": {\"address_1\":\"Some address\",\"street\":\"Your street\",\"landmark\":\"My landmark\",\"area\":\"My area\",\"city\":\"Mumbai\",\"state\":\"Maharashtra\",\"pincode\":\"400018\",\"address_type\":\"residential\"},\"total_amount\": 4,\"order_line_items\": [{\"referrer_platform_sku_id\":\"sku1\",\"title\":\"Designer Triangles\",\"description\":\"Wallpaper by  chenspec from Pixabay\",\"quantity\": 1,\"rate\": 4,\"amount\": 4,\"total_amount\": 4,\"image_url\":\"https:\/\/cdn.pixabay.com\/photo\/2021\/02\/15\/15\/25\/rhomboid-6018215_960_720.jpg\"}]}",  ParameterType.RequestBody);
            //IRestResponse response = client.Execute(request);
            var transactionfetchbyID = await client.FetchTransactionbyID("https://api.nimbbl.tech/api/v2/fetch-transaction/:transaction-id");
            //client.Timeout = -1;
            //var request = new RestRequest(Method.GET);
            //request.AddHeader("Authorization", "Bearer <token>");
            //IRestResponse response = client.Execute(request);
            var transactionfetchbyOrderID = await client.FetchTransactionbyOrderID("https://api.nimbbl.tech/api/v2/order/fetch-transactions/:order-id");
            //client.Timeout = -1;
            //var request = new RestRequest(Method.GET);
            //request.AddHeader("Authorization", "Bearer <token>");
            //IRestResponse response = client.Execute(request);
            //return  View(new HomeModel(){ NimbblHostUrl = "<base_url>", AccessKey = token.AuthPrincipal.Access_Key, OrderId = order.Order_Id });
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
