﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;

namespace Volo.Abp.TenantManagement
{
    [Authorize(TenantManagementPermissions.Tenants.Default)]
    public class TenantAppService : TenantManagementAppServiceBase, ITenantAppService
    {
        protected IDataSeeder DataSeeder { get; }
        protected ITenantRepository TenantRepository { get; }
        protected ITenantManager TenantManager { get; }

        public TenantAppService(
            ITenantRepository tenantRepository, 
            ITenantManager tenantManager,
            IDataSeeder dataSeeder)
        {
            DataSeeder = dataSeeder;
            TenantRepository = tenantRepository;
            TenantManager = tenantManager;
        }

        public async Task<TenantDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<Tenant, TenantDto>(
                await TenantRepository.GetAsync(id)
            );
        }

        public async Task<PagedResultDto<TenantDto>> GetListAsync(GetTenantsInput input)
        {
            var count = await TenantRepository.GetCountAsync();
            var list = await TenantRepository.GetListAsync(input.Sorting, input.MaxResultCount, input.SkipCount, input.Filter);

            return new PagedResultDto<TenantDto>(
                count,
                ObjectMapper.Map<List<Tenant>, List<TenantDto>>(list)
            );
        }

        [Authorize(TenantManagementPermissions.Tenants.Create)]
        public async Task<TenantDto> CreateAsync(TenantCreateDto input)
        {
            var tenant = await TenantManager.CreateAsync(input.Name);
            await TenantRepository.InsertAsync(tenant);

            using (CurrentTenant.Change(tenant.Id, tenant.Name))
            {
                //TODO: Handle database creation?

                //TODO: Set admin email & password..?
                await DataSeeder.SeedAsync(tenant.Id);
            }
            
            return ObjectMapper.Map<Tenant, TenantDto>(tenant);
        }

        [Authorize(TenantManagementPermissions.Tenants.Update)]
        public async Task<TenantDto> UpdateAsync(Guid id, TenantUpdateDto input)
        {
            var tenant = await TenantRepository.GetAsync(id);
            await TenantManager.ChangeNameAsync(tenant, input.Name);
            await TenantRepository.UpdateAsync(tenant);
            return ObjectMapper.Map<Tenant, TenantDto>(tenant);
        }

        [Authorize(TenantManagementPermissions.Tenants.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            var tenant = await TenantRepository.FindAsync(id);
            if (tenant == null)
            {
                return;
            }

            await TenantRepository.DeleteAsync(tenant);
        }

        public async Task<string> GetDefaultConnectionStringAsync(Guid id)
        {
            var tenant = await TenantRepository.GetAsync(id);
            return tenant?.FindDefaultConnectionString();
        }

        public async Task SetDefaultConnectionStringAsync(Guid id, string defaultConnectionString)
        {
            var tenant = await TenantRepository.GetAsync(id);
            tenant.SetDefaultConnectionString(defaultConnectionString);
        }

        public async Task RemoveDefaultConnectionStringAsync(Guid id)
        {
            var tenant = await TenantRepository.GetAsync(id);
            tenant.RemoveDefaultConnectionString();
        }
    }
}
