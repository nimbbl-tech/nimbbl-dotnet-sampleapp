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
    public class TransactionController : Controller
    {
        public async Task<Transaction> FetchTransactionbyID(string id)
        {
            try
            {
                var fetchTransaction = await _restClient.PostAsync<Transaction>(_url_fetchTransaction_byID, id);
                return fetchTransaction;
            }
            catch (System.Exception)
            {
                throw;
            }
        }
        public async Task<Transaction> FetchTransactionbyOrderID(string id)
        {
            try
            {
                var fetchTransaction = await _restClient.PostAsync<Transaction>(_url_fetchTransaction_byOrderID, id);
                return fetchTransaction;
            }
            catch (System.Exception)
            {
                throw;
            }
        }

    }
}