﻿using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class ShortDescription
    {
        [CascadingParameter]
        private IModalService Modal
        {
            get;
            set;
        }

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("ShortDescription.OnInitializedAsync");
            await UserManager.CheckLogin();

            if (!UserManager.IsLoggedIn)
            {
                Console.WriteLine("not logged in");
                Nav.NavigateTo("/");
                return;
            }

            var success = await Handler.InitializePage();

            if (success)
            {
                Handler.DefineModal(Modal);
            }
            else
            {
                Handler.DefineModal(null);
                Nav.NavigateTo("/");
            }
        }
    }
}