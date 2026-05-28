using System.Text.RegularExpressions;

namespace HaoticniKupidon.Server.Validation;

public static class InputValidator
{
    private static readonly Regex AlphanumericRegex =
        new(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex =
        new(@"^[0-9\+\-\s\(\)]{6,20}$", RegexOptions.Compiled);
    private static readonly Regex CityRegex =
        new(@"^[a-zA-ZÀ-žА-яа-я\s\-']{2,50}$", RegexOptions.Compiled);

    public static ValidationResult ValidateUsername(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ValidationResult.Fail("Korisničko ime ne sme biti prazno.");
        if (input.Length < 3 || input.Length > 30)
            return ValidationResult.Fail("Korisničko ime mora imati između 3 i 30 karaktera.");
        if (!AlphanumericRegex.IsMatch(input))
            return ValidationResult.Fail("Korisničko ime sme da sadrži samo slova, brojeve i '_'.");
        return ValidationResult.Ok();
    }

    public static ValidationResult ValidateCity(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ValidationResult.Fail("Grad ne sme biti prazan.");
        if (!CityRegex.IsMatch(input))
            return ValidationResult.Fail("Naziv grada sadrži nedozvoljene karaktere.");
        return ValidationResult.Ok();
    }

    public static ValidationResult ValidateAge(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ValidationResult.Fail("Godine ne smeju biti prazne.");
        if (!int.TryParse(input.Trim(), out int age))
            return ValidationResult.Fail("Godine moraju biti broj (npr. 25).");
        if (age <= 0)
            return ValidationResult.Fail("Godine moraju biti pozitivan broj.");
        if (age < 18 || age > 99)
            return ValidationResult.Fail("Godine moraju biti između 18 i 99.");
        return ValidationResult.Ok(age);
    }

    public static ValidationResult ValidatePhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ValidationResult.Fail("Broj telefona ne sme biti prazan.");
        if (!PhoneRegex.IsMatch(input.Trim()))
            return ValidationResult.Fail("Broj telefona nije validan. Primer: +381601234567");
        return ValidationResult.Ok();
    }
}

public class ValidationResult
{
    public bool IsValid { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? ParsedInt { get; private set; }

    private ValidationResult() { }

    public static ValidationResult Ok(int? parsed = null) =>
        new() { IsValid = true, ParsedInt = parsed };

    public static ValidationResult Fail(string message) =>
        new() { IsValid = false, ErrorMessage = message };
}