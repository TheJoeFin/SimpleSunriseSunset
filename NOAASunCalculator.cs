namespace SimpleSunriseSunset;
public static class NOAASunCalculator
{
    public static (DateTime sunrise, DateTime sunset) Calculate(
        double latitude, double longitude, DateTime date)
    {
        int dayOfYear = date.DayOfYear;

        // Solar declination angle
        double P = Math.Asin(0.39795 * Math.Cos(0.98563 * (dayOfYear - 173) * Math.PI / 180));

        // Hour angle
        double hourAngle = Math.Acos(-Math.Tan(latitude * Math.PI / 180) * Math.Tan(P));
        int hoursUtcOffset = (int)(longitude / 15);

        // Calculate times (in decimal hours)
        double sunriseHour = 13 - (hourAngle * 180 / Math.PI / 15);
        double sunsetHour = 13 + (hourAngle * 180 / Math.PI / 15);

        return (
            date.Date.AddHours(sunriseHour),
            date.Date.AddHours(sunsetHour)
        );
    }
}
