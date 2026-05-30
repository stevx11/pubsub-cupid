using System.Text.RegularExpressions;
namespace HaoticniKupidon.Client;

// Klijentska validacija unosa. Iste provere kao na serveru
public static class Validators
{
    private static readonly Regex AlphanumericRegex =
        new(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex =
        new(@"^[0-9\+\-\s\(\)]{6,20}$", RegexOptions.Compiled);
    private static readonly Regex CityRegex =
        new(@"^[a-zA-ZÀ-žА-яа-я\s\-']{2,50}$", RegexOptions.Compiled);

    public static (bool isValid, string error) ValidateUsername(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (false, "Korisnicko ime ne sme biti prazno.");
        if (input.Length < 3 || input.Length > 30)
            return (false, "Korisnicko ime mora imati između 3 i 30 karaktera.");
        if (!AlphanumericRegex.IsMatch(input.Trim()))
            return (false, "Korisnicko ime sme da sadrzi samo slova, brojeve i '_'.");
        return (true, "");
    }

    public static (bool isValid, string error) ValidateCity(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (false, "Grad ne sme biti prazan.");
        if (!CityRegex.IsMatch(input.Trim()))
            return (false, "Naziv grada sadrzi nedozvoljene karaktere.");
        return (true, "");
    }

    public static (bool isValid, string error) ValidateAge(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (false, "Godine ne smeju biti prazne.");
        if (!int.TryParse(input.Trim(), out int age))
            return (false, "Godine moraju biti broj (npr. 25).");
        if (age <= 0)
            return (false, "Godine moraju biti pozitivan broj.");
        if (age < 18 || age > 99)
            return (false, "Godine moraju biti izmedju 18 i 99.");
        return (true, "");
    }

    public static (bool isValid, string error) ValidatePhone(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (false, "Broj telefona ne sme biti prazan.");
        if (!PhoneRegex.IsMatch(input.Trim()))
            return (false, "Format broja telefona nije validan. Primer: 0601234567");
        return (true, "");
    }
}