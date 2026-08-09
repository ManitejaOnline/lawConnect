namespace LawConnect.Shared.Constants;

/// <summary>
/// Deterministic identifiers for seed data (roles and specializations).
/// </summary>
public static class SeedIds
{
    public static class Roles
    {
        public static readonly Guid Customer = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid Lawyer = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid Admin = Guid.Parse("33333333-3333-3333-3333-333333333333");
    }

    public static class Specializations
    {
        public static readonly Guid CriminalLaw = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000001");
        public static readonly Guid CivilLaw = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000002");
        public static readonly Guid FamilyLaw = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000003");
        public static readonly Guid CorporateLaw = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000004");
        public static readonly Guid PropertyLaw = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000005");
        public static readonly Guid IntellectualProperty = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000006");
        public static readonly Guid TaxLaw = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000007");
        public static readonly Guid LaborEmployment = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000008");
        public static readonly Guid ConstitutionalLaw = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000009");
        public static readonly Guid ConsumerProtection = Guid.Parse("aaaaaaaa-0001-0001-0001-00000000000a");
        public static readonly Guid CyberLaw = Guid.Parse("aaaaaaaa-0001-0001-0001-00000000000b");
        public static readonly Guid ImmigrationLaw = Guid.Parse("aaaaaaaa-0001-0001-0001-00000000000c");
        public static readonly Guid EnvironmentalLaw = Guid.Parse("aaaaaaaa-0001-0001-0001-00000000000d");
        public static readonly Guid BankingFinance = Guid.Parse("aaaaaaaa-0001-0001-0001-00000000000e");
        public static readonly Guid MedicalNegligence = Guid.Parse("aaaaaaaa-0001-0001-0001-00000000000f");
    }

    public static class EmergencyCategories
    {
        public static readonly Guid CriminalEmergency = Guid.Parse("bbbbbbbb-0001-0001-0001-000000000001");
        public static readonly Guid CivilEmergency = Guid.Parse("bbbbbbbb-0001-0001-0001-000000000002");
        public static readonly Guid FamilyCrisis = Guid.Parse("bbbbbbbb-0001-0001-0001-000000000003");
        public static readonly Guid CyberCrime = Guid.Parse("bbbbbbbb-0001-0001-0001-000000000004");
        public static readonly Guid ConsumerEmergency = Guid.Parse("bbbbbbbb-0001-0001-0001-000000000005");
    }

    public static class Geography
    {
        public static readonly Guid India = Guid.Parse("dddddddd-0001-0001-0001-000000000001");
        public static readonly Guid UnitedKingdom = Guid.Parse("dddddddd-0001-0001-0001-000000000002");

        public static readonly Guid Karnataka = Guid.Parse("dddddddd-0002-0001-0001-000000000001");
        public static readonly Guid Maharashtra = Guid.Parse("dddddddd-0002-0001-0001-000000000002");
        public static readonly Guid Delhi = Guid.Parse("dddddddd-0002-0001-0001-000000000003");
        public static readonly Guid Telangana = Guid.Parse("dddddddd-0002-0001-0001-000000000004");
        public static readonly Guid Haryana = Guid.Parse("dddddddd-0002-0001-0001-000000000005");
        public static readonly Guid Chandigarh = Guid.Parse("dddddddd-0002-0001-0001-000000000006");
        public static readonly Guid Gujarat = Guid.Parse("dddddddd-0002-0001-0001-000000000007");
        public static readonly Guid Rajasthan = Guid.Parse("dddddddd-0002-0001-0001-000000000008");
        public static readonly Guid England = Guid.Parse("dddddddd-0002-0001-0001-000000000009");

        public static readonly Guid Bengaluru = Guid.Parse("dddddddd-0003-0001-0001-000000000001");
        public static readonly Guid Mumbai = Guid.Parse("dddddddd-0003-0001-0001-000000000002");
        public static readonly Guid NewDelhi = Guid.Parse("dddddddd-0003-0001-0001-000000000003");
        public static readonly Guid Hyderabad = Guid.Parse("dddddddd-0003-0001-0001-000000000004");
        public static readonly Guid Gurugram = Guid.Parse("dddddddd-0003-0001-0001-000000000005");
        public static readonly Guid ChandigarhCity = Guid.Parse("dddddddd-0003-0001-0001-000000000006");
        public static readonly Guid Mysuru = Guid.Parse("dddddddd-0003-0001-0001-000000000007");
        public static readonly Guid Surat = Guid.Parse("dddddddd-0003-0001-0001-000000000008");
        public static readonly Guid Jaipur = Guid.Parse("dddddddd-0003-0001-0001-000000000009");
        public static readonly Guid Ahmedabad = Guid.Parse("dddddddd-0003-0001-0001-00000000000a");
        public static readonly Guid London = Guid.Parse("dddddddd-0003-0001-0001-00000000000b");
    }
}
