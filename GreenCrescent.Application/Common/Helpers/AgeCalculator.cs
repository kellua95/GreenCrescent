namespace GreenCrescent.Application.Common.Helpers;

public static class AgeCalculator
{
    public static int? Calculate(
        DateOnly? dateOfBirth,
        DateOnly today)
    {
        if (!dateOfBirth.HasValue ||
            dateOfBirth.Value > today)
        {
            return null;
        }

        var birthDate = dateOfBirth.Value;
        var age = today.Year - birthDate.Year;

        if (today < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }

    public static DateOnly TodayInJordan()
    {
        var timeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman");

        var localNow = TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            timeZone);

        return DateOnly.FromDateTime(localNow.DateTime);
    }
}