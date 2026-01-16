using SMHFR_BE.DTOs;
using SMHFR_BE.Models;

namespace SMHFR_BE.Services;

public static class HealthFacilityMapper
{
    public static HealthFacilityDTO ToDTO(this HealthFacility entity)
    {
        return new HealthFacilityDTO
        {
            HealthFacilityId = entity.HealthFacilityId,
            FacilityId = entity.FacilityId,
            HealthFacilityName = entity.HealthFacilityName,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            District = entity.District.ToDTO(),
            FacilityType = entity.FacilityType.ToDTO(),
            Ownership = entity.Ownership,
            OperationalStatus = entity.OperationalStatus,
            HCPartners = entity.HCPartners,
            HCProjectEndDate = entity.HCProjectEndDate,
            NutritionClusterPartners = entity.NutritionClusterPartners,
            DamalCaafimaadPartner = entity.DamalCaafimaadPartner,
            DamalCaafimaadProjectEndDate = entity.DamalCaafimaadProjectEndDate,
            BetterLifeProjectPartner = entity.BetterLifeProjectPartner,
            BetterLifeProjectEndDate = entity.BetterLifeProjectEndDate,
            CaafimaadPlusPartner = entity.CaafimaadPlusPartner,
            CaafimaadPlusProjectEndDate = entity.CaafimaadPlusProjectEndDate,
            FacilityInChargeName = entity.FacilityInChargeName,
            FacilityInChargeNumber = entity.FacilityInChargeNumber,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static StateDTO ToDTO(this State entity)
    {
        return new StateDTO
        {
            StateId = entity.StateId,
            StateCode = entity.StateCode,
            StateName = entity.StateName,
            CreatedAt = entity.CreatedAt
        };
    }

    public static RegionDTO ToDTO(this Region entity)
    {
        return new RegionDTO
        {
            RegionId = entity.RegionId,
            RegionName = entity.RegionName,
            State = entity.State.ToDTO(),
            CreatedAt = entity.CreatedAt
        };
    }

    public static DistrictDTO ToDTO(this District entity)
    {
        return new DistrictDTO
        {
            DistrictId = entity.DistrictId,
            DistrictName = entity.DistrictName,
            Region = entity.Region.ToDTO(),
            CreatedAt = entity.CreatedAt
        };
    }

    public static FacilityTypeDTO ToDTO(this FacilityType entity)
    {
        return new FacilityTypeDTO
        {
            FacilityTypeId = entity.FacilityTypeId,
            TypeName = entity.TypeName,
            CreatedAt = entity.CreatedAt
        };
    }

}
