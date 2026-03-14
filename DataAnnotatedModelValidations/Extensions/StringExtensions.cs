namespace DataAnnotatedModelValidations.Extensions;

internal static class StringExtensions
{
    // Adaption of HC FormatFieldName(string fieldName)
    // @see HotChocolate.Types.Descriptors.NameFormattingHelpers.FormatFieldName
    internal static string Camelize(this string memberName)
    {
        ArgumentException.ThrowIfNullOrEmpty(memberName);

        // quick exit
        if (char.IsLower(memberName[0]))
        {
            return memberName;
        }

        return string.Create(
            length: memberName.Length, 
            state: memberName, 
            (output, fieldName) =>
            {
                int p = 0;
                for (; p < fieldName.Length && char.IsLetter(fieldName[p]) && char.IsUpper(fieldName[p]); p++)
                {
                    output[p] = char.ToLowerInvariant(fieldName[p]);
                }

                // in case more than one character is upper case, we uppercase
                // the current character. We only uppercase the character
                // back if the last character is a letter
                //
                // before    after      result
                // FOOBar    FOOBar   = fooBar
                //    ^        ^
                // FOO1Ar    FOO1Ar   = foo1Ar
                //   ^         ^
                // FOO_Ar    FOO_Ar   = foo_Ar
                //   ^         ^
                if (p < fieldName.Length && p > 1 && char.IsLetter(fieldName[p]))
                {
                    output[p - 1] = char.ToUpperInvariant(output[p - 1]);
                }

                // Copy the rest unchanged
                for (; p < fieldName.Length; p++)
                {
                    output[p] = fieldName[p];
                }
            }
        );
    }
}
