using NocturneDetailedSkillInfo;

static void Equal(string name, string expected, string actual)
{
    if (!String.Equals(expected, actual, StringComparison.Ordinal))
        throw new InvalidOperationException(
            $"{name} failed. Expected [{expected}] but got [{actual}].");

    Console.WriteLine($"PASS {name}");
}

string untouched = "  Official<AF_DEL>\r\nHelp  ";
Equal(
    "no details preserves the incoming string byte-for-byte",
    untouched,
    GeneratedSuffixPolicy.Append(untouched, ""));

Equal(
    "AF_DEL is preserved when no suffix is added",
    "説明<AF_DEL>",
    GeneratedSuffixPolicy.Append("説明<AF_DEL>", ""));

Equal(
    "newlines are preserved when no suffix is added",
    "line 1\r\nline 2\n",
    GeneratedSuffixPolicy.Append("line 1\r\nline 2\n", ""));

string unrelatedEnglish = "Another mod says Power: grows with level.";
Equal(
    "unrelated Power text is not truncated",
    unrelatedEnglish,
    GeneratedSuffixPolicy.RemoveKnownSuffix(
        unrelatedEnglish,
        new[] { "  Power:41  Accuracy:76  Crit:24%" }));

string unrelatedJapanese = "別MODの説明では威力:状況により変化";
Equal(
    "unrelated Japanese power text is not truncated",
    unrelatedJapanese,
    GeneratedSuffixPolicy.RemoveKnownSuffix(
        unrelatedJapanese,
        new[] { "　威力:41　命中:76　CT:24%" }));

string baseHelp = "Deals physical damage.<AF_DEL>\r\n";
string suffix = "  Power:41  Accuracy:76  Crit:24%";
string first = GeneratedSuffixPolicy.Append(baseHelp, suffix);
string secondBase = GeneratedSuffixPolicy.RemoveKnownSuffix(first, new[] { suffix });
string second = GeneratedSuffixPolicy.Append(secondBase, suffix);
Equal("repeated calls do not duplicate the generated suffix", first, second);

string japaneseBase = "物理属性でダメージ<AF_DEL>\n";
string japaneseSuffix = "　威力:41　命中:76　CT:24%";
Equal(
    "only an exact known terminal suffix is removed",
    japaneseBase,
    GeneratedSuffixPolicy.RemoveKnownSuffix(
        japaneseBase + japaneseSuffix,
        new[] { japaneseSuffix }));

Console.WriteLine("All suffix-policy regression tests passed.");
