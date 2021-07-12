﻿using System;
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
            var orderfetch = await client.Fetch("");
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
            return  View(new HomeModel(){ NimbblHostUrl = "<base_url>", AccessKey = token.AuthPrincipal.Access_Key, OrderId = order.Order_Id });
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
