using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SampleAspNetCore.Models;
using nimbbl.checkout;
using HomeController;
using NimbblClient;
using RestClient;

namespace SampleAspNetCore.Controllers
{
    public class SegmentController : Controller
    {
        
        public track(string req)
        {
            try
            {
                var isSegmentReq = true;
                var res = PostAsync(seg_track,req);
                isSegmentReq = false;
                return res
            }
            catch (System.Exception)
            {
                throw;
            }
        }
        public identify(string req)
        {
            try
            {
                var isSegmentReq = true;
                var res = PostAsync(seg_identify,req);
                isSegmentReq = false;
                return res
            }
            catch (System.Exception)
            {
                throw;
            }
        }
        public page(string req)
        {
            try
            {
                var isSegmentReq = true;
                var res = PostAsync(seg_page,req);
                isSegmentReq = false;
                return res
            }
            catch (System.Exception)
            {
                throw;
            }
        }
        public screen(string req)
        {
            try
            {
                var isSegmentReq = true;
                var res = PostAsync(seg_screen,req);
                isSegmentReq = false;
                return res
            }
            catch (System.Exception)
            {
                throw;
            }
        }
        public authReq(string req)
        {
            try
            {
                
            }
            catch (System.Exception)
            {
                throw;
            }
        }
        public authRes(string req)
        {
            try
            {
                
            }
            catch (System.Exception)
            {
                throw;
            }
        }
    var seg_identify = "https://api.segment.io/v1/identify";
    var seg_track = "https://api.segment.io/v1/track";
    var seg_page = "https://api.segment.io/v1/page";
    var seg_screen = "https://api.segment.io/v1/screen";
    var seg_group = "https://api.segment.io/v1/group";
    var seg_alias = "https://api.segment.io/v1/alias";
    var seg_batch = "https://api.segment.io/v1/batch";


    }
}