﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Kcesar.TrainingSite.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kcesar.TrainingSite
{
  internal class TrainingSigninManager : SignInManager<ApplicationUser>
  {
    private readonly IConfiguration config;

    public TrainingSigninManager(IConfiguration config, UserManager<ApplicationUser> userManager, IHttpContextAccessor contextAccessor, IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor, ILogger<SignInManager<ApplicationUser>> logger, IAuthenticationSchemeProvider schemes, IUserConfirmation<ApplicationUser> confirmation) 
      : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
      this.config = config;
    }

    public override Task<SignInResult> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent)
    {
      return base.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent);
    }

    public override async Task<SignInResult> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent, bool bypassTwoFactor)
    {
      var result = await base.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent, bypassTwoFactor);
      var info = await GetExternalLoginInfoAsync();
      if (result.Succeeded)
      {
        var user = await UserManager.FindByLoginAsync(loginProvider, providerKey);
        var dirty = false;
        var value = info.Principal.FindFirst(ClaimTypes.GivenName).Value;
        if (!user.FirstName.Equals(value))
        {
          user.FirstName = value;
          dirty = true;
        }
        value = info.Principal.FindFirst(ClaimTypes.Surname).Value;
        if (!user.LastName.Equals(value))
        {
          user.LastName = value;
          dirty = true;
        }

        if (dirty) await UserManager.UpdateAsync(user);
      }
      else if (!(result.IsLockedOut || result.IsNotAllowed))
      {
        if (info.Principal.FindFirst("domain").Value == config["auth:google:domains"])
        {
          var user = new ApplicationUser
          {
            UserName = info.Principal.FindFirst(ClaimTypes.Email).Value,
            Email = info.Principal.FindFirst(ClaimTypes.Email).Value,
            FirstName = info.Principal.FindFirst(ClaimTypes.GivenName).Value,
            LastName = info.Principal.FindFirst(ClaimTypes.Surname).Value,
            EmailConfirmed = true
          };
          var identityResult = await UserManager.CreateAsync(user);
          await UserManager.AddLoginAsync(user, info);
          result = await base.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent, bypassTwoFactor);
        }
      }
      return result;
    }
  }
}