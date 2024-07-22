﻿using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BHG.WebService
{
    public class BaseController : ControllerBase
    {
        protected ActionResult Error()
        {
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}
