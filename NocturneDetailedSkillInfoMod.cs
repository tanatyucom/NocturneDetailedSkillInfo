using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

[assembly: MelonInfo(typeof(NocturneDetailedSkillInfo.NocturneDetailedSkillInfoMod), "Nocturne Detailed Skill Info", "1.0.0", "Gray Ghost")]
[assembly: MelonGame(null, "smt3hd")]

namespace NocturneDetailedSkillInfo
{
    public sealed class NocturneDetailedSkillInfoMod : MelonMod
    {
        internal static MelonLogger.Instance? Log;
        internal static readonly HashSet<int> LoggedHelpIds = new HashSet<int>();

        // RC3 localized-audit state.
        // The canonical txt[] arrays are Japanese-fixed in this game build,
        // while datSkillHelp_msg.Get(id) follows the active game language.
        internal static bool AuditBypass;
        internal static bool AuditExporting;
        internal static bool AuditExported;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            LoggerInstance.Msg("Nocturne Detailed Skill Info 1.0.0 initialized.");
            LoggerInstance.Msg("Verified Detailed Skill Info enabled (Japanese / English).");

            try
            {
                HarmonyInstance.PatchAll(typeof(NocturneDetailedSkillInfoMod).Assembly);
                LoggerInstance.Msg("Harmony patch registration complete.");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Harmony patch registration failed: {ex.GetType().Name}: {ex.Message}");
            }

            // RC3 audit is intentionally delayed until the first real
            // datSkillHelp_msg.Get(id) UI request. At that point the game's
            // active language table is known to be initialized.
        }

        internal static void TryExportLocalizedAudit()
        {
            if (AuditExported || AuditExporting)
                return;

            string outPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "NOCTURNE_DETAILED_SKILL_INFO_AUDIT.csv");

            AuditExporting = true;

