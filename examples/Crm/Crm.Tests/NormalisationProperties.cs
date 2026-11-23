using GalaxyCheck;

namespace Crm.Tests;

[NUnit.Framework.TestFixture]
public class NormalisationProperties
{
    public static IGen<string> Emails => Gen.Email();

    [Property]
    public bool EveryGeneratedEmailIsValid([MemberGen(nameof(Emails))] string email) =>
        EmailAddress.Normalise(email) is not null;

    [Property]
    public bool EmailNormalisationIsIdempotent([MemberGen(nameof(Emails))] string email)
    {
        var once = EmailAddress.Normalise(email);
        return EmailAddress.Normalise(once) == once;
    }

    // Metamorphic: the case of the letters, white space at the ends, and a sub-address do not change the key.
    [Property]
    public bool CaseSpacesAndSubAddressDoNotMatter([MemberGen(nameof(Emails))] string email, bool upper, bool pad, bool subAddress)
    {
        var variant = upper ? email.ToUpperInvariant() : email;
        variant = pad ? "  " + variant + "\t" : variant;
        variant = subAddress ? variant.Replace("@", "+promo@") : variant;
        return EmailAddress.Normalise(variant) == EmailAddress.Normalise(email);
    }

    public static IGen<string> TextWithoutAt => Gen.String().WithLengthLessThanEqual(30).Where(s => !s.Contains('@'));

    // Known bad addresses run first. The generated cases add text with no "@" at all.
    [Property]
    [Example("@example.com")]
    [Example("ann@")]
    [Example("ann@@example.com")]
    [Example("ann@example")]
    [Example("ann@example..com")]
    [Example("+news@example.com")]
    [Example("ann smith@example.com")]
    public bool InvalidAddressesAreRejected([MemberGen(nameof(TextWithoutAt))] string text) =>
        EmailAddress.Normalise(text) is null;

    public static IGen<string> Phones => CrmGen.Phone;

    [Property]
    public bool PhoneNormalisationIsIdempotent([MemberGen(nameof(Phones))] string phone)
    {
        var once = PhoneNumber.Normalise(phone);
        return PhoneNumber.Normalise(once) == once;
    }

    public static IGen<string> Separators => Gen.String().FromCharacters(" -.()").WithLengthLessThanEqual(2);

    // Metamorphic: a separator between two digits does not change the number.
    [Property]
    public bool SeparatorsDoNotMatter(
        [MemberGen(nameof(Phones))] string phone,
        [MemberGen(nameof(Separators))] string separator,
        [GalaxyCheck.Gens.Injection.Int32.Between(0, 20)] int position)
    {
        var digitPositions = phone.Select((c, i) => (c, i)).Where(x => char.IsAsciiDigit(x.c)).Select(x => x.i).ToList();
        Property.Precondition(position < digitPositions.Count - 1);
        var at = digitPositions[position] + 1;
        return PhoneNumber.Normalise(phone.Insert(at, separator)) == PhoneNumber.Normalise(phone);
    }

    public static IGen<string> InternationalPhones => CrmGen.InternationalPhone;

    [Property]
    public bool PlusAndDoubleZeroAreTheSamePrefix([MemberGen(nameof(InternationalPhones))] string phone)
    {
        var digits = phone.TrimStart('+', '0');
        return PhoneNumber.Normalise("+" + digits) == PhoneNumber.Normalise("00" + digits);
    }

    // Pattern strings: a New Zealand mobile number in its national form, as people write it.
    public static IGen<string> NewZealandMobile => Gen.StringMatching("02[1-9] ?[0-9]{3} ?[0-9]{3,4}");

    [Property]
    public bool NationalNumbersGetTheCountryCodeAndLoseTheTrunkZero([MemberGen(nameof(NewZealandMobile))] string phone)
    {
        var digits = new string(phone.Where(char.IsAsciiDigit).ToArray());
        return PhoneNumber.Normalise(phone, countryCode: "64") == "+64" + digits[1..];
    }

    [Property]
    [Example("1-212-555-0175")]
    [Example("(212) 555-0175")]
    [Example("212.555.0175")]
    [Example("+1 212 555 0175")]
    [Example("001 212 555 0175")]
    public bool OneNorthAmericanNumberHasOneKey([MemberGen(nameof(SameNumber))] string spelling) =>
        PhoneNumber.Normalise(spelling) == "+12125550175";

    public static IGen<string> SameNumber => Gen.Constant("212-555-0175");
}
