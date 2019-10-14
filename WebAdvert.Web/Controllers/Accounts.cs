using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAdvert.Web.Models.Accounts;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAdvert.Web.Controllers
{
	public class Accounts : Controller
	{
		private readonly SignInManager<CognitoUser> _signInManager;
		private readonly UserManager<CognitoUser> _userManager;
		private readonly CognitoUserPool _pool;

		public Accounts(SignInManager<CognitoUser> signInManager, UserManager<CognitoUser> userManager, CognitoUserPool pool)
		{
			_signInManager = signInManager;
			_userManager = userManager;
			_pool = pool;
		}

		public async Task<IActionResult> Signup()
		{
			var model = new SignupModel();
			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Signup(SignupModel model)
		{
			if (ModelState.IsValid)
			{
				var user = _pool.GetUser(model.Email);
				if (user.Status != null)
				{
					ModelState.AddModelError("UserExists", "User with email already exists");
					return View(model);
				}

				user.Attributes.Add(CognitoAttribute.Email.AttributeName, model.Email);
				user.Attributes.Add(CognitoAttribute.Name.AttributeName, "Alex");
				var createdUser = await _userManager.CreateAsync(user, model.Password);

				if (createdUser.Succeeded)
				{
					await _signInManager.SignInAsync(user, false);
					return RedirectToAction("Confirm");
				}
			}
			return View();
		}

		public async Task<IActionResult> Confirm()
		{
			var accessToken = await HttpContext.GetTokenAsync("access_token");
			var model = new ConfirmModel();
			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Confirm(ConfirmModel model)
		{
			if (ModelState.IsValid)
			{
				var user = await _userManager.FindByEmailAsync(model.Email);
				if (user == null)
				{
					ModelState.AddModelError("NotFound", "A user with the given email not found");
					return View(model);
				}

				var result = await _userManager.ConfirmEmailAsync(user, model.Code);
				if (result.Succeeded)
				{
					return RedirectToAction("Index", "Home");
				}
				else
				{
					foreach (var item in result.Errors)
					{
						ModelState.AddModelError(item.Code, item.Description);
					}
					return View(model);
				}
			}

			return View(model);

		}
	}
}