            try
            {
                var skills = datSkill.tbl;
                var normal = datNormalSkill.tbl;
                var names = datSkillName.txt;

                if (skills == null || normal == null || names == null)
                {
                    Log?.Warning("Localized audit skipped because one or more canonical tables are null.");
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine(
                    "SkillID,RawName,ObservedLanguage,OfficialHelp,GeneratedHelp,Changed,Category," +
                    "SkillAttr,Type,Use,KoukaType,CostType,Cost,TargetType,TargetArea," +
                    "HitType,HitLevel,HitMin,HitMax,HpType,HpN,BadType,BadLevel,BasStatus," +
                    "HojoType,HojoPoint,DeadType,Program,CriticalPoint,FailPoint,MagicBase,MagicLimit,ReviewFlags"
                );

                int rows = 0;
                int changed = 0;

                // Prevent our own Harmony postfix from modifying the nested
                // Get(id) calls used by the audit sweep.
                AuditBypass = true;

                for (int id = 0; id < skills.Length; id++)
                {
                    try
                    {
                        var map = skills[id];
                        if (map == null) continue;

                        int idx = map.index;
                        if (idx < 0 || idx >= normal.Length) continue;

                        var n = normal[idx];
                        if (n == null) continue;

                        string rawName = SafeArray(names, id);

                        // Critical RC3 change:
                        // Use the same getter path the actual game UI uses.
                        // In English mode this returns English official Help;
                        // in Japanese mode it returns Japanese official Help.
                        string official = Normalize(datSkillHelp_msg.Get(id) ?? "");

                        string observedLanguage =
                            DetailedHelpBuilder.DetectLanguage(official).ToString();

                        bool eligible =
                            DetailedHelpBuilder.IsEligibleForDetailedHelp(id, official);

                        string generated =
                            DetailedHelpBuilder.Build(id, official);

                        bool isChanged =
                            !String.Equals(official, generated, StringComparison.Ordinal);

                        if (isChanged) changed++;

                        string category = Classify(map.skillattr, n);
                        string reviewFlags =
                            BuildReviewFlags(id, n, generated, official);

                        if (!eligible)
                            reviewFlags = String.IsNullOrEmpty(reviewFlags)
                                ? "RC_FILTER_EXCLUDED"
                                : reviewFlags + "|RC_FILTER_EXCLUDED";

                        string[] values =
                        {
                            id.ToString(CultureInfo.InvariantCulture),
                            rawName,
                            observedLanguage,
                            official,
                            generated,
                            isChanged ? "YES" : "NO",
                            category,

                            map.skillattr.ToString(CultureInfo.InvariantCulture),
                            map.type.ToString(CultureInfo.InvariantCulture),
                            n.use.ToString(CultureInfo.InvariantCulture),
                            n.koukatype.ToString(CultureInfo.InvariantCulture),
                            n.costtype.ToString(CultureInfo.InvariantCulture),
                            n.cost.ToString(CultureInfo.InvariantCulture),
                            n.targettype.ToString(CultureInfo.InvariantCulture),
                            n.targetarea.ToString(CultureInfo.InvariantCulture),

                            n.hittype.ToString(CultureInfo.InvariantCulture),
                            n.hitlevel.ToString(CultureInfo.InvariantCulture),
                            n.targetcntmin.ToString(CultureInfo.InvariantCulture),
                            n.targetcntmax.ToString(CultureInfo.InvariantCulture),

                            n.hptype.ToString(CultureInfo.InvariantCulture),
                            n.hpn.ToString(CultureInfo.InvariantCulture),
                            n.badtype.ToString(CultureInfo.InvariantCulture),
                            n.badlevel.ToString(CultureInfo.InvariantCulture),
                            n.basstatus.ToString(CultureInfo.InvariantCulture),

                            n.hojotype.ToString(CultureInfo.InvariantCulture),
                            n.hojopoint.ToString(CultureInfo.InvariantCulture),
                            n.deadtype.ToString(CultureInfo.InvariantCulture),
                            n.program.ToString(CultureInfo.InvariantCulture),
                            n.criticalpoint.ToString(CultureInfo.InvariantCulture),
                            n.failpoint.ToString(CultureInfo.InvariantCulture),
                            n.magicbase.ToString(CultureInfo.InvariantCulture),
                            n.magiclimit.ToString(CultureInfo.InvariantCulture),

                            reviewFlags
                        };

                        sb.AppendLine(
                            string.Join(",", Array.ConvertAll(values, EscapeCsv)));

                        rows++;
                    }
                    catch (Exception rowEx)
                    {
                        Log?.Warning(
                            $"Localized audit row {id} failed: " +
                            $"{rowEx.GetType().Name}: {rowEx.Message}");
                    }
                }

                File.WriteAllText(
                    outPath,
                    sb.ToString(),
                    new UTF8Encoding(true));

                AuditExported = true;

                Log?.Msg(
                    $"LOCALIZED_AUDIT_DONE rows={rows} changed={changed} path={outPath}");
            }
            catch (Exception ex)
            {
                Log?.Error(
                    $"Localized audit failed: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                AuditBypass = false;
                AuditExporting = false;
            }
        }

        internal static string Classify(int skillAttr, Il2Cppnewdata_H.datNormalSkill_t n)
        {
            if (n.hojotype != 0 && n.hojopoint > 0 && n.hojopoint < 99)
                return "BUFF_DEBUFF";

            if (n.hptype == 2)
                return "RECOVERY";

            if (n.hptype == 1 && n.basstatus != 0)
                return "DAMAGE_AILMENT";

            if (n.hptype == 1)
                return skillAttr == 0 ? "PHYSICAL_DAMAGE" : "MAGIC_DAMAGE";

            if (n.basstatus != 0 && n.badlevel > 0 && n.badlevel < 255)
                return "AILMENT";

            if (n.targetcntmin > 1 || n.targetcntmax > 1)
                return "MULTI_HIT_OR_MULTI_EFFECT";

            return "OTHER";
        }

        internal static string BuildReviewFlags(
            int id,
            Il2Cppnewdata_H.datNormalSkill_t n,
            string generated,
            string official)
        {
            var flags = new List<string>();

            if (n.criticalpoint != 0 || n.failpoint != 0)
            {
                string normalizedForCrit = NocturneDetailedSkillInfoMod.Normalize(official);
                bool verifiedDamage =
                    (normalizedForCrit.IndexOf("ダメージ", StringComparison.Ordinal) >= 0 ||
                     normalizedForCrit.IndexOf("damage", StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (n.hptype == 1 || n.hptype == 6 || id == 262 || id == 275);

                if (verifiedDamage)
                    flags.Add("CRIT_ACCURACY_RESOLVED");
                else
                    flags.Add("CRIT_FAIL_UNRESOLVED");
            }

            if (n.magicbase != 0 || n.magiclimit != 0)
                flags.Add("MAGIC_SCALING_UNRESOLVED");

            string normalizedOfficial = NocturneDetailedSkillInfoMod.Normalize(official);
            if (n.hptype == 1 &&
                normalizedOfficial.IndexOf("ダメージ", StringComparison.Ordinal) < 0 &&
                normalizedOfficial.IndexOf("damage", StringComparison.OrdinalIgnoreCase) < 0)
                flags.Add("HPN_DAMAGE_GUARD");

            if (n.targetcntmax > 1 &&
                normalizedOfficial.IndexOf("ランダム", StringComparison.Ordinal) < 0 &&
                normalizedOfficial.IndexOf("複数回", StringComparison.Ordinal) < 0 &&
                normalizedOfficial.IndexOf("連続", StringComparison.Ordinal) < 0 &&
                normalizedOfficial.IndexOf("random", StringComparison.OrdinalIgnoreCase) < 0 &&
                normalizedOfficial.IndexOf("multiple", StringComparison.OrdinalIgnoreCase) < 0 &&
                normalizedOfficial.IndexOf("times", StringComparison.OrdinalIgnoreCase) < 0 &&
                normalizedOfficial.IndexOf("hits", StringComparison.OrdinalIgnoreCase) < 0)
                flags.Add("HITCOUNT_GUARD");

            if (n.deadtype != 0)
                flags.Add("DEADTYPE_UNRESOLVED");

            if (n.program != 0)
                flags.Add("PROGRAM_SPECIAL");

            if (n.hojotype != 0 && !DetailedHelpBuilder.IsBasicHojoMask(n.hojotype))
                flags.Add("HOJO_UNKNOWN_BITS");

            if (n.basstatus != 0 && String.IsNullOrEmpty(DetailedHelpBuilder.DecodeAilment(n.basstatus)))
                flags.Add("AILMENT_MULTI_OR_UNKNOWN");

            if (!String.Equals(generated, official, StringComparison.Ordinal))
                flags.Add("AUTO_DETAIL_ACTIVE");

            return string.Join("|", flags);
        }

        internal static string SafeArray(
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray arr,
            int index)
        {
            try
            {
                if (index < 0 || index >= arr.Length) return "";
                return arr[index] ?? "";
            }
            catch { return ""; }
        }

        internal static string Normalize(string s)
        {
            return (s ?? "")
                .Replace("<AF_DEL>", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();
        }

        internal static string EscapeCsv(string? value)
        {
            string s = value ?? "";
            if (s.Contains("\"")) s = s.Replace("\"", "\"\"");
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\r") || s.Contains("\n"))
                s = "\"" + s + "\"";
            return s;
        }
    }

    [HarmonyPatch(typeof(datSkillHelp_msg), nameof(datSkillHelp_msg.Get), new Type[] { typeof(int) })]
    internal static class SkillHelpGetterPatch
    {
        [HarmonyPostfix]
        private static void Postfix(int id, ref string __result)
        {
            // Nested getter calls from RC3's audit sweep must return the raw
            // localized official text without being detailed again.
            if (NocturneDetailedSkillInfoMod.AuditBypass)
                return;

            try
            {
                string original = NocturneDetailedSkillInfoMod.Normalize(__result ?? "");
                string detailed = DetailedHelpBuilder.Build(id, original);
                __result = detailed;

                // Public 1.0: localized audit export is disabled by default.
                // Developers can re-enable TryExportLocalizedAudit() here when
                // validating future language or skill-data changes.

                if (!String.Equals(original, detailed, StringComparison.Ordinal) &&
                    NocturneDetailedSkillInfoMod.LoggedHelpIds.Add(id))
                {
                    string name = "";
                    try
                    {
                        if (datSkillName.txt != null && id >= 0 && id < datSkillName.txt.Length)
                            name = datSkillName.txt[id] ?? "";
                    }
                    catch { }

                    NocturneDetailedSkillInfoMod.Log?.Msg(
                        $"AUTO_HELP id={id} name=\"{OneLine(name)}\" before=\"{OneLine(original)}\" after=\"{OneLine(detailed)}\""
                    );
                }
            }
            catch (Exception ex)
            {
                NocturneDetailedSkillInfoMod.Log?.Warning(
                    $"AUTO_HELP_FAIL id={id}: {ex.GetType().Name}: {ex.Message}"
                );
            }
        }

        private static string OneLine(string? value)
        {
            return (value ?? "")
                .Replace("\r", "")
                .Replace("\n", " / ")
                .Replace("\"", "'");
        }
    }

    internal enum DetailLanguage
    {
        Japanese,
        English
    }

    internal static class DetailedHelpBuilder
    {
        internal static DetailLanguage DetectLanguage(string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                foreach (char c in text)
                {
                    // Hiragana, Katakana, CJK Unified Ideographs, half/full-width kana.
                    if ((c >= '\u3040' && c <= '\u30ff') ||
                        (c >= '\u3400' && c <= '\u9fff') ||
                        (c >= '\uff66' && c <= '\uff9f'))
                        return DetailLanguage.Japanese;
                }
            }

            return DetailLanguage.English;
        }

        private static string JoinDetails(string baseText, List<string> details, DetailLanguage lang)
        {
            if (details.Count == 0)
                return baseText;

            string sep = lang == DetailLanguage.Japanese ? "　" : "  ";
            return baseText + sep + string.Join(sep, details);
        }

        private static string Label(DetailLanguage lang, string ja, string en)
            => lang == DetailLanguage.Japanese ? ja : en;

        public static string Build(int id, string original)
        {
            var skills = datSkill.tbl;
            var normal = datNormalSkill.tbl;

            if (skills == null || normal == null)
                return original;

            if (id < 0 || id >= skills.Length)
                return original;

            var map = skills[id];
            if (map == null || map.index < 0 || map.index >= normal.Length)
                return original;

            var n = normal[map.index];
            if (n == null)
                return original;

            string baseText = StripGeneratedSuffix(original);
            DetailLanguage lang = DetectLanguage(baseText);

            if (!IsEligibleForDetailedHelp(id, baseText))
                return baseText;
            var details = new List<string>();

            if (IsVerifiedDamageDetail(id, baseText, n))
            {
                // Two verified fixed-multi-hit skills store total power 32,
                // while external SMT3 tables document 8 power per hit.
                if (id == 262 || id == 275) // Boogie Woogie / Andalusia
                    details.Add(lang == DetailLanguage.Japanese ? "威力:8/回" : "Power:8/hit");
                else if (n.hpn > 0)
                    details.Add($"{Label(lang, "威力", "Power")}:{n.hpn}");

                AddAccuracyAndCritical(details, n, lang);
            }

            if (n.hptype == 2 && n.hpn > 0)
                details.Add($"{Label(lang, "回復基礎値", "Heal Base")}:{n.hpn}");

            string ailment = DecodeAilment(n.basstatus, lang);
            if (!String.IsNullOrEmpty(ailment) &&
                n.badlevel > 0 &&
                n.badlevel < 255)
            {
                details.Add(lang == DetailLanguage.Japanese
                    ? $"{ailment}付与値:{n.badlevel}"
                    : $"{ailment} Rate:{n.badlevel}");
            }

            string hitCount = "";
            if (TryGetHitCountDisplay(id, baseText, n, lang, out hitCount))
                details.Add($"{Label(lang, "回数", "Hits")}:{hitCount}");

            AddBasicHojoDetails(details, n.hojotype, n.hojopoint, lang);

            return JoinDetails(baseText, details, lang);
        }

        private static bool IsVerifiedDamageDetail(
            int id,
            string baseText,
            Il2Cppnewdata_H.datNormalSkill_t n)
        {
            if (n == null || !IsDamageHelp(baseText))
                return false;

            // Standard damage rows.
            if (n.hptype == 1)
                return true;

            // HP-dependent physical rows validated against published SMT3 data.
            if (n.hptype == 6)
                return true;

            // Fixed/special multi-hit rows validated individually:
            // 262 ブギウギ, 275 血のアンダルシア.
            return id == 262 || id == 275;
        }

        private static void AddAccuracyAndCritical(
            List<string> details,
            Il2Cppnewdata_H.datNormalSkill_t n,
            DetailLanguage lang)
        {
            if (n.hitlevel > 0 && n.hitlevel < 255)
            {
                int displayedAccuracy = n.hitlevel - n.failpoint;
                if (displayedAccuracy < 0)
                    displayedAccuracy = 0;

                details.Add($"{Label(lang, "命中", "Accuracy")}:{displayedAccuracy}");
            }

            if (n.criticalpoint > 0 && n.criticalpoint < 255)
                details.Add($"{Label(lang, "CT", "Crit")}:{n.criticalpoint}%");
        }

        private static void AddBasicHojoDetails(
            List<string> details,
            uint hojoType,
            int hojoPoint,
            DetailLanguage lang)
        {
            if (hojoType == 0 || hojoPoint <= 0 || hojoPoint >= 99)
                return;

            if (!IsBasicHojoMask(hojoType))
                return;

            string stage = lang == DetailLanguage.Japanese
                ? (hojoPoint == 1 ? "1段階" : $"{hojoPoint}段階")
                : (hojoPoint == 1 ? "1 stage" : $"{hojoPoint} stages");

            if ((hojoType & 1u)   != 0) details.Add($"{Label(lang, "物理攻撃", "Phys Atk")}:+{stage}");
            if ((hojoType & 2u)   != 0) details.Add($"{Label(lang, "物理攻撃", "Phys Atk")}:-{stage}");
            if ((hojoType & 4u)   != 0) details.Add($"{Label(lang, "魔法威力", "Mag Power")}:+{stage}");
            if ((hojoType & 8u)   != 0) details.Add($"{Label(lang, "魔法威力", "Mag Power")}:-{stage}");
            if ((hojoType & 16u)  != 0) details.Add($"{Label(lang, "命中", "Accuracy")}:+{stage}");
            if ((hojoType & 32u)  != 0) details.Add($"{Label(lang, "命中", "Accuracy")}:-{stage}");
            if ((hojoType & 64u)  != 0) details.Add($"{Label(lang, "防御", "Defense")}:+{stage}");
            if ((hojoType & 128u) != 0) details.Add($"{Label(lang, "防御", "Defense")}:-{stage}");
            if ((hojoType & 256u) != 0) details.Add($"{Label(lang, "回避", "Evasion")}:+{stage}");
            if ((hojoType & 512u) != 0) details.Add($"{Label(lang, "回避", "Evasion")}:-{stage}");
        }

        public static bool IsBasicHojoMask(uint hojoType)
        {
            const uint KnownMask =
                1u | 2u | 4u | 8u |
                16u | 32u | 64u | 128u |
                256u | 512u;

            return hojoType != 0 && (hojoType & ~KnownMask) == 0;
        }

        private static bool IsDamageHelp(string baseText)
        {
            string h = NocturneDetailedSkillInfoMod.Normalize(baseText);
            if (String.IsNullOrWhiteSpace(h))
                return false;

            // HpType=1 is reused by at least one non-damage effect.
            // Require the localized official help to explicitly identify damage.
            return h.IndexOf("ダメージ", StringComparison.Ordinal) >= 0 ||
                   h.IndexOf("damage", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryGetHitCountDisplay(
            int id,
            string baseText,
            Il2Cppnewdata_H.datNormalSkill_t n,
            DetailLanguage lang,
            out string display)
        {
            display = "";

            if (n == null || n.targetcntmax <= 1)
                return false;

            string h = NocturneDetailedSkillInfoMod.Normalize(baseText);
            if (String.IsNullOrWhiteSpace(h))
                return false;

            // Verified player-facing physical exceptions must run BEFORE
            // the generic "ランダム" rule, otherwise their more precise
            // behavior is masked.
            switch (id)
            {
                case 98:  // 暴れまくり
                case 108: // デスバウンド
                case 110: // 乱入剣
                    display = lang == DetailLanguage.Japanese
                        ? "最大5（敵1体時最大2）"
                        : "Max 5 (Max 2 vs 1 enemy)";
                    return true;

                case 115: // 玉砕破
                    display = lang == DetailLanguage.Japanese
                        ? "最大4（敵1体時最大2）"
                        : "Max 4 (Max 2 vs 1 enemy)";
                    return true;

                case 275: // 血のアンダルシア
                    display = lang == DetailLanguage.Japanese ? "最大4" : "Max 4";
                    return true;
            }

            // For the remaining externally validated random multi-hit attacks,
            // the raw minimum is not a literal minimum hit count.
            if (h.IndexOf("ランダム", StringComparison.Ordinal) >= 0 ||
                h.IndexOf("random", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                display = lang == DetailLanguage.Japanese
                    ? $"1～{n.targetcntmax}"
                    : $"1-{n.targetcntmax}";
                return true;
            }

            // Explicit multiple-hit wording with a fixed raw count is safe.
            if ((h.IndexOf("複数回", StringComparison.Ordinal) >= 0 ||
                 h.IndexOf("連続", StringComparison.Ordinal) >= 0 ||
                 h.IndexOf("multiple", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 h.IndexOf("times", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 h.IndexOf("hits", StringComparison.OrdinalIgnoreCase) >= 0) &&
                n.targetcntmin == n.targetcntmax)
            {
                display = n.targetcntmax.ToString();
                return true;
            }

            return false;
        }

        public static bool IsEligibleForDetailedHelp(int id, string baseText)
        {
            string name = "";
            try
            {
                if (datSkillName.txt != null && id >= 0 && id < datSkillName.txt.Length)
                    name = datSkillName.txt[id] ?? "";
            }
            catch { }

            string n = (name ?? "").Trim();
            string h = NocturneDetailedSkillInfoMod.Normalize(baseText);

            // Release-candidate exclusions found by the 0.9.7 full audit.
            if (String.IsNullOrWhiteSpace(n))
                return false;

            if (n.IndexOf("リザーブ", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            if (n.IndexOf("RESERVE", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            if (h.IndexOf("ＲＥＳＥＲＶＥＳＫＩＬＬ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                h.IndexOf("RESERVESKILL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                h.IndexOf("RESERVE SKILL", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            if (h.IndexOf("敵のみのスキルです", StringComparison.Ordinal) >= 0 ||
                h.IndexOf("enemy-only", StringComparison.OrdinalIgnoreCase) >= 0 ||
                h.IndexOf("enemy only", StringComparison.OrdinalIgnoreCase) >= 0 ||
                h.IndexOf("enemies only", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            // Obvious raw/internal placeholders.
            if (n.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                h.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return false;

            // Empty or placeholder-like official help should never receive generated detail.
            if (String.IsNullOrWhiteSpace(h))
                return false;

            return true;
        }

        private static string StripGeneratedSuffix(string s)
        {
            string t = NocturneDetailedSkillInfoMod.Normalize(s);

            string[] markers =
            {
                "　威力:",
                "　回復基礎値:",
                "　感電付与値:",
                "　凍結付与値:",
                "　睡眠付与値:",
                "　混乱付与値:",
                "　緊縛付与値:",
                "　魔封付与値:",
                "　毒付与値:",
                "　魅了付与値:",
                "　麻痺付与値:",
                "　石化付与値:",
                "　即死付与値:",
                "　回数:",
                "　物理攻撃:",
                "　魔法威力:",
                "　命中:",
                "　防御:",
                "　回避:",
                "  Power:",
                "  Heal Base:",
                "  Shock Rate:",
                "  Freeze Rate:",
                "  Sleep Rate:",
                "  Panic Rate:",
                "  Bind Rate:",
                "  Mute Rate:",
                "  Poison Rate:",
                "  Charm Rate:",
                "  Paralyze Rate:",
                "  Petrify Rate:",
                "  Instant Death Rate:",
                "  Hits:",
                "  Phys Atk:",
                "  Mag Power:",
                "  Accuracy:",
                "  Defense:",
                "  Evasion:",
                "  Crit:"
            };

            int cut = -1;

            foreach (string marker in markers)
            {
                int p = t.IndexOf(marker, StringComparison.Ordinal);
                if (p >= 0 && (cut < 0 || p < cut))
                    cut = p;
            }

            if (cut >= 0)
                return t.Substring(0, cut).TrimEnd();

            return t;
        }

        public static string DecodeAilment(uint bits)
            => DecodeAilment(bits, DetailLanguage.Japanese);

        private static string DecodeAilment(uint bits, DetailLanguage lang)
        {
            if (lang == DetailLanguage.Japanese)
            {
                return bits switch
                {
                    1u    => "感電",
                    2u    => "凍結",
                    4u    => "睡眠",
                    8u    => "混乱",
                    16u   => "緊縛",
                    32u   => "魔封",
                    64u   => "毒",
                    128u  => "魅了",
                    256u  => "麻痺",
                    1024u => "石化",
                    2048u => "即死",
                    _     => ""
                };
            }

            return bits switch
            {
                1u    => "Shock",
                2u    => "Freeze",
                4u    => "Sleep",
                8u    => "Panic",
                16u   => "Bind",
                32u   => "Mute",
                64u   => "Poison",
                128u  => "Charm",
                256u  => "Paralyze",
                1024u => "Petrify",
                2048u => "Instant Death",
                _     => ""
            };
        }
    }
}
