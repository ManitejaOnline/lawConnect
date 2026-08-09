using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds demo lawyers, addresses, availability, and sample notifications for local testing.
/// </summary>
public sealed class DemoDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DemoDataSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoDataSeeder"/> class.
    /// </summary>
    public DemoDataSeeder(ApplicationDbContext db, ILogger<DemoDataSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Inserts demo lawyers if none exist yet.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        GeographySeeder.Ensure(_db);

        var existingLawyerIds = await _db.Lawyers
            .Select(l => l.Id)
            .ToHashSetAsync(cancellationToken);
        var drafts = DemoLawyers().Where(d => !existingLawyerIds.Contains(d.LawyerId)).ToList();
        if (drafts.Count == 0)
        {
            _logger.LogInformation("Demo lawyers already present — skipping seed");
            return;
        }

        var specs = await _db.Specializations.ToDictionaryAsync(s => s.Id, cancellationToken);
        if (specs.Count == 0)
        {
            _logger.LogWarning("Specializations missing — run migrations before seeding demo data");
            return;
        }

        foreach (var draft in drafts)
        {
            var user = new User
            {
                Id = draft.UserId,
                PhoneNumber = draft.Phone,
                Email = draft.Email,
                FirstName = draft.FirstName,
                LastName = draft.LastName,
                ProfileImageUrl = draft.AvatarUrl,
                IsActive = true,
                IsPhoneVerified = true,
                IsEmailVerified = true
            };
            user.UserRoles.Add(new UserRole { UserId = draft.UserId, RoleId = SeedIds.Roles.Lawyer });

            var lawyer = new Lawyer
            {
                Id = draft.LawyerId,
                UserId = draft.UserId,
                BarCouncilNumber = draft.BarNumber,
                Bio = draft.Bio,
                YearsOfExperience = draft.Years,
                ConsultationFee = draft.Fee,
                EmergencyFee = draft.EmergencyFee,
                IsVerified = true,
                IsAvailableForEmergency = draft.Emergency,
                IsAcceptingAppointments = true,
                IsOnline = draft.Online,
                LastOnlineAtUtc = DateTime.UtcNow,
                RatingAverage = draft.Rating,
                RatingCount = draft.RatingCount,
                VerificationStatus = VerificationStatus.Approved,
                VerifiedAtUtc = DateTime.UtcNow.AddDays(-30)
            };

            lawyer.LawyerSpecializations.Add(new LawyerSpecialization
            {
                LawyerId = draft.LawyerId,
                SpecializationId = draft.PrimarySpec,
                YearsOfExperience = draft.Years,
                IsPrimary = true
            });
            if (draft.SecondarySpec is { } secondary)
            {
                lawyer.LawyerSpecializations.Add(new LawyerSpecialization
                {
                    LawyerId = draft.LawyerId,
                    SpecializationId = secondary,
                    YearsOfExperience = Math.Max(1, draft.Years / 2),
                    IsPrimary = false
                });
            }

            foreach (var lang in draft.Languages)
            {
                lawyer.Languages.Add(new LawyerLanguage
                {
                    LawyerId = draft.LawyerId,
                    LanguageCode = lang.Code,
                    LanguageName = lang.Name,
                    Proficiency = "Fluent"
                });
            }

            lawyer.Educations.Add(new LawyerEducation
            {
                LawyerId = draft.LawyerId,
                InstitutionName = draft.College,
                Degree = "LL.B.",
                FieldOfStudy = "Law",
                StartYear = 2005,
                EndYear = 2008
            });

            lawyer.Experiences.Add(new LawyerExperience
            {
                LawyerId = draft.LawyerId,
                OrganizationName = draft.Firm,
                Title = "Advocate",
                StartDate = new DateOnly(2014, 1, 1),
                IsCurrent = true,
                Description = draft.Bio
            });

            foreach (var day in new[]
                     {
                         DayOfWeekFlag.Monday,
                         DayOfWeekFlag.Tuesday,
                         DayOfWeekFlag.Wednesday,
                         DayOfWeekFlag.Thursday,
                         DayOfWeekFlag.Friday,
                         DayOfWeekFlag.Saturday
                     })
            {
                lawyer.Availabilities.Add(new LawyerAvailability
                {
                    LawyerId = draft.LawyerId,
                    DayOfWeek = day,
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(18, 0),
                    IsAvailable = true,
                    TimeZoneId = "Asia/Kolkata"
                });
            }

            user.Addresses.Add(new Address
            {
                UserId = draft.UserId,
                CityId = draft.CityId,
                Line1 = draft.AddressLine,
                PostalCode = draft.Postal,
                Latitude = draft.Lat,
                Longitude = draft.Lng,
                IsPrimary = true,
                Label = "Chambers"
            });

            _db.Users.Add(user);
            _db.Lawyers.Add(lawyer);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} demo lawyers for local testing", drafts.Count);
    }

    private static IReadOnlyList<DemoLawyerDraft> DemoLawyers() =>
    [
        new(
            Guid.Parse("c0de0001-0001-0001-0001-000000000001"),
            Guid.Parse("1a000001-0001-0001-0001-000000000001"),
            "+919876543201",
            "Priya",
            "Sharma",
            "priya.sharma@demo.lawconnect",
            "KAR/2012/10421",
            "Criminal defence advocate focused on bail, FIR quashing, and trial strategy.",
            14,
            2500,
            8000,
            4.9,
            186,
            true,
            true,
            SeedIds.Specializations.CriminalLaw,
            SeedIds.Specializations.ConstitutionalLaw,
            12.9716,
            77.5946,
            SeedIds.Geography.Bengaluru,
            "MG Road Chambers, Bengaluru",
            "560001",
            "National Law School of India University",
            "Sharma & Co.",
            "https://i.pravatar.cc/300?img=47",
            [("en", "English"), ("hi", "Hindi"), ("kn", "Kannada")]),
        new(
            Guid.Parse("c0de0001-0001-0001-0001-000000000002"),
            Guid.Parse("1a000001-0001-0001-0001-000000000002"),
            "+919876543202",
            "Arjun",
            "Mehta",
            "arjun.mehta@demo.lawconnect",
            "DEL/2010/8831",
            "Family law counsel for custody, maintenance, and protection orders.",
            16,
            2200,
            7500,
            4.8,
            142,
            true,
            true,
            SeedIds.Specializations.FamilyLaw,
            SeedIds.Specializations.CivilLaw,
            28.6139,
            77.2090,
            SeedIds.Geography.NewDelhi,
            "Connaught Place, New Delhi",
            "110001",
            "Campus Law Centre, DU",
            "Mehta Family Law",
            "https://i.pravatar.cc/300?img=12",
            [("en", "English"), ("hi", "Hindi")]),
        new(
            Guid.Parse("c0de0001-0001-0001-0001-000000000003"),
            Guid.Parse("1a000001-0001-0001-0001-000000000003"),
            "+919876543203",
            "Neha",
            "Kapoor",
            "neha.kapoor@demo.lawconnect",
            "MH/2015/22019",
            "Cyber crime and digital fraud specialist. Helps freeze accounts and file complaints fast.",
            11,
            3000,
            9000,
            4.7,
            98,
            true,
            true,
            SeedIds.Specializations.CyberLaw,
            SeedIds.Specializations.CriminalLaw,
            19.0760,
            72.8777,
            SeedIds.Geography.Mumbai,
            "Bandra West, Mumbai",
            "400050",
            "Government Law College, Mumbai",
            "Kapoor Cyber Counsel",
            "https://i.pravatar.cc/300?img=32",
            [("en", "English"), ("hi", "Hindi"), ("mr", "Marathi")]),
        new(
            Guid.Parse("c0de0001-0001-0001-0001-000000000004"),
            Guid.Parse("1a000001-0001-0001-0001-000000000004"),
            "+919876543204",
            "Rahul",
            "Iyer",
            "rahul.iyer@demo.lawconnect",
            "KAR/2016/55102",
            "Property and civil disputes — possession, title, and injunctions.",
            10,
            1800,
            5000,
            4.6,
            74,
            true,
            false,
            SeedIds.Specializations.PropertyLaw,
            SeedIds.Specializations.CivilLaw,
            12.9352,
            77.6245,
            SeedIds.Geography.Bengaluru,
            "Koramangala, Bengaluru",
            "560034",
            "Christ University School of Law",
            "Iyer Property Desk",
            "https://i.pravatar.cc/300?img=15",
            [("en", "English"), ("ta", "Tamil"), ("kn", "Kannada")]),
        new(
            Guid.Parse("c0de0001-0001-0001-0001-000000000005"),
            Guid.Parse("1a000001-0001-0001-0001-000000000005"),
            "+919876543205",
            "Ananya",
            "Reddy",
            "ananya.reddy@demo.lawconnect",
            "TG/2013/33118",
            "Consumer protection and product liability. Strong track record at district forums.",
            13,
            1500,
            4000,
            4.5,
            61,
            false,
            true,
            SeedIds.Specializations.ConsumerProtection,
            SeedIds.Specializations.CivilLaw,
            17.3850,
            78.4867,
            SeedIds.Geography.Hyderabad,
            "Banjara Hills, Hyderabad",
            "500034",
            "NALSAR University of Law",
            "Reddy Consumer Law",
            "https://i.pravatar.cc/300?img=45",
            [("en", "English"), ("te", "Telugu"), ("hi", "Hindi")]),
        new(
            Guid.Parse("c0de0001-0001-0001-0001-000000000006"),
            Guid.Parse("1a000001-0001-0001-0001-000000000006"),
            "+919876543206",
            "Vikram",
            "Singh",
            "vikram.singh@demo.lawconnect",
            "UP/2008/11990",
            "Corporate and employment counsel for startups and mid-size companies.",
            18,
            4500,
            null,
            4.8,
            210,
            false,
            true,
            SeedIds.Specializations.CorporateLaw,
            SeedIds.Specializations.LaborEmployment,
            28.4595,
            77.0266,
            SeedIds.Geography.Gurugram,
            "Cyber City, Gurugram",
            "122002",
            "Faculty of Law, BHU",
            "Singh Corporate Advisors",
            "https://i.pravatar.cc/300?img=13",
            [("en", "English"), ("hi", "Hindi")]),
        new(
            Guid.Parse("c0de0001-0001-0001-0001-000000000007"),
            Guid.Parse("1a000001-0001-0001-0001-000000000007"),
            "+919876543207",
            "Meera",
            "Nair",
            "meera.nair@demo.lawconnect",
            "KL/2011/7744",
            "Medical negligence and personal injury claims with hospital litigation experience.",
            15,
            2800,
            6500,
            4.7,
            88,
            true,
            false,
            SeedIds.Specializations.MedicalNegligence,
            SeedIds.Specializations.CivilLaw,
            12.9719,
            77.6412,
            SeedIds.Geography.Bengaluru,
            "Indiranagar, Bengaluru",
            "560038",
            "NLS Bangalore",
            "Nair Injury Law",
            "https://i.pravatar.cc/300?img=49",
            [("en", "English"), ("ml", "Malayalam"), ("hi", "Hindi")]),
        new(
            Guid.Parse("c0de0001-0001-0001-0001-000000000008"),
            Guid.Parse("1a000001-0001-0001-0001-000000000008"),
            "+919876543208",
            "Karan",
            "Malhotra",
            "karan.malhotra@demo.lawconnect",
            "PB/2017/90221",
            "Tax, GST notices, and banking recovery matters.",
            9,
            2000,
            null,
            4.4,
            53,
            false,
            true,
            SeedIds.Specializations.TaxLaw,
            SeedIds.Specializations.BankingFinance,
            30.7333,
            76.7794,
            SeedIds.Geography.ChandigarhCity,
            "Sector 17, Chandigarh",
            "160017",
            "Panjab University",
            "Malhotra Tax Chambers",
            "https://i.pravatar.cc/300?img=14",
            [("en", "English"), ("hi", "Hindi"), ("pa", "Punjabi")]),
        new(
            Guid.Parse("c0de0001-0001-0001-0001-000000000009"),
            Guid.Parse("1a000001-0001-0001-0001-000000000009"),
            "+919876543209",
            "Sameer",
            "Rao",
            "sameer.rao@demo.lawconnect",
            "TG/2014/44021",
            "Criminal and family counsel based in LB Nagar. Available for emergency SOS nearby.",
            12,
            2000,
            7000,
            4.8,
            91,
            true,
            true,
            SeedIds.Specializations.CriminalLaw,
            SeedIds.Specializations.FamilyLaw,
            17.3673,
            78.5763,
            SeedIds.Geography.Hyderabad,
            "LB Nagar Chambers, Hyderabad",
            "500074",
            "Osmania University College of Law",
            "Rao & Associates",
            "https://i.pravatar.cc/300?img=33",
            [("en", "English"), ("te", "Telugu"), ("hi", "Hindi")]),
    ];

    private sealed record DemoLawyerDraft(
        Guid UserId,
        Guid LawyerId,
        string Phone,
        string FirstName,
        string LastName,
        string Email,
        string BarNumber,
        string Bio,
        int Years,
        decimal Fee,
        decimal? EmergencyFee,
        double Rating,
        int RatingCount,
        bool Emergency,
        bool Online,
        Guid PrimarySpec,
        Guid? SecondarySpec,
        double Lat,
        double Lng,
        Guid CityId,
        string AddressLine,
        string Postal,
        string College,
        string Firm,
        string AvatarUrl,
        (string Code, string Name)[] Languages);
}
