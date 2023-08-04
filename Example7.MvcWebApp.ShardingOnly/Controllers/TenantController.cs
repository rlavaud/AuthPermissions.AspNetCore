﻿// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.SupportCode.DownStatusCode;
using Example7.MvcWebApp.ShardingOnly.Models;
using Example7.MvcWebApp.ShardingOnly.PermissionsCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Example7.MvcWebApp.ShardingOnly.Controllers
{
    public class TenantController : Controller
    {
        private readonly IAuthTenantAdminService _authTenantAdmin;

        public TenantController(IAuthTenantAdminService authTenantAdmin)
        {
            _authTenantAdmin = authTenantAdmin;

        }

        [HasPermission(Example7Permissions.TenantList)]
        public async Task<IActionResult> Index(string message)
        {
            var tenantNames = await ShardingOnlyTenantDto.TurnIntoDisplayFormat( _authTenantAdmin.QueryTenants())
                .OrderBy(x => x.TenantName)
                .ToListAsync();

            ViewBag.Message = message;

            return View(tenantNames);
        }

        [HasPermission(Example7Permissions.ListDbsWithTenants)]
        public async Task<IActionResult> ListDatabases([FromServices] IGetSetShardingEntries shardingService)
        {
            var connections = await shardingService.GetDatabaseInfoNamesWithTenantNamesAsync();

            return View(connections);
        }

        [HasPermission(Example7Permissions.TenantCreate)]
        public IActionResult Create([FromServices]AuthPermissionsOptions authOptions, 
        [FromServices] IGetSetShardingEntries shardingService)
        {
            return View(ShardingOnlyTenantDto.SetupForCreate(authOptions,
                shardingService.GetAllShardingEntries().Select(x => x.Name).ToList()
                ));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example7Permissions.TenantCreate)]
        public async Task<IActionResult> Create(ShardingOnlyTenantDto input)
        {
            var status = await _authTenantAdmin.AddSingleTenantAsync(input.TenantName, null,
                true, input.ShardingName);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }

        [HasPermission(Example7Permissions.TenantUpdate)]
        public async Task<IActionResult> Edit(int id)
        {
            return View(await ShardingOnlyTenantDto.SetupForUpdateAsync(_authTenantAdmin, id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example7Permissions.TenantUpdate)]
        public async Task<IActionResult> Edit(ShardingOnlyTenantDto input)
        {
            var status = await _authTenantAdmin
                .UpdateTenantNameAsync(input.TenantId, input.TenantName);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }


        [HasPermission(Example7Permissions.TenantDelete)]
        public async Task<IActionResult> Delete(int id)
        {
            var status = await _authTenantAdmin.GetTenantViaIdAsync(id);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(new ShardingOnlyTenantDto
            {
                TenantId = id,
                TenantName = status.Result.TenantFullName,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example7Permissions.TenantDelete)]
        public async Task<IActionResult> Delete(ShardingOnlyTenantDto input)
        {
            var status = await _authTenantAdmin.DeleteTenantAsync(input.TenantId);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object)errorMessage);
        }
    }
}
